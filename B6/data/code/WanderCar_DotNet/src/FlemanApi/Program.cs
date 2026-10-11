using System.Reflection;
using System.Text;
using FlemanApi.Data;
using FlemanApi.Middleware;
using FlemanApi.Security;
using FlemanApi.Service;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Serilog;

const string GoogleSignInScheme = "GoogleSignIn";

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (requirement #1) ----------------------------------------
builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/fleman-dotnet-.log", rollingInterval: RollingInterval.Day));

// ---- MVC / validation ---------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Enums as their plain string name (e.g. "CASH"), matching Java's
        // Jackson default for @Enumerated(STRING) fields — without this,
        // System.Text.Json serializes enums as raw numbers.
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddFluentValidationAutoValidation();

// Matches Java's MethodArgumentNotValidException handler: one aggregated
// "field: message, field: message" string in the same { message, status,
// timestamp } envelope, instead of ASP.NET's default ProblemDetails shape.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(", ", context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => $"{kvp.Key}: {e.ErrorMessage}")));

        var body = new FlemanApi.Exceptions.ErrorResponse(message, StatusCodes.Status400BadRequest);
        return new BadRequestObjectResult(body);
    };
});

// ---- EF Core / MySQL ------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<FlemanDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ---- AutoMapper (requirement #10) -----------------------------------------
builder.Services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());

// ---- JWT auth (requirement #2) + Google OAuth2 -----------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

var authBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });

// Google OAuth2 is only registered when real credentials are configured —
// GoogleOptions.Validate() throws eagerly on every request's handler
// resolution otherwise (RemoteAuthenticationOptions requires a non-empty
// ClientId), which would take down every endpoint, not just the OAuth ones.
var googleSection = builder.Configuration.GetSection("GoogleOAuth");
var googleClientId = googleSection["ClientId"];
var googleClientSecret = googleSection["ClientSecret"];
var googleOAuthConfigured = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

if (googleOAuthConfigured)
{
    authBuilder
        .AddCookie(GoogleSignInScheme) // transient cookie for the OAuth handshake only — no session auth is used afterwards
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId!;
            options.ClientSecret = googleClientSecret!;
            options.SignInScheme = GoogleSignInScheme;
            options.CallbackPath = "/signin-google";
            options.Scope.Add("email");
            options.Scope.Add("profile");
            // "email_verified" isn't mapped to a claim by the handler by
            // default — pull it straight off the raw user-info payload to
            // mirror CustomOAuth2UserService's emailVerified assignment.
            options.Events.OnCreatingTicket = context =>
            {
                if (context.User.TryGetProperty("email_verified", out var verifiedProp))
                {
                    context.Identity!.AddClaim(new System.Security.Claims.Claim("email_verified", verifiedProp.ToString()));
                }
                return Task.CompletedTask;
            };
            // Forces Google's account chooser every time, mirroring
            // CustomAuthorizationRequestResolver's prompt=select_account.
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
                return Task.CompletedTask;
            };
        });
}

builder.Services.AddAuthorization();

// ---- CORS (dev-time, mirrors CorsConfig.java) ------------------------------
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigin)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .AllowAnyHeader());
});

// ---- Security helpers -------------------------------------------------
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticatedUserAccessor, AuthenticatedUserAccessor>();

// ---- Swagger with JWT bearer support ---------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fleman API (.NET)", Version = "v1" });
    var jwtScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter a JWT issued by /api/auth/login, /register or /staff-login",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

// ---- QuestPDF (requirement: PDF invoice generation) ------------------
QuestPDF.Settings.License = LicenseType.Community;

// ---- Repositories / services are registered per-domain below ----------
// (populated incrementally as each domain — Location, Airport, Addon,
// Customer/Auth, Vehicle, Booking, Staff/Invoice/Email/PDF, AI, legacy
// proxy — is implemented.)
builder.Services.AddScoped(typeof(FlemanApi.Repository.IGenericRepository<,>), typeof(FlemanApi.Repository.GenericRepository<,>));

builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IAddonService, AddonService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<FlemanApi.Repository.IAvailabilityRepository, FlemanApi.Repository.AvailabilityRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection(MailSettings.SectionName));
builder.Services.AddSingleton<IEmailBackgroundQueue, EmailBackgroundQueue>();
builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
builder.Services.AddHostedService<EmailQueueHostedService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// Calls the Java backend's invoice-PDF endpoint instead of generating
// locally with QuestPDF — see JavaInvoicePdfService. Swap back to
// InvoicePdfService to generate locally without the Java backend running.
builder.Services.AddScoped<IInvoicePdfService, JavaInvoicePdfService>();
builder.Services.AddScoped<IStaffService, StaffService>();

builder.Services.Configure<AiSettings>(builder.Configuration.GetSection(AiSettings.SectionName));
builder.Services.AddScoped<IAiInsightsService, AiInsightsService>();

// Requirement #11 — register a typed HttpClient for the legacy Java microservice.
builder.Services.AddHttpClient<IJavaMicroserviceClient, JavaMicroserviceClient>(client =>
{
    var javaBaseUrl = builder.Configuration["JavaMicroservice:BaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(javaBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// ---- HTTP pipeline ------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlemanDbContext>();
    db.Database.Migrate();
    await FlemanApi.Data.DataSeeder.SeedAsync(db);
}

app.Run();
