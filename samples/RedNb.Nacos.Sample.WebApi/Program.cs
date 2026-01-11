using RedNb.Nacos.DependencyInjection;
using RedNb.Nacos.AspNetCore.Configuration;
using RedNb.Nacos.AspNetCore.HealthChecks;
using RedNb.Nacos.AspNetCore.ServiceRegistry;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;

var builder = WebApplication.CreateBuilder(args);

// Add Nacos configuration as a source
builder.Configuration.AddNacosConfiguration(source =>
{
    source.Options.ServerAddresses = "localhost:8848";
    source.Options.Username = "nacos";
    source.Options.Password = "nacos";
    source.Options.Namespace = "";
    source.ConfigItems.Add(new NacosConfigurationItem { DataId = "app-config", Group = "DEFAULT_GROUP" });
    source.ConfigItems.Add(new NacosConfigurationItem { DataId = "db-config", Group = "DEFAULT_GROUP", Optional = true });
});

// Add Nacos services using DI extensions
builder.Services.AddNacos(options =>
{
    options.ServerAddresses = "localhost:8848";
    options.Username = "nacos";
    options.Password = "nacos";
    options.Namespace = "";
    options.DefaultTimeout = 5000;
});

// Add Nacos health checks
builder.Services.AddHealthChecks()
    .AddNacos();

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// Use Nacos service registry for automatic registration
app.UseNacosServiceRegistry(
    serviceName: "sample-webapi",
    port: 5000,
    metadata: new Dictionary<string, string>
    {
        { "version", "1.0.0" },
        { "env", app.Environment.EnvironmentName }
    });

app.Run();
