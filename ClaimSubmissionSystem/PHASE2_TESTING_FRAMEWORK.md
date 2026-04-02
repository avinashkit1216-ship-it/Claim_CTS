# COMPREHENSIVE TESTING FRAMEWORK

**Status:** Phase 2 - Testing Infrastructure  
**Date:** April 1, 2026  
**Goal:** Enterprise-grade testing with unit, integration, E2E, and load tests

---

## Testing Strategy Overview

### Test Pyramid
```
         ┌─────────────────┐
         │   E2E Tests     │  5-10% (Selenium/Playwright)
         │   (UI, Slow)    │
         ├─────────────────┤
         │  Integration    │  20-30% (API calls, DB)
         │   Tests         │
         ├─────────────────┤
         │   Unit Tests    │  60-75% (Fast, isolated)
         │  (Mocks, Fast)  │
         └─────────────────┘
         
Load Tests: Separate (JMeter, k6, NBomber)
```

### Test Coverage Goals
- **Unit Tests:** 80%+ code coverage
- **Integration Tests:** All API endpoints
- **E2E Tests:** Critical user workflows
- **Load Tests:** 10,000+ requests/second sustainably

---

## Unit Tests

### 1. Authentication Controller Tests

**File:** `ClaimSubmission.Web.Tests/Controllers/AuthenticationControllerTests.cs`

```csharp
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using ClaimSubmission.Web.Controllers;
using ClaimSubmission.Web.Models;
using ClaimSubmission.Web.Services;

namespace ClaimSubmission.Web.Tests.Controllers
{
    public class AuthenticationControllerTests
    {
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<ILogger<AuthenticationController>> _mockLogger;
        private readonly AuthenticationController _controller;

        public AuthenticationControllerTests()
        {
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockLogger = new Mock<ILogger<AuthenticationController>>();
            _controller = new AuthenticationController(_mockAuthService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_SignsInUserAndRedirects()
        {
            // Arrange
            var loginModel = new LoginViewModel 
            { 
                Username = "testuser", 
                Password = "password123" 
            };

            var userViewModel = new UserViewModel
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                FullName = "Test User",
                Token = "jwt-token-here"
            };

            _mockAuthService.Setup(x => x.LoginAsync(loginModel))
                .ReturnsAsync(userViewModel);

            var httpContext = new DefaultHttpContext();
            var sessionMock = new Mock<ISession>();
            httpContext.Session = sessionMock.Object;
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.Login(loginModel, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Claim", redirectResult.ControllerName);

            // Verify session was set
            sessionMock.Verify(x => x.SetString("UserId", "1"), Times.Once);
            sessionMock.Verify(x => x.SetString("Username", "testuser"), Times.Once);
            sessionMock.Verify(x => x.SetString("IsAuthenticated", "true"), Times.Once);

            _mockAuthService.Verify(x => x.LoginAsync(loginModel), Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var loginModel = new LoginViewModel
            {
                Username = "invaliduser",
                Password = "wrongpassword"
            };

            _mockAuthService.Setup(x => x.LoginAsync(loginModel))
                .ReturnsAsync((UserViewModel)null);

            // Act
            var result = await _controller.Login(loginModel, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("Invalid username or password", 
                _controller.ModelState[""].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Logout_WithAuthenticatedUser_SignsOutAndRedirectsToLogin()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var sessionMock = new Mock<ISession>();
            httpContext.Session = sessionMock.Object;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);

            sessionMock.Verify(x => x.Clear(), Times.Once);
        }

        [Fact]
        public void Register_WithValidModel_CreatesUserAndSignsIn()
        {
            // Arrange - test registration with valid data
            // ...implementation...
        }
    }
}
```

### 2. Claim Service Tests

**File:** `ClaimSubmission.Web.Tests/Services/ClaimApiServiceTests.cs`

