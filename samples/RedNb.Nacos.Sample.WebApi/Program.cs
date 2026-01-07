using RedNb.Nacos;
using RedNb.Nacos.AspNetCore;
using RedNb.Nacos.AspNetCore.Hosting;
using RedNb.Nacos.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 加载本地配置文件（包含敏感信息，不会提交到 Git）
// 本地配置文件会覆盖 appsettings.json 和 appsettings.{Environment}.json 中的配置
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

// 从 appsettings.json 配置节读取 Nacos 配置
builder.Services.AddRedNbNacosAspNetCore(builder.Configuration, "RedNb:Nacos");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
