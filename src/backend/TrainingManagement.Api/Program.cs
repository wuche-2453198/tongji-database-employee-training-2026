using System.Data;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oracle.ManagedDataAccess.Client;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Options;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Middlewares;
using TrainingManagement.Api.Repositories.Implementations;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Implementations;
using TrainingManagement.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<OracleOptions>(builder.Configuration.GetSection(OracleOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .SelectMany(item => item.Value!.Errors.Select(error =>
                    new ApiError(item.Key, error.ErrorMessage)))
                .ToArray();

            var response = ApiResponse<object>.Fail(
                "Validation failed.",
                context.HttpContext.TraceIdentifier,
                errors);

            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Training Management API",
        Version = "v1",
        Description = "Enterprise internal training management backend API."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Input: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicies.Frontend, policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (origins.Length == 0)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(origins);
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

const string DefaultDevelopmentSigningKey = "dev-only-training-management-signing-key-change-me";

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
    || (!builder.Environment.IsDevelopment()
        && string.Equals(jwtOptions.SigningKey, DefaultDevelopmentSigningKey, StringComparison.Ordinal)))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be provided by environment variables or appsettings.Local.json. Non-development environments cannot use an empty or default signing key.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (!context.Response.HasStarted)
                {
                    await context.HttpContext.WriteErrorResponseAsync(
                        StatusCodes.Status401Unauthorized,
                        "Authentication is required.");
                }
            },
            OnForbidden = async context =>
            {
                if (!context.Response.HasStarted)
                {
                    await context.HttpContext.WriteErrorResponseAsync(
                        StatusCodes.Status403Forbidden,
                        "Permission denied.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(RoleCodes.Admin));

    options.AddPolicy(AuthorizationPolicies.HrOrAdmin, policy =>
        policy.RequireRole(RoleCodes.Hr, RoleCodes.Admin));

    options.AddPolicy(AuthorizationPolicies.ManagerHrOrAdmin, policy =>
        policy.RequireRole(RoleCodes.DepartmentManager, RoleCodes.Hr, RoleCodes.Admin));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddScoped<IDbConnectionFactory, OracleConnectionFactory>();

builder.Services.AddScoped<IAuthRepository, OracleAuthRepository>();
builder.Services.AddScoped<IRoleRepository, OracleRoleRepository>();
builder.Services.AddScoped<IHealthRepository, OracleHealthRepository>();
builder.Services.AddScoped<ITrainingRequestRepository, OracleTrainingRequestRepository>();
builder.Services.AddScoped<ITrainerRepository, OracleTrainerRepository>();
builder.Services.AddScoped<ICourseRepository, OracleCourseRepository>();
builder.Services.AddScoped<IRegistrationRepository, OracleRegistrationRepository>();
builder.Services.AddScoped<IAttendanceRepository, OracleAttendanceRepository>();
builder.Services.AddScoped<IRatingRepository, OracleRatingRepository>();
builder.Services.AddScoped<ITestRepository, OracleTestRepository>();
builder.Services.AddScoped<ICertificateRepository, OracleCertificateRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<ITrainingRequestService, TrainingRequestService>();
builder.Services.AddScoped<ITrainerService, TrainerService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();

// Employee
builder.Services.AddScoped<IEmployeeRepository, OracleEmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// DepartmentTraining
builder.Services.AddScoped<IDepartmentTrainingRepository, OracleDepartmentTrainingRepository>();
builder.Services.AddScoped<IDepartmentTrainingService, DepartmentTrainingService>();

// Blacklist
builder.Services.AddScoped<IBlacklistRepository, OracleBlacklistRepository>();
builder.Services.AddScoped<IBlacklistService, BlacklistService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors(CorsPolicies.Frontend);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