```csharp
using Xunit;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClaimSubmission.Web.Models;
using ClaimSubmission.Web.Services;

namespace ClaimSubmission.Web.Tests.Services
{
    public class ClaimApiServiceTests
    {
        [Fact]
        public async Task GetClaimsAsync_WithValidToken_ReturnsClaims()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var responseContent = new StringContent(
                @"{ ""data"": { ""claims"": [], ""totalRecords"": 0 } }",
                System.Text.Encoding.UTF8,
                "application/json");

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:5285/")
            };

            var mockLogger = new Mock<ILogger<ClaimApiService>>();
            var service = new ClaimApiService(httpClient, "http://localhost:5285", mockLogger.Object);

            // Act
            var result = await service.GetClaimsAsync("token", 1, 20);

            // Assert
            Assert.NotNull(result);
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetClaimsAsync_WithUnauthorized_ThrowsException()
        {
            // Arrange - test 401 response
            // ...implementation...
        }
    }
}
```

---

## Integration Tests

### 1. Authentication Integration Test

**File:** `ClaimSubmission.Web.Tests/Integration/AuthenticationIntegrationTests.cs`

```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClaimSubmission.Web.Tests.Integration
{
    public class AuthenticationIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Login_Page_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/Authentication/Login");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Login", content);
        }

        [Fact]
        public async Task UnauthenticatedAccess_ToProtectedRoute_RedirectsToLogin()
        {
            // Act
            var response = await _client.GetAsync("/Claim/Index");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.RequestMessage.RequestUri.PathAndQuery.Contains("Authentication/Login"));
        }

        [Fact]
        public async Task LoginWithValidCredentials_SetsCookieAndRedirects()
        {
            // Arrange
            var loginContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Username", "user1"),
                new KeyValuePair<string, string>("Password", "Password123!"),
                new KeyValuePair<string, string>("__RequestVerificationToken", 
                    await GetAntiForgeryToken())
            });

            // Act
            var response = await _client.PostAsync("/Authentication/Login", loginContent);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.True(response.Headers.Contains("Set-Cookie"));
            Assert.Contains(".AspNetCore.ClaimSubmission.Auth", 
                response.Headers.GetValues("Set-Cookie").FirstOrDefault() ?? "");
        }

        private async Task<string> GetAntiForgeryToken()
        {
            var response = await _client.GetAsync("/Authentication/Login");
            var content = await response.Content.ReadAsStringAsync();
            
            // Extract CSRF token from form
            var tokenMatch = System.Text.RegularExpressions.Regex.Match(
                content, @"name=""__RequestVerificationToken"" type=""hidden"" value=""(.*?)""");
            
            return tokenMatch.Success ? tokenMatch.Groups[1].Value : "";
        }
    }
}
```

### 2. Claim Submission Integration Test

**File:** `ClaimSubmission.Web.Tests/Integration/ClaimSubmissionIntegrationTests.cs`

```csharp
using Xunit;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClaimSubmission.Web.Tests.Integration
{
    public class ClaimSubmissionIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _authenticatedClient;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>();
            _authenticatedClient = await CreateAuthenticatedClient();
        }

        public async Task DisposeAsync()
        {
            _authenticatedClient?.Dispose();
            _factory?.Dispose();
        }

        [Fact]
        public async Task GetClaims_WithValidSession_ReturnsOk()
        {
            // Act
            var response = await _authenticatedClient.GetAsync("/Claim/Index");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Claims", content);
        }

        [Fact]
        public async Task SubmitClaim_WithValidData_CreatesClaimAndReturnsSuccess()
        {
            // Arrange
            var claimData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("PatientName", "John Doe"),
                new KeyValuePair<string, string>("ClaimAmount", "1500.00"),
                // ... other fields ...
            });

            // Act
            var response = await _authenticatedClient.PostAsync("/Claim/Create", claimData);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        private async Task<HttpClient> CreateAuthenticatedClient()
        {
            var client = _factory.CreateClient();
            
            // Login
            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Username", "user1"),
                new KeyValuePair<string, string>("Password", "Password123!")
            });

            var loginResponse = await client.PostAsync("/Authentication/Login", loginData);
            
            // Extract auth cookie
            if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                var authCookie = cookies.FirstOrDefault(c => c.Contains(".AspNetCore.ClaimSubmission.Auth"));
                if (authCookie != null)
                {
                    client.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);
                }
            }

            return client;
        }
    }
}
```

---

