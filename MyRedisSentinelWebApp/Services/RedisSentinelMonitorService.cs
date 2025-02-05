using Microsoft.Extensions.Options;
using MyRedisSentinelWebApp.Models;
using StackExchange.Redis;
using System.Net;

namespace MyRedisSentinelWebApp.Services
{
    public class RedisSentinelMonitorService : BackgroundService
    {
        private readonly ILogger<RedisSentinelMonitorService> _logger;
        private readonly RedisSettings _redisSettings;
        private EndPoint? _currentMaster;
        public event Action<EndPoint>? MasterChanged; // 主节点切换事件

        public RedisSentinelMonitorService(IOptions<RedisSettings> redisSettings, ILogger<RedisSentinelMonitorService> logger)
        {
            _redisSettings = redisSettings.Value;
            _logger = logger;
        }

        public EndPoint? GetCurrentMaster()
        {
            return _currentMaster;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var newMaster = await GetMasterFromSentinelAsync();

                    if (newMaster != null &&
                        (_currentMaster == null || !_currentMaster.Equals(newMaster)))
                    {
                        _currentMaster = newMaster;
                        _logger.LogInformation($"Redis 主节点已切换到：{_currentMaster}");
                        MasterChanged?.Invoke(newMaster!); // 触发主节点切换事件
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "哨兵监控服务发生异常");
                }

                // 每隔指定时间检查一次
                await Task.Delay(TimeSpan.FromSeconds(_redisSettings.ConnectionRetryDelaySeconds), stoppingToken);
            }
        }

        private async Task<EndPoint?> GetMasterFromSentinelAsync()
        {
            foreach (var sentinelNode in _redisSettings.SentinelNodes)
            {
                try
                {
                    var sentinelConfig = new ConfigurationOptions
                    {
                        EndPoints = { sentinelNode },
                        AbortOnConnectFail = false,
                        KeepAlive = 180,
                        SyncTimeout = 10000, // 增加超时时间
                        AsyncTimeout = 10000,
                        ConnectTimeout = 10000, // 连接超时
                        DefaultVersion = new Version(6, 0),
                        ClientName = "MyRedisApp",
                        CommandMap = CommandMap.Sentinel,
                        AllowAdmin = true
                    };

                    using var sentinelConnection = await ConnectionMultiplexer.ConnectAsync(sentinelConfig);
                    var server = sentinelConnection.GetServer(sentinelConfig.EndPoints.First());
                    var masterInfo = await server.SentinelGetMasterAddressByNameAsync(_redisSettings.ServiceName);
                    if (masterInfo != null)
                    {
                        return masterInfo;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"无法连接到哨兵节点 {sentinelNode}: {ex.Message}");
                }
            }
            return null;
        }
    }
}
