using Api.Middleware;
using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Seeding;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Fail fast on missing JWT config at boot, instead of the previous null-forgiving "!" that
// only threw lazily, the moment the AddJwtBearer options delegate first ran on a real request.
static string RequireConfigValue(IConfiguration configuration, string key) =>
    configuration[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Configuration is missing required value '{key}'.");

var jwtSettings = new JwtSettings
{
    Key = RequireConfigValue(builder.Configuration, "Jwt:Key"),
    Issuer = RequireConfigValue(builder.Configuration, "Jwt:Issuer"),
    Audience = RequireConfigValue(builder.Configuration, "Jwt:Audience"),
    ExpiryMinutes = builder.Configuration.GetValue("Jwt:ExpiryMinutes", 60),
};

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
// AddIdentity (above) sets Default{Authenticate,Challenge,SignIn}Scheme to the Identity cookie
// handlers; those explicit per-purpose values beat a mere DefaultScheme, so [Authorize] must set
// all three explicitly here (after AddIdentity) or every request gets a cookie-login challenge
// and the JwtBearer handler is never consulted.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SameUnitorAdmin", policy =>
        policy.Requirements.Add(new SameUnitOrAdminRequirement()));
    options.AddPolicy("GuardTodayOnly", policy =>
        policy.Requirements.Add(new GuardTodayOnlyRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, SameUnitOrAdminHandler>();
builder.Services.AddScoped<IAuthorizationHandler, GuardTodayOnlyHandler>();
builder.Services.AddScoped<IUnitAccessGuard, UnitAccessGuard>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<IComplaintCategoryRepository, ComplaintCategoryRepository>();
builder.Services.AddScoped<IComplaintStatusHistoryRepository, ComplaintStatusHistoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IComplaintService, ComplaintService>();

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var seedScope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(seedScope.ServiceProvider, app.Configuration, app.Environment);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
