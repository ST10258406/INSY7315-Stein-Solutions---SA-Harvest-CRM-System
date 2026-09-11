namespace CRM.API.Extensions;

using CRM.API.Authorization;
using CRM.Application.Common.Behaviours;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Domain.Entities;
using CRM.Infrastructure.Auth;
using CRM.Infrastructure.Jobs;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Interceptors;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(CRM.Application.AssemblyReference).Assembly));

        services.AddValidatorsFromAssembly(
            typeof(CRM.Application.AssemblyReference).Assembly);

        services.AddAutoMapper(cfg => { }, typeof(CRM.Application.AssemblyReference).Assembly);

        // Pipeline behaviours — order matters: validation → logging → audit
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehaviour<,>));

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CrmDbContext>((sp, options) =>
            options.UseNpgsql(configuration.GetConnectionString("Default"))
                   .AddInterceptors(new UpdatedAtInterceptor()));
                   
        // Persistence abstractions. All scoped — same lifetime as CrmDbContext itself —
        // so every repository and the UnitOfWork resolved within one request share a
        // single DbContext instance. That is what lets a handler mutate entities via
        // several repositories and commit them in one IUnitOfWork.SaveChangesAsync call.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDonorRepository, DonorRepository>();
        services.AddScoped<IDonorDocumentRepository, DonorDocumentRepository>();
        services.AddScoped<IInteractionLogRepository, InteractionLogRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReportsRepository, ReportsRepository>();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        var jwtSecret = configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException(
                "JWT_SECRET is not set. Add it to .env (local) or Azure Key Vault (production).");

        // Bind from "Jwt" section and securely apply the signing key from .env 
        // without mutating the global IConfiguration object
        services.Configure<JwtSettings>(opts => 
        {
            configuration.GetSection("Jwt").Bind(opts);
            opts.SigningKey = jwtSecret;
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddCrmAuthorizationPolicies();
        services.AddSingleton<IAuthorizationHandler, DocumentTypeAuthorizationHandler>();


        // Hangfire — same Postgres connection string, own schema
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(configuration.GetConnectionString("Default"))));
        services.AddHangfireServer();

        // Service implementations
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IReportExportService, ReportExportService>(); // QuestPDF + ClosedXML, synchronous for now
        services.AddScoped<IEmailService, EmailService>();       // skeleton, SendGrid later
        services.AddScoped<INotificationService, NotificationService>();

        // Recurring background jobs
        services.AddScoped<ITaskDueNotificationJob, TaskDueNotificationJob>();

        return services;
    }

    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor(); // required by CurrentUserService

        services.AddControllers();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SA Harvest CRM API",
                Version = "v1"
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT token}"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Default")!, name: "postgresql");

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("PublicFormPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = 429,
                    code = "RATE_LIMITED",
                    message = "Too many requests. Please try again later.",
                    traceId = context.HttpContext.TraceIdentifier
                }, cancellationToken);
            };
        });

        return services;
    }
}
