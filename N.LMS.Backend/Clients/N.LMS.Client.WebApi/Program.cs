using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using N.LMS.Client.WebApi;
using N.LMS.Client.WebApi.Middleware;
using N.LMS.Common.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(json =>
    {
        json.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        json.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsPolicy = "n-lms-default";
builder.Services.AddCors(options => options.AddPolicy(corsPolicy, p => p
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials()));

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "n-lms";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "n-lms-client";
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DEV-ONLY-KEY-replace-in-production-with-secret-manager-aaaaaaa";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Bundle every layer's registrations and apply them.
var registrationBuilder = new RegistrationBuilder(Hosting.Registrations);
ServiceRegistrar.RegisterServices(builder.Services, registrationBuilder);

var app = builder.Build();

// Make the root provider available to ServiceProxyGenerator.
ServiceProxyHost.Configure(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
