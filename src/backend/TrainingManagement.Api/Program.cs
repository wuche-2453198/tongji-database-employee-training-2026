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

// 应用启动入口：先注册配置和服务，再组装 HTTP 请求处理管道。
var builder = WebApplication.CreateBuilder(args);

// 允许 Dapper 将数据库下划线列名映射到 C# 属性名。
DefaultTypeMap.MatchNamesWithUnderscores = true;

// 后加入的配置优先级更高：本地文件之后再读取环境变量和命令行参数。
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// 将配置绑定为强类型选项，业务类通过 IOptions 获取配置。
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<OracleOptions>(builder.Configuration.GetSection(OracleOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

// 将模型校验失败转换为统一错误响应，避免前端处理两套 JSON 结构。
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

// 注册 Swagger 接口说明及 Bearer 认证入口，便于联调。
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

// 按配置允许前端来源；来源列表为空时，当前实现允许任意来源。
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

// 启动时检查签名密钥：所有环境都禁止空密钥，非开发环境还禁止示例密钥。
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

// JWT 验证签发方、受众、签名和有效期，允许两分钟时钟偏差。
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

        // 将认证失败和权限不足分别转换为统一的 401、403 JSON 响应。
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

// 角色策略用于控制器授权，与返回给前端的权限代码列表分开管理。
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(RoleCodes.Admin));

    options.AddPolicy(AuthorizationPolicies.HrOrAdmin, policy =>
        policy.RequireRole(RoleCodes.Hr, RoleCodes.Admin));

    options.AddPolicy(AuthorizationPolicies.ManagerHrOrAdmin, policy =>
        policy.RequireRole(RoleCodes.DepartmentManager, RoleCodes.Hr, RoleCodes.Admin));
});

// 服务和仓储通过接口注入；Scoped 对象在同一请求内复用，令牌服务为单例。
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

// 员工模块的仓储与业务服务。
builder.Services.AddScoped<IEmployeeRepository, OracleEmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// 部门培训预算模块的仓储与业务服务。
builder.Services.AddScoped<IDepartmentTrainingRepository, OracleDepartmentTrainingRepository>();
builder.Services.AddScoped<IDepartmentTrainingService, DepartmentTrainingService>();

// 黑名单模块的仓储与业务服务。
builder.Services.AddScoped<IBlacklistRepository, OracleBlacklistRepository>();
builder.Services.AddScoped<IBlacklistService, BlacklistService>();

var app = builder.Build();

// 开发环境或显式开启配置时提供 Swagger 页面。
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 顺序影响行为：先捕获异常，再处理跨域、验证身份、检查权限，最后分发到控制器。
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors(CorsPolicies.Frontend);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
