using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GenesysTopicSubscriberPOC
{
    public static class GenesysAuth
    {
        private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenesysTopicSubscriberPOC");
        private static readonly string TokenPath = Path.Combine(AppDataPath, "access_token.json");
        private static readonly string SettingsPath = Path.Combine(AppDataPath, "settings.json");

        private class TokenCache
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("expires_at")]
            public DateTime ExpiresAt { get; set; }
        }

        private class Settings
        {
            [JsonPropertyName("client_id")]
            public string ClientId { get; set; }

            [JsonPropertyName("redirect_uri")]
            public string RedirectUri { get; set; }

            [JsonPropertyName("region")]
            public string Region { get; set; }
        }

        public static async Task<string> GetTokenAsync()
        {
            if (File.Exists(TokenPath))
            {
                var cache = JsonSerializer.Deserialize<TokenCache>(File.ReadAllText(TokenPath));
                if (cache != null && cache.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
                {
                    return cache.AccessToken;
                }
            }

            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath));
            if (settings == null)
                throw new Exception("Missing settings.json for PKCE auth.");

            return await RunPkceFlowAsync(settings);
        }

        private static async Task<string> RunPkceFlowAsync(Settings settings)
        {
            string codeVerifier = GenerateCodeVerifier();
            string codeChallenge = GenerateCodeChallenge(codeVerifier);

            string authUrl = $"https://login.{settings.Region}.pure.cloud/oauth/authorize?" +
                             $"response_type=code&client_id={settings.ClientId}&redirect_uri={Uri.EscapeDataString(settings.RedirectUri)}" +
                             $"&code_challenge={codeChallenge}&code_challenge_method=S256";

            using var listener = new HttpListener();
            listener.Prefixes.Add(settings.RedirectUri + "/");
            listener.Start();
            Process.Start(new ProcessStartInfo { FileName = authUrl, UseShellExecute = true });

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];
            using var writer = new StreamWriter(context.Response.OutputStream);
            writer.Write("<html><body>Login successful. You can close this window.</body></html>");
            writer.Flush();
            context.Response.Close();

            using var http = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, $"https://login.{settings.Region}.pure.cloud/oauth/token");
            req.Content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", settings.RedirectUri),
                new KeyValuePair<string, string>("client_id", settings.ClientId),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            });
            var res = await http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 30);

            Directory.CreateDirectory(AppDataPath);
            File.WriteAllText(TokenPath, JsonSerializer.Serialize(new TokenCache { AccessToken = accessToken, ExpiresAt = expiresAt }));

            return accessToken;
        }

        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string GenerateCodeChallenge(string verifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}