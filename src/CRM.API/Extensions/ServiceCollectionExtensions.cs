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
using CRM.Infrastructure.Services.Email;
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
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<IDonorRepository, DonorRepository>();
        services.AddScoped<IDonorDocumentRepository, DonorDocumentRepository>();
        services.AddScoped<IInteractionLogRepository, InteractionLogRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReportsRepository, ReportsRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IUserPasswordHasher, UserPasswordHasher>();
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

                // The signature/lifetime checks above only prove the token hasn't been
                // tampered with and hasn't expired — they say nothing about whether the
                // user is still active or still holds the roles baked into the token at
                // login time. Re-check both against the current DB state on every
                // authenticated request, so a deactivation or role change takes effect
                // immediately instead of waiting up to AccessTokenExpiryMinutes.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                        {
                            context.Fail("Token has no valid subject.");
                            return;
                        }

                        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                        var user = await users.GetByIdWithRolesReadOnlyAsync(userId, context.HttpContext.RequestAborted);

                        if (user is null || !user.IsActive)
                        {
                            context.Fail("User is deactivated or no longer exists.");
                            return;
                        }

                        // Rebuild the role claims from the DB rather than trusting whatever
                        // was embedded in the token — a ChangeUserRole call doesn't (and
                        // can't) reach out and mint the holder a new access token.
                        var identity = (ClaimsIdentity)context.Principal!.Identity!;
                        foreach (var staleRoleClaim in identity.FindAll(ClaimTypes.Role).ToList())
                        {
                            identity.RemoveClaim(staleRoleClaim);
                        }
                        foreach (var roleName in user.UserRoles.Select(ur => ur.Role.Name))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                        }
                    }
                };
            });

        services.AddCrmAuthorizationPolicies();
        services.AddSingleton<IAuthorizationHandler, DocumentTypeAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, RoleAssignmentAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, UserTargetAuthorizationHandler>();

        var brevoApiKey = configuration["BREVO_API_KEY"]
            ?? throw new InvalidOperationException(
                "BREVO_API_KEY is not set. Add it to .env (local) or Azure Key Vault (production).");

        services.Configure<BrevoSettings>(opts =>
        {
            configuration.GetSection("Brevo").Bind(opts);
            opts.ApiKey = brevoApiKey;
        });

        services.AddHttpClient<IEmailService, EmailService>(client =>
        {
            client.BaseAddress = new Uri("https://api.brevo.com/v3/");
            client.DefaultRequestHeaders.Add("api-key", brevoApiKey);
            client.DefaultRequestHeaders.Add("accept", "application/json");
        });


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

        // All rate limit policies (global + named per-tier) are configured in
        // RateLimitingExtensions.cs, not inline here.
        services.AddPublicApiRateLimiting();
        services.AddAuthenticatedApiRateLimiting(configuration);

        return services;
    }
}