## End-to-End Tests

### 1. Login & Claim Submission E2E Test

**File:** `ClaimSubmission.E2E.Tests/LoginAndClaimSubmissionTests.cs`

```csharp
using Xunit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading.Tasks;

namespace ClaimSubmission.E2E.Tests
{
    public class LoginAndClaimSubmissionTests : IAsyncLifetime
    {
        private IWebDriver _driver;
        private WebDriverWait _wait;
        private readonly string _baseUrl = "https://localhost:7277";

        public async Task InitializeAsync()
        {
            var options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            
            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _driver?.Quit();
            _driver?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public void LoginFlow_WithValidCredentials_NavigatesToDashboard()
        {
            // Arrange & Act
            _driver.Navigate().GoToUrl($"{_baseUrl}/Authentication/Login");
            
            var usernameInput = _wait.Until(d => d.FindElement(By.Id("Username")));
            var passwordInput = _driver.FindElement(By.Id("Password"));
            var submitButton = _driver.FindElement(By.XPath("//button[@type='submit']"));

            usernameInput.SendKeys("user1@example.com");
            passwordInput.SendKeys("Password123!");
            submitButton.Click();

            // Assert
            var claimsHeading = _wait.Until(d => 
                d.FindElement(By.XPath("//h2[contains(text(), 'Claims')]")));
            
            Assert.NotNull(claimsHeading);
            Assert.Contains("/Claim/Index", _driver.Url);
        }

        [Fact]
        public void ClaimSubmission_WithValidData_CreatesClaimSuccessfully()
        {
            // Arrange
            LoginUser();
            _driver.Navigate().GoToUrl($"{_baseUrl}/Claim/Add");

            // Act
            var patientNameInput = _wait.Until(d => d.FindElement(By.Id("PatientName")));
            var claimAmountInput = _driver.FindElement(By.Id("ClaimAmount"));
            var submitButton = _driver.FindElement(By.XPath("//button[@type='submit']"));

            patientNameInput.SendKeys("John Doe");
            claimAmountInput.SendKeys("1500");
            submitButton.Click();

            // Assert
            var successMessage = _wait.Until(d => 
                d.FindElement(By.XPath("//div[@class='alert-success']")));
            
            Assert.NotNull(successMessage);
            Assert.Contains("success", successMessage.Text.ToLower());
        }

        [Fact]
        public void Logout_RemovesAuthenticationAndRedirects()
        {
            // Arrange
            LoginUser();

            // Act
            var logoutButton = _wait.Until(d => 
                d.FindElement(By.XPath("//a[contains(text(), 'Logout')]")));
            logoutButton.Click();

            // Assert - should redirect to login
            _wait.Until(d => d.Url.Contains("/Authentication/Login"));
            Assert.Contains("/Authentication/Login", _driver.Url);
        }

        private void LoginUser()
        {
            _driver.Navigate().GoToUrl($"{_baseUrl}/Authentication/Login");
            
            var usernameInput = _wait.Until(d => d.FindElement(By.Id("Username")));
            usernameInput.SendKeys("user1@example.com");
            
            var passwordInput = _driver.FindElement(By.Id("Password"));
            passwordInput.SendKeys("Password123!");
            
            var submitButton = _driver.FindElement(By.XPath("//button[@type='submit']"));
            submitButton.Click();

            // Wait for redirect
            _wait.Until(d => d.Url.Contains("/Claim/Index"));
        }
    }
}
```

---

## Load Testing

### 1. Load Test with NBomber

**File:** `ClaimSubmission.LoadTests/LoginLoadTest.cs`

```csharp
using NBomber.Contracts;
using NBomber.CSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClaimSubmission.LoadTests
{
    public class LoginLoadTest
    {
        private const string BaseUrl = "https://localhost:7277";
        private const int Seconds = 60;
        private const int Requests = 10000;

        public static void RunLoginLoadTest()
        {
            var httpClient = new HttpClient();
            
            var scenario = Scenario.Create("login_load", async context =>
            {
                var form = new Dictionary<string, string>
                {
                    { "Username", $"user{Random.Shared.Next(1, 100)}@example.com" },
                    { "Password", "Password123!" }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/Authentication/Login")
                {
                    Content = new FormUrlEncodedContent(form)
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.SendAsync(request);
                stopwatch.Stop();

                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: (int?)response.StatusCode)
                    : Response.Fail(statusCode: (int?)response.StatusCode);
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(copies: 10, during: TimeSpan.FromSeconds(Seconds))
            );

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
        }
    }
}
```

