using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InvoiceService.Data;
using InvoiceService.Middleware;
using InvoiceService.Seeders;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// JWT KEY GENERATION AND VALIDATION
string jwtKey = EnsureJwtKey(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Your normal Swagger config
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // Add the JWT security scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Apply the scheme globally to all operations
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2", // doesn't affect functionality much
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// CONFIGURE POSTGRESQL DATABASE CONTEXT
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(
builder.Configuration.GetConnectionString("DefaultConnection")
));

// REGISTER ALL SERVICE HERE
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InvoiceServices>();

// CONFIGURE JWT AUTHENTICATION
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            RoleClaimType = ClaimTypes.Role,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // ALLOW 5 MINUTES CLOCK SKEW AFTER TOKEN EXPIRES
        };

        // Add debugging for token validation
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // Console.WriteLine("Token successfully validated");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                //    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

// CONFIGURE FOR CORS POLICY FOR FRONTEND
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendClients", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            var uri = new Uri(origin);
            return uri.Host == "localhost" || uri.Host == "10.245.85.115";
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// JWT ENCRYPTION
static string EnsureJwtKey(IConfiguration configuration)
{
    var jwtKey = configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        jwtKey = GenerateJwtKey();
        // Console.WriteLine("Generated new JWT Key: " + jwtKey);
        configuration["Jwt:Key"] = jwtKey;
    }

    if (jwtKey.Length < 32)
    {
        Environment.Exit(1);
        throw new Exception("JWT Key must be at least 256 bits (32 characters) long.");
    }
    return jwtKey;
}

// METHOD TO GENERATE A CRYPTOGRAPHICALLY SECURE KEY
static string GenerateJwtKey()
{
    // METHOD TO GENERATE A 512-BIT (64-BYTE) CRYPTOGRAPHICALLY SECURE JWT KEY
    byte[] key = new byte[64];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(key);
    }
    return Convert.ToBase64String(key);
}

var app = builder.Build();

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        Console.WriteLine("Checking and applying database migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Database is up to date!");
    }
    catch (Exception ex)
    {
       Console.WriteLine($"Migration failed: {ex.Message}");
    }
}

// SEED THE SUPER ADMIN USER TO DATABASE
await SuperAdminSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

app.UseCors("AllowFrontendClients");
app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
