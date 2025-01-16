using MyRedisSentinelWebApp.Logging;
using MyRedisSentinelWebApp.Models;
using MyRedisSentinelWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ��������ļ�
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// ע�� Redis �ڱ������ Redis ��������
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<RedisSentinelMonitorService>(); // �ڱ���ط���
builder.Services.AddSingleton<IRedisService, RedisService>(); // Redis ��������
builder.Services.AddHostedService(provider => provider.GetRequiredService<RedisSentinelMonitorService>()); // ���ú�̨����

// ע����־
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
