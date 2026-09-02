using Oracle.ManagedDataAccess.Core;
using System.Data;
using TrainingManagement.Api.Repositories.Implementations;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Implementations;
using TrainingManagement.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDbConnection>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("OracleConnection");
    return new OracleConnection(connectionString);
});

builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
