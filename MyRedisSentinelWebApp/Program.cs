using MyRedisSentinelWebApp.Logging;
using MyRedisSentinelWebApp.Models;
using MyRedisSentinelWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加配置文件
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 注册 Redis 哨兵服务和 Redis 操作服务
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<RedisSentinelMonitorService>(); // 哨兵监控服务
builder.Services.AddSingleton<IRedisService, RedisService>(); // Redis 操作服务
builder.Services.AddHostedService(provider => provider.GetRequiredService<RedisSentinelMonitorService>()); // 启用后台服务

// 注册日志
var log4NetConfig = Path.Combine(Directory.GetCurrentDirectory(), "log4net.config");
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddProvider(new Log4NetProvider(log4NetConfig));
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var redisService = app.Services.GetRequiredService<IRedisService>() as RedisService;
if (redisService != null)
{
    await redisService.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
