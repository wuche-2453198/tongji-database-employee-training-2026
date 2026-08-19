using TrainingManagement.Api.Common;
using TrainingManagement.Api.Repositories.Implementations;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Implementations;
using TrainingManagement.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<OracleConnectionFactory>();
builder.Services.AddScoped<ITrainingRequestRepository, TrainingRequestRepository>();
builder.Services.AddScoped<ITrainingRequestService, TrainingRequestService>();

//添加认证（虽然还没实现，但先加上占位，避免后续忘记）
builder.Services.AddAuthentication().AddJwtBearer();  // （占位，后续配置）

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();   // 新增（启用认证中间件）
app.UseAuthorization();
app.MapControllers();
app.Run();

//var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//app.UseHttpsRedirection();
//app.UseAuthorization();
//app.MapControllers();
//app.Run();