using StackExchange.Redis;

namespace MyRedisSentinelWebApp.Services
{
    public class RedisService : IRedisService, IAsyncDisposable
    {
        private readonly ILogger<RedisService> _logger;
        private ConnectionMultiplexer? _redisConnection;
        private readonly RedisSentinelMonitorService _sentinelMonitorService;

        public RedisService(RedisSentinelMonitorService sentinelMonitorService, ILogger<RedisService> logger)
        {
            _sentinelMonitorService = sentinelMonitorService;
            _logger = logger;
            // 订阅主节点变更事件
            _sentinelMonitorService.MasterChanged += async master =>
            {
                _logger.LogInformation($"主节点切换到 {master}，准备重新连接...");
                await TryConnectToMasterAsync();
            };
        }

        public async Task InitializeAsync()
        {
            await TryConnectToMasterAsync();
        }

        private async Task TryConnectToMasterAsync()
        {
            var masterEndpoint = _sentinelMonitorService.GetCurrentMaster();
            if (masterEndpoint == null)
            {
                _logger.LogWarning("尚未检测到 Redis 主节点，稍后将继续尝试连接...");
                return; // 暂时不连接，等待后续尝试
            }
            try
            {
                // 如果存在原有的连接,关闭连接并清空原有资源
                if (_redisConnection != null && _redisConnection.IsConnected)
                {
                    _logger.LogInformation($"关闭原有Redis连接: {_redisConnection.Configuration}");
                    await _redisConnection.CloseAsync();
                    _redisConnection.Dispose();
                    _redisConnection = null;
                }
                _redisConnection = await ConnectionMultiplexer.ConnectAsync(masterEndpoint.ToString() ??
                        throw new NullReferenceException(nameof(masterEndpoint)));
                _logger.LogInformation($"已连接到 Redis 主节点：{masterEndpoint}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接到 Redis 主节点时发生错误");
            }
        }

        public async Task RetryConnectIfMasterChangesAsync()
        {
            var masterEndpoint = _sentinelMonitorService.GetCurrentMaster();
            if (masterEndpoint != null && (_redisConnection == null || !_redisConnection.IsConnected))
            {
                _logger.LogInformation($"检测到主节点切换到 {masterEndpoint}，重新连接...");
                await TryConnectToMasterAsync();
            }
        }

        public async Task<string?> GetStringAsync(string key)
        {
            try
            {
                if (_redisConnection == null)
                {
                    throw new InvalidOperationException("RedisService 尚未连接到主节点");
                }
                var db = await GetDatabaseAsync();
                return await db.StringGetAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis GET 操作失败");
                return null;
            }
        }

        public async Task SetStringAsync(string key, string value)
        {
            try
            {
                if (_redisConnection == null)
                {
                    throw new InvalidOperationException("RedisService 尚未连接到主节点");
                }
                var db = await GetDatabaseAsync();
                await db.StringSetAsync(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET 操作失败");
            }
        }

        public async Task<IDatabase> GetDatabaseAsync()
        {
            if (_redisConnection == null || !_redisConnection.IsConnected)
            {
                await ConnectToMasterAsync();
            }
            return _redisConnection!.GetDatabase();
        }

        private async Task ConnectToMasterAsync()
        {
            var masterEndpoint = _sentinelMonitorService.GetCurrentMaster();

            if (masterEndpoint != null)
            {
                try
                {
                    _redisConnection = await ConnectionMultiplexer.ConnectAsync(masterEndpoint.ToString() ??
                        throw new NullReferenceException(nameof(masterEndpoint)));
                    _logger.LogInformation($"已连接到 Redis 主节点：{masterEndpoint}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "连接 Redis 主节点失败");
                }
            }
            else
            {
                _logger.LogWarning("当前未检测到有效的 Redis 主节点");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_redisConnection != null)
            {
                await _redisConnection.CloseAsync();
                _redisConnection.Dispose();
                _redisConnection = null;
            }
        }
    }
}
