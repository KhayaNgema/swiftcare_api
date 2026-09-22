
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using swiftcare_api.Data;
using swiftcare_api.Models;
using swiftcare_api.Services;
using System.Globalization;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------- Connection String ----------
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

// ---------- Database ----------
builder.Services.AddDbContext<swiftcare_apiDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutter", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------- Identity ----------
builder.Services.AddIdentity<UserBaseModel, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<swiftcare_apiDbContext>()
.AddDefaultTokenProviders();


// ---------- Multipart Upload ----------
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 268435456;
});


// ---------- Encryption ----------
var key = builder.Configuration["AES_KEY"];
var iv = builder.Configuration["AES_IV"];

var keyBytes = Convert.FromBase64String(key!);
var ivBytes = Convert.FromBase64String(iv!);

// ---------- Identity Token Lifespan ----------
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(72);
});


// ---------- JWT Authentication ----------
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],

        IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
            )
    };
});


// ---------- Authorization ----------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AnyRole", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireAssertion(context =>
            context.User.Claims.Any(
                c => c.Type == ClaimTypes.Role));
    });
});


// ---------- Session ----------
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// ---------- Logging ----------
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

builder.Services.AddHttpClient<
    IDiditFaceLoginService,
    DiditFaceLoginService>();

builder.Services.AddHttpClient();


// ---------- SignalR ----------
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;
});

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------- Controllers ----------
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });


// ---------- Razor Pages ----------
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();


// ---------- Swagger ----------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "swift_care API",
            Version = "v1"
        });

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter 'Bearer' [space] and then your valid JWT token."
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});


// ============================================================
// BUILD APP
// ============================================================

var app = builder.Build();


// ---------- Swagger ----------
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Ecommerce API V1");

    c.RoutePrefix = "swagger";
});


// ---------- Culture ----------
SetCulture("en-US");

var defaultCulture = new CultureInfo("en-US");

app.UseRequestLocalization(
    new RequestLocalizationOptions
    {
        DefaultRequestCulture =
            new RequestCulture(defaultCulture),

        SupportedCultures =
            new List<CultureInfo>
            {
                defaultCulture
            },

        SupportedUICultures =
            new List<CultureInfo>
            {
                defaultCulture
            }
    });



// ---------- Middleware ----------
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseSession();
app.UseCors("AllowFlutter");

app.UseAuthentication();

app.UseAuthorization();


// ============================================================
// DATABASE SEEDING
// ============================================================

// ============================================================
// ROUTES
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "tabRedirect",
    pattern: "{tab?}",
    defaults: new
    {
        controller = "Home",
        action = "Index"
    },
    constraints: new
    {
        tab =
            @"sportnews|fixtures|standings|matchresults|clubs|players|managers|topScores|topAssists"
    });

app.MapRazorPages();

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    // Clear all session data
    httpContext.Session.Clear();

    // Sign out of the configured authentication scheme
    await httpContext.SignOutAsync();

    return Results.Ok(new
    {
        success = true,
        message = "Logout successful. Session has been cleared."
    });
});
// ---------- Run ----------
app.Run();


// ============================================================
// CULTURE METHOD
// ============================================================

void SetCulture(string cultureCode)
{
    var culture =
        CultureInfo.CreateSpecificCulture(cultureCode);

    Thread.CurrentThread.CurrentCulture = culture;
    Thread.CurrentThread.CurrentUICulture = culture;
}