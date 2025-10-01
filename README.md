# Genesys Cloud Notification Collector

A Windows WPF desktop application that allows you to subscribe to and monitor Genesys Cloud platform notifications in real-time.

## Overview

The Genesys Cloud Notification Collector is a proof-of-concept application that demonstrates how to:
- Authenticate with Genesys Cloud using OAuth 2.0 PKCE flow
- Retrieve available notification topics from the Genesys Cloud API
- Subscribe to specific topics via WebSocket
- Display real-time notifications from the Genesys Cloud platform

## Features

- **OAuth 2.0 Authentication**: Secure authentication using PKCE (Proof Key for Code Exchange) flow
- **Topic Discovery**: Browse and search available notification topics organized by category
- **Real-time Notifications**: Subscribe to topics and receive live updates via WebSocket
- **Persistent Token Management**: Automatic token caching and refresh handling
- **User-friendly Interface**: WPF-based GUI with grouped topic display and notification log

## Prerequisites

- Windows OS
- .NET 8.0 SDK or later
- A Genesys Cloud account with appropriate permissions
- A registered OAuth client in Genesys Cloud

## Setup

### 1. Clone the Repository

```bash
git clone https://github.com/xfaith4/GenesysCloudNotificationCollector.git
cd GenesysCloudNotificationCollector
```

### 2. Configure OAuth Settings

Create a `settings.json` file in the application data folder at:
```
%APPDATA%\GenesysTopicSubscriberPOC\settings.json
```

Use the following template (based on `src/settings.json.example`):

```json
{
  "client_id": "your-oauth-client-id",
  "redirect_uri": "http://localhost:8080",
  "region": "mypurecloud.com"
}
```

Replace the values with your Genesys Cloud OAuth client configuration:
- `client_id`: Your OAuth client ID from Genesys Cloud
- `redirect_uri`: The redirect URI configured for your OAuth client
- `region`: Your Genesys Cloud region (e.g., `mypurecloud.com`, `mypurecloud.ie`, etc.)

### 3. Build the Application

```bash
cd src
dotnet restore
dotnet build --configuration Release
```

### 4. Run the Application

```bash
dotnet run --project src/GenesysTopicSubscriberPOC.csproj
```

Or run the compiled executable from:
```
src\bin\Release\net8.0-windows\GenesysTopicSubscriberPOC.exe
```

## Usage

1. **Authentication**: On first launch, the application will open a browser window for OAuth authentication. Sign in with your Genesys Cloud credentials.

2. **Browse Topics**: The left panel displays all available notification topics, grouped by category.

3. **Subscribe**: Select one or more topics from the list and click "Subscribe" to start receiving notifications.

4. **Monitor Notifications**: Real-time notifications appear in the right panel with timestamp, topic name, and payload.

5. **Unsubscribe**: Click "Unsubscribe" to stop receiving notifications.

## Architecture

The application consists of several key components:

### MainWindow.xaml / MainWindow.xaml.cs
- Main application window and UI logic
- Topic browsing and selection
- Notification display

### GenesysAuth.cs
- OAuth 2.0 PKCE authentication flow
- Token management and caching
- Settings persistence

### TopicService
- Retrieves available notification topics from Genesys Cloud API
- Handles API authentication

### NotificationService
- WebSocket connection management
- Topic subscription/unsubscription
- Real-time message handling

## API Endpoints

The application interacts with the following Genesys Cloud APIs:

- **Authorization**: `https://login.{region}.pure.cloud/oauth/authorize`
- **Token Exchange**: `https://login.{region}.pure.cloud/oauth/token`
- **Available Topics**: `https://api.{region}/api/v2/notifications/availabletopics`
- **WebSocket Notifications**: `wss://api.{region}/notifications`

## Development

### Project Structure

```
GenesysCloudNotificationCollector/
├── .github/
│   └── workflows/
│       └── pr-validation.yml      # CI/CD workflow
├── src/
│   ├── App.xaml                   # Application definition
│   ├── App.xaml.cs
│   ├── MainWindow.xaml            # Main window UI
│   ├── MainWindow.xaml.cs         # Main window logic
│   ├── GenesysAuth.cs             # Authentication module
│   ├── GenesysTopicSubscriberPOC.csproj
│   ├── settings.json.example      # Configuration template
│   └── sample_export.jsonl        # Sample notification data
├── .gitignore
└── README.md
```

### Building from Source

```bash
# Restore NuGet packages
dotnet restore src/GenesysTopicSubscriberPOC.csproj

# Build in Debug mode
dotnet build src/GenesysTopicSubscriberPOC.csproj --configuration Debug

# Build in Release mode
dotnet build src/GenesysTopicSubscriberPOC.csproj --configuration Release
```

### CI/CD

The project includes a GitHub Actions workflow (`.github/workflows/pr-validation.yml`) that automatically:
- Validates pull requests to the main branch
- Restores dependencies
- Builds the project in Release configuration
- Runs tests (if test projects are present)

The workflow is triggered on:
- Pull request opened
- Pull request synchronized (new commits)
- Pull request reopened

## Dependencies

- **.NET 8.0**: Target framework
- **Newtonsoft.Json**: JSON serialization and parsing
- **System.Net.WebSockets**: WebSocket client implementation

## Troubleshooting

### Authentication Issues
- Ensure your OAuth client is properly configured in Genesys Cloud
- Verify the redirect URI matches between settings.json and your OAuth client configuration
- Check that your OAuth client has the necessary permissions

### Connection Issues
- Verify your Genesys Cloud region is correctly set in settings.json
- Check your network connection and firewall settings
- Ensure WebSocket connections are not blocked

### Build Issues
- Confirm .NET 8.0 SDK is installed: `dotnet --version`
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Restore packages: `dotnet restore`

## Security Considerations

- Access tokens are stored locally in `%APPDATA%\GenesysTopicSubscriberPOC\access_token.json`
- Tokens are cached with expiration tracking
- Never commit OAuth client secrets or access tokens to version control
- Use appropriate OAuth scopes for your use case

## License

This project is a proof-of-concept and is provided as-is for educational and demonstration purposes.

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

All pull requests will be validated by the automated CI/CD pipeline.

## Support

For issues related to:
- **Genesys Cloud Platform**: Refer to [Genesys Cloud Developer Center](https://developer.genesys.cloud/)
- **This Application**: Open an issue on GitHub

## Acknowledgments

This application demonstrates integration with the Genesys Cloud notification service using the platform's public APIs and WebSocket connections.
