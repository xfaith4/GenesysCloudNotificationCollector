using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace GenesysTopicSubscriberPOC
{
    public partial class MainWindow : Window
    {
        private TopicService _topicService;
        private NotificationService _notificationService;

        public ObservableCollection<Topic> AvailableTopics { get; } = new ObservableCollection<Topic>();
        public ICollectionView GroupedTopics { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _topicService = new TopicService("https://api.mypurecloud.com", GetTokenAsync);
            _notificationService = new NotificationService("wss://api.mypurecloud.com/notifications", GetTokenAsync);
            _notificationService.OnNotificationReceived += (msg) =>
            {
                Dispatcher.Invoke(() =>
                {
                    string line = $"{msg.ReceivedAt:O} [{msg.Topic}] {msg.Payload.ToString(Formatting.None)}";
                    txtNotifications.AppendText(line + Environment.NewLine);
                    txtNotifications.ScrollToEnd();
                });
            };

            _ = LoadTopics();
        }

        private async Task LoadTopics()
        {
            try
            {
                var topics = await _topicService.GetAvailableTopicsAsync();
                AvailableTopics.Clear();
                foreach (var t in topics)
                    AvailableTopics.Add(t);

                GroupedTopics = CollectionViewSource.GetDefaultView(AvailableTopics);
                GroupedTopics.GroupDescriptions.Clear();
                GroupedTopics.GroupDescriptions.Add(new PropertyGroupDescription("GroupingKey"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading topics: " + ex.Message);
            }
        }

        private async void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            var selected = listTopics.SelectedItems.Cast<Topic>().Select(t => t.Name).ToList();
            await _notificationService.SubscribeAsync(selected);
        }

        private async void BtnUnsubscribe_Click(object sender, RoutedEventArgs e)
        {
            await _notificationService.UnsubscribeAsync();
        }

        private Task<string> GetTokenAsync()
        {
            // Replace with real token logic or mock
            return Task.FromResult("mock-access-token");
        }
    }

    public class Topic
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string GroupingKey => Name?.Split('.')[2] ?? "Other";
    }

    public class NotificationMessage
    {
        public string Topic { get; set; }
        public DateTime ReceivedAt { get; set; }
        public JObject Payload { get; set; }
    }

    public class TopicService
    {
        private readonly string _baseApiUrl;
        private readonly Func<Task<string>> _getAuthToken;

        public TopicService(string baseApiUrl, Func<Task<string>> getAuthToken)
        {
            _baseApiUrl = baseApiUrl;
            _getAuthToken = getAuthToken;
        }

        public async Task<List<Topic>> GetAvailableTopicsAsync()
        {
            var token = await _getAuthToken();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"{_baseApiUrl}/api/v2/notifications/availabletopics");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var j = JObject.Parse(json);
            return j["entities"].Select(e => new Topic
            {
                Name = (string)e["name"],
                Description = (string)e["description"]
            }).ToList();
        }
    }

    public class NotificationService
    {
        private readonly string _wsUrl;
        private readonly Func<Task<string>> _getAuthToken;
        private ClientWebSocket _websocket;

        public event Action<NotificationMessage> OnNotificationReceived;

        public NotificationService(string wsUrl, Func<Task<string>> getAuthToken)
        {
            _wsUrl = wsUrl;
            _getAuthToken = getAuthToken;
        }

        public async Task SubscribeAsync(IEnumerable<string> topicNames)
        {
            if (_websocket?.State == WebSocketState.Open)
            {
                await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "resubscribe", CancellationToken.None);
                _websocket.Dispose();
            }

            _websocket = new ClientWebSocket();
            var token = await _getAuthToken();
            _websocket.Options.SetRequestHeader("Authorization", "Bearer " + token);
            await _websocket.ConnectAsync(new Uri(_wsUrl), CancellationToken.None);

            var subscribeMsg = new { type = "subscribe", topics = topicNames };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(subscribeMsg));
            await _websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

            _ = Task.Run(async () =>
            {
                var buf = new byte[65536];
                while (_websocket.State == WebSocketState.Open)
                {
                    var seg = new ArraySegment<byte>(buf);
                    var result = await _websocket.ReceiveAsync(seg, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server close", CancellationToken.None);
                        break;
                    }
                    var msg = Encoding.UTF8.GetString(buf, 0, result.Count);
                    try
                    {
                        var jobj = JObject.Parse(msg);
                        var nm = new NotificationMessage
                        {
                            Topic = jobj["topic"]?.ToString(),
                            ReceivedAt = DateTime.UtcNow,
                            Payload = jobj
                        };
                        OnNotificationReceived?.Invoke(nm);
                    }
                    catch { /* log if needed */ }
                }
            });
        }

        public async Task UnsubscribeAsync()
        {
            if (_websocket?.State == WebSocketState.Open)
            {
                var unsub = new { type = "unsubscribe" };
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(unsub));
                await _websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "unsubscribe", CancellationToken.None);
            }
        }
    }
}