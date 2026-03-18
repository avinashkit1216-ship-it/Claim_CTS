using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data.DbContext;
using TaskManagement.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// DEPENDENCY INJECTION CONFIGURATION FOR WEB LAYER
// ===================================================================

// Add MVC services
builder.Services.AddControllersWithViews();

// Configure Entity Framework Core with In-Memory Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("TaskManagementDb")
);

// Register the Repository (Infrastructure Layer)
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// Register the Validator (Application Layer)
builder.Services.AddScoped<TaskValidator>();

// Register the Service (Application Layer)
builder.Services.AddScoped<ITaskService, TaskService>();

// ===================================================================
// BUILD THE APPLICATION
// ===================================================================
var app = builder.Build();

// Auto-create database and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Task}/{action=Index}/{id?}");

await app.RunAsync();
