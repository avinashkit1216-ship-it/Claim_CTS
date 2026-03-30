using ClaimSubmission.API.Data;
using ClaimSubmission.API.Data.LocalStorage;
using ClaimSubmission.API.Services;
using ClaimSubmission.API.Middleware;

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
app.UseAuthorization();

// Add default root endpoint
app.MapGet("/", () => "ClaimSubmission API is running (Local Storage Mode). Visit /swagger or /openapi to explore the API.");
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow, mode = "local-storage" });

app.MapControllers();

app.Run();
