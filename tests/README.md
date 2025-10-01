# Test Suite Summary

## Comprehensive Validation Testing

This test suite provides full feature validation for the Genesys Cloud Notification Collector application.

### Test Coverage

#### 1. Build Validation Tests
- Dependency resolution (Newtonsoft.Json package)
- .NET runtime version compliance (.NET 8.0+)

#### 2. Authentication Utility Tests  
- Base64URL encoding format validation
- SHA256 hash generation capability

#### 3. Configuration Tests
- Application data path construction
- Settings JSON serialization/deserialization

#### 4. Prerequisites Validation Tests
- Operating system detection (Windows requirement)
- .NET SDK version requirement (.NET 8.0 SDK or later)
- Required namespace availability (HTTP, JSON, WebSockets)
- Dependency package validation (Newtonsoft.Json)

#### 5. API Endpoint Tests
- API endpoint URL format for different Genesys Cloud regions
- WebSocket URL construction
- OAuth redirect URI format validation

#### 6. Integration Tests
- Main assembly loading
- GenesysAuth class accessibility
- Public API method availability

### Running Tests

```bash
# Run all tests
dotnet test tests/GenesysCloudNotificationCollector.Tests.csproj

# Run with detailed output
dotnet test tests/GenesysCloudNotificationCollector.Tests.csproj --verbosity detailed
```

### CI/CD Integration

These tests are automatically run by GitHub Actions on every pull request to the main branch. Test results are uploaded as artifacts for review.

### Test Framework

- **xUnit**: Industry-standard .NET testing framework
- **Microsoft.NET.Test.Sdk**: Test execution platform
- **Visual Studio Test Runner**: For IDE integration
- **Coverlet**: Code coverage analysis

### Total Tests: 19

All tests validate critical functionality and prerequisites without requiring external dependencies or authentication.
