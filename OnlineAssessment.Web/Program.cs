using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OnlineAssessment.Web.Models;
using System.Text;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Try ports in sequence until one works
    var ports = new[] { 5000, 5001, 5002, 5003, 5004 };
    var bound = false;
    
    foreach (var port in ports)
    {
        try
        {
            serverOptions.ListenLocalhost(port);
            Console.WriteLine($"Server bound to port {port}");
            bound = true;
            break;
        }
        catch
        {
            Console.WriteLine($"Port {port} is in use, trying next port...");
        }
    }
    
    if (!bound)
    {
        throw new Exception("Could not find an available port to bind to.");
    }
});

// ✅ Load Configuration explicitly
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var configuration = builder.Configuration;

// ✅ Ensure JWT Secret is Valid
var jwtSecret = configuration["JWT:Secret"];
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 16)
{
    throw new Exception("JWT Secret Key is invalid! Ensure it is at least 16 characters long.");
}

// ✅ Add Database Context (MySQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 32))));

// ✅ Configure CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// ✅ Add Authentication with JWT and Cookies
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = configuration["JWT:Issuer"],
        ValidAudience = configuration["JWT:Audience"]
    };
});

// ✅ Add Authorization
builder.Services.AddAuthorization();

// ✅ Add Controllers
builder.Services.AddControllersWithViews();

// ✅ Configure Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OnlineAssessment API", Version = "v1" });

    // 🔹 Add JWT Authorization to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// ✅ Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OnlineAssessment API v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");  // Enable CORS globally
app.UseAuthentication();  // Enable Authentication Middleware
app.UseAuthorization();   // Enable Authorization Middleware

// Configure routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "test",
    pattern: "Test/{action}/{id?}",
    defaults: new { controller = "Test" });

app.MapControllers();

// Ensure uploads directory exists
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Add static file serving for uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.Run();
