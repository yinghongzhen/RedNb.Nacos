using RedNb.Nacos;
using RedNb.Nacos.AspNetCore;
using RedNb.Nacos.AspNetCore.Hosting;
using RedNb.Nacos.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 方式一：从 appsettings.json 配置
// ============================================
// 添加 Nacos 配置源（从 Nacos 读取配置）
// builder.Configuration.AddRedNbNacosConfiguration(
//     serverAddresses: "http://localhost:8848",
//     dataId: "sample-webapi.json",
//     group: "DEFAULT_GROUP",
//     namespaceId: "public",
//     username: "nacos",
//     password: "nacos"
// );

// ============================================
// 方式二：使用 appsettings.json 中的配置节
// ============================================
// builder.Configuration.AddRedNbNacosConfiguration(builder.Configuration, "RedNb:Nacos");

// ============================================
// 方式三：直接代码配置
// ============================================
builder.Services.AddRedNbNacosAspNetCore(options =>
{
    // 服务器地址
    options.ServerAddresses = ["http://localhost:8848"];
    options.Namespace = "public";

    // 认证
    options.UserName = "nacos";
    options.Password = "nacos";

    // 服务注册配置
    options.Naming.ServiceName = "sample-webapi";
    options.Naming.GroupName = "DEFAULT_GROUP";
    options.Naming.ClusterName = "DEFAULT";
    options.Naming.RegisterEnabled = true;
    options.Naming.Weight = 1.0;
    options.Naming.Metadata["version"] = "1.0.0";
    options.Naming.Metadata["env"] = "dev";

    // 配置中心配置
    options.Config.Listeners.Add(new()
    {
        DataId = "sample-webapi.json",
        Group = "DEFAULT_GROUP"
    });
});

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