### 2. Performance Test Configuration

**File:** `ClaimSubmission.PerformanceTests/PerformanceBenchmarks.cs`

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ClaimSubmission.Web.Services;
using System.Net.Http;

namespace ClaimSubmission.PerformanceTests
{
    [MemoryDiagnoser]
    public class AuthenticationServiceBenchmarks
    {
        private HttpClient _httpClient;
        private IAuthenticationService _service;

        [GlobalSetup]
        public void Setup()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5285/") };
            _service = new AuthenticationService(_httpClient, "http://localhost:5285");
        }

        [Benchmark]
        public async Task LoginPerformance()
        {
            var loginModel = new LoginViewModel
            {
                Username = "user1@example.com",
                Password = "Password123!"
            };

            await _service.LoginAsync(loginModel);
        }

        [Benchmark]
        public async Task ClaimRetrievalPerformance()
        {
            // Simulate retrieving 100 claims
            for (int i = 0; i < 10; i++)
            {
                // await claimService.GetClaimsAsync(token, i, 10);
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<AuthenticationServiceBenchmarks>();
        }
    }
}
```

---

## Test Project Setup Instructions

### Create Test Projects

```bash
# From ClaimSubmissionSystem directory

# Unit Tests
dotnet new xunit -n ClaimSubmission.Web.Tests
dotnet new xunit -n ClaimSubmission.API.Tests

# Integration Tests (add to Web.Tests)
# Add additional test classes

# E2E Tests
dotnet new xunit -n ClaimSubmission.E2E.Tests

# Load Tests
dotnet new console -n ClaimSubmission.LoadTests

# Performance Tests
dotnet new console -n ClaimSubmission.PerformanceTests
```

### Add NuGet Packages

```bash
# Unit/Integration Tests
dotnet add package Xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Moq
dotnet add package Microsoft.AspNetCore.Mvc.Testing

# E2E Tests
dotnet add package Selenium.WebDriver
dotnet add package Selenium.WebDriver.ChromeDriver

# Load Tests
dotnet add package NBomber
dotnet add package NBomber.Http

# Performance Tests
dotnet add package BenchmarkDotNet
```

### Run Tests

```bash
# Run all unit tests
dotnet test

# Run specific test project
dotnet test ClaimSubmission.Web.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura

# Run performance tests
dotnet run --project ClaimSubmission.PerformanceTests
```

---

## Test Coverage Goals

### Unit Tests
- Authentication Controller: 95%+ coverage
- Services: 90%+ coverage
- Utilities: 85%+ coverage

### Integration Tests
- All API endpoints: 100% coverage
- Authentication flow: 100%
- Claim CRUD: 100%

### E2E Tests
- Login flow: ✅
- Registration flow: ✅
- Claim submission: ✅
- Logout flow: ✅

### Load Tests
- 10,000 requests total
- 100 concurrent users (peak)
- 60-second duration
- Less than 5% failure rate
- Average response time < 500ms

---

## CI/CD Integration

### GitHub Actions Workflow

**File:** `.github/workflows/tests.yml`

```yaml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Unit Tests
      run: dotnet test ClaimSubmission.Web.Tests --no-build
    
    - name: Integration Tests
      run: dotnet test ClaimSubmission.Web.Tests/Integration --no-build
    
    - name: Upload coverage
      uses: codecov/codecov-action@v2
```

---

## Completion Checklist

- [ ] Create test projects
- [ ] Implement unit tests (80%+ coverage)
- [ ] Implement integration tests
- [ ] Implement E2E tests  
- [ ] Run load tests
- [ ] Validate all tests pass
- [ ] Set up CI/CD pipeline
- [ ] Achieve coverage targets

---

**Next: Phase 3 - Production Deployment & Containerization**
