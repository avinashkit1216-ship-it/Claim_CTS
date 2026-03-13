using ClaimSubmission.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

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

// Add HttpClientFactory and authentication service
builder.Services.AddHttpClient<AuthenticationService>((provider, client) =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var apiBaseUrl = config["ApiBaseUrl"] ?? "http://localhost:5285";
    client.BaseAddress = new Uri(apiBaseUrl);
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
}

app.UseHttpsRedirection();
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
