using ClaimSubmission.API.Data;
using ClaimSubmission.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add repository services
builder.Services.AddScoped<IClaimsRepository, ClaimsRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Add business services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("http://localhost:5277", "https://localhost:7205")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

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
app.MapGet("/", () => "ClaimSubmission API is running. Visit /swagger or /openapi to explore the API.");

app.MapControllers();

app.Run();
