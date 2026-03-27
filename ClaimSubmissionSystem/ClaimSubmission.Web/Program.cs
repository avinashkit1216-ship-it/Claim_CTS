using ClaimSubmission.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add distributed memory cache (required for distributed session)
builder.Services.AddDistributedMemoryCache();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowApi", policyBuilder =>
    {
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5285";
        var uri = new Uri(apiBaseUrl);
        policyBuilder.WithOrigins(uri.GetLeftPart(UriPartial.Authority))
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Get API base URL from configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5285";

// Add HttpClientFactory and service registrations with proper configuration
builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IClaimApiService, ClaimApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
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

// Add authentication
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// Use session middleware
app.UseSession();

// Use CORS
app.UseCors("AllowApi");

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
