using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureNotes.Core.Interfaces;
using SecureNotes.Core.Security;
using SecureNotes.Core.Services;
using SecureNotes.Infrastructure.Data;
using SecureNotes.Infrastructure.Redis;
using SecureNotes.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// --- Redis ---
var redisConnection = builder.Configuration["Redis:ConnectionString"]!;
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<TokenBlacklistService>();

// --- Encryption ---
var encryptionKey = builder.Configuration["Encryption:Key"]!;
builder.Services.AddSingleton<IEncryptionService>(
    new AesEncryptionService(encryptionKey));

// --- Repositories ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

// --- Services ---
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var jwtExpiry = int.Parse(builder.Configuration["Jwt:ExpiryMinutes"]!);
builder.Services.AddScoped<IAuthService>(sp => new AuthService(
    sp.GetRequiredService<IUserRepository>(),
    sp.GetRequiredService<IAuditLogRepository>(),
    sp.GetRequiredService<ITokenBlacklistService>(),
    jwtSecret,
    jwtExpiry));
builder.Services.AddScoped<INoteService, NoteService>();

// --- JWT Authentication ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var blacklist = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenBlacklistService>();
                var token = context.HttpContext.Request.Headers["Authorization"]
                    .ToString().Replace("Bearer ", "");

                if (await blacklist.IsBlacklistedAsync(token))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Auto migrate on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();