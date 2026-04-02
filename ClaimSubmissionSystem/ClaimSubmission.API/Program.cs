using ClaimSubmission.API.Data;
using ClaimSubmission.API.Data.LocalStorage;
using ClaimSubmission.API.Services;
using ClaimSubmission.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add local storage service
builder.Services.AddSingleton<LocalStorageService>();

// Add repository services - using local storage implementations
builder.Services.AddScoped<IClaimsRepository, LocalClaimsRepository>();
builder.Services.AddScoped<IAuthRepository, LocalAuthRepository>();

// Add business services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();

// Configure JWT Bearer Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClaimSubmissionAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ClaimSubmissionClients";
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = securityKey,
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Strict lifetime validation
    };
});

builder.Services.AddAuthorization();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("http://localhost:5277", "https://localhost:7277")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Seed test data in development
if (app.Environment.IsDevelopment())
{
    try
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Initializing local storage data seeding...");
        await DataSeeder.SeedTestUsersAsync(builder.Configuration, logger);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error seeding test data during startup");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("AllowWeb");
app.UseAuthentication();
app.UseAuthorization();

// Add default root endpoint
app.MapGet("/", () => "ClaimSubmission API is running (Local Storage Mode). Visit /swagger or /openapi to explore the API.");
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow, mode = "local-storage" });

app.MapControllers();

app.Run();
