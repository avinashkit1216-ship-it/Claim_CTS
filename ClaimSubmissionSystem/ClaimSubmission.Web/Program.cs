using ClaimSubmission.Web.Services;
using ClaimSubmission.Web.Middleware;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// PART 1: ADD SERVICES
// ============================================================================

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add data protection for sensitive data
builder.Services.AddDataProtection();

// Add session services with hardened configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.Name = ".AspNetCore.Session.ClaimSubmission";  // Explicit name
    options.Cookie.HttpOnly = true;                               // Prevent JavaScript access
    options.Cookie.IsEssential = true;                            // Required for functionality
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always                               // HTTPS only in production
        : CookieSecurePolicy.SameAsRequest;                       // HTTP in development
    options.Cookie.SameSite = SameSiteMode.Strict;               // Prevent CSRF
});

// Add distributed memory cache (required for distributed session)
builder.Services.AddDistributedMemoryCache();

// Configure CORS with strict policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowApi", policyBuilder =>
    {
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5285";
        var uri = new Uri(apiBaseUrl);
        
        // Strict CORS configuration for production
        policyBuilder
            .WithOrigins(uri.GetLeftPart(UriPartial.Authority))
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
    });
    
    // Optional: Add CORS for Swagger/development
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowSwagger", policyBuilder =>
        {
            policyBuilder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    }
});

// Get API base URL from configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5285";

// Add HttpClientFactory and service registrations with proper configuration
builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
    // Add User-Agent header for API tracking
    client.DefaultRequestHeaders.Add("User-Agent", "ClaimSubmission.Web/1.0");
});

builder.Services.AddHttpClient<IClaimApiService, ClaimApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "ClaimSubmission.Web/1.0");
});

// Register IAuthenticationService factory
builder.Services.AddScoped<IAuthenticationService>(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(nameof(AuthenticationService));
    var logger = provider.GetRequiredService<ILogger<AuthenticationService>>();
    return new AuthenticationService(httpClient, apiBaseUrl, logger);
});

// Register IClaimApiService factory
builder.Services.AddScoped<IClaimApiService>(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(nameof(ClaimApiService));
    var logger = provider.GetRequiredService<ILogger<ClaimApiService>>();
    return new ClaimApiService(httpClient, apiBaseUrl, logger);
});

// ✅ Add cookie authentication with secure defaults
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Authentication/Login";           // Redirect if not logged in
        options.LogoutPath = "/Authentication/Logout";         // Logout endpoint
        options.AccessDeniedPath = "/Authentication/AccessDenied"; // Forbidden page
        options.SlidingExpiration = true;                       // Extend session if active
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);      // Session timeout

        // ✅ Harden cookie security
        options.Cookie.Name = ".AspNetCore.ClaimSubmission.Auth";
        options.Cookie.HttpOnly = true;                          // Prevent JavaScript access
        options.Cookie.IsEssential = true;                       // Required for functionality
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always                          // HTTPS only in production
            : CookieSecurePolicy.SameAsRequest;                  // HTTP in development
        options.Cookie.SameSite = SameSiteMode.Strict;          // Prevent CSRF attacks
        
        // Configure events for logging
        options.Events = new CookieAuthenticationEvents
        {
            OnSignedIn = context =>
            {
                var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("CookieAuthentication");
                logger.LogInformation($"User authenticated with cookie. User ID: {userId}");
                return Task.CompletedTask;
            },
            OnSigningOut = context =>
            {
                var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("CookieAuthentication");
                logger.LogInformation($"User signing out. User ID: {userId}");
                return Task.CompletedTask;
            },
            OnValidatePrincipal = context =>
            {
                // Additional validation can be done here (e.g., check token expiration)
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ============================================================================
// PART 2: BUILD APP AND CONFIGURE MIDDLEWARE PIPELINE
// ============================================================================

var app = builder.Build();

// ✅ Configure the HTTP request pipeline with correct middleware ordering
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // Development-specific middleware
    app.UseDeveloperExceptionPage();
}

// Serve static files
app.UseStaticFiles();

// Add security headers middleware
app.Use(async (context, next) =>
{
    // Prevent MIME-type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    
    // Enable XSS protection
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    
    // Content Security Policy
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'";
    
    // Referrer Policy
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    await next();
});

// Routing must come early
app.UseRouting();

// ✅ CORRECT MIDDLEWARE ORDER:
// 1. Session middleware (before auth)
app.UseSession();

// 2. CORS policy
app.UseCors("AllowApi");

// 3. Custom authentication middleware (before ASP.NET Core auth)
app.UseMiddleware<AuthenticationSessionMiddleware>();

// 4. ✅ Authentication MUST come before Authorization
app.UseAuthentication();

// 5. Authorization
app.UseAuthorization();

// ============================================================================
// PART 3: MAP ENDPOINTS AND RUN
// ============================================================================

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Add health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run();
