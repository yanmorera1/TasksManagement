using System.Text;
using Ardalis.GuardClauses;
using Carter;
using dotenv.net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TasksManagement.API.Common.Behaviors;
using TasksManagement.API.Common.Schemas;
using TasksManagement.API.Contracts;
using TasksManagement.API.Data;
using TasksManagement.API.Data.Interceptors;
using TasksManagement.API.Models;
using TasksManagement.API.Models.Entities;
using TasksManagement.API.Services;

namespace TasksManagement.API;
public static class DependencyInjection
{
    public static IServiceCollection ConfigureApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(Program).Assembly;

        //Configuring MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        //Configuring FluentValidations
        services.AddValidatorsFromAssembly(assembly);

        services.AddCarter();

        //Configuring DbContext
        services.ConfigureDbContext(configuration);

        services.AddCustomAuthentication(configuration);
        services.AddAuthorizationBuilder();

        services
            .AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddSingleton(TimeProvider.System);

        //Services
        services.AddTransient<IAuthService, AuthService>();
        //services.AddTransient<IIdentityService, IdentityService>();
        services.AddSingleton<ISseHolder, SseHolder>();
        services.AddScoped<ICurrentRequest, CurrentRequest>();

        services.ConfigureSettings<JwtConfiguration>(configuration);


        services.AddHttpContextAccessor();


        return services;
    }

    public static IServiceCollection ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Guard.Against.Null(connectionString, message: "Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.EnableSensitiveDataLogging();
            options.UseSqlServer(connectionString);
        });

        return services;
    }

    private static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["JwtConfiguration:Secret"];
        Guard.Against.NullOrEmpty(secret, message: "No key found by 'JwtConfiguration:Secret' name");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = false,
            ClockSkew = TimeSpan.Zero,
        };

        services.AddSingleton(tokenValidationParameters);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = tokenValidationParameters;
        }).AddScheme<AuthenticationSchemeOptions, CustomAuthenticationScheme>(CustomAuthenticationScheme.CustomScheme, _ =>
        {
        });

        return services;
    }

    public static void ConfigureAppsettingsEnvironment(this WebApplicationBuilder builder)
    {
        //Load environment variables
        DotEnv.Load();

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true);
        });

        builder.Host.UseSerilog((context, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration));
    }

    private static void ConfigureSettings<T>(this IServiceCollection services, IConfiguration config, string key = null)
        where T : class
    {
        if (!string.IsNullOrEmpty(key))
        {
            services.Configure<T>(x => { config.GetSection(key).Bind(x); });
        }
        else
        {
            services.Configure<T>(x => { config.GetSection(typeof(T).Name).Bind(x); });
        }
    }
}
