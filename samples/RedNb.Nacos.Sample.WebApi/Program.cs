using RedNb.Nacos;
using RedNb.Nacos.AspNetCore;
using RedNb.Nacos.AspNetCore.Hosting;
using RedNb.Nacos.Configuration;

var builder = WebApplication.CreateBuilder(args);

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
