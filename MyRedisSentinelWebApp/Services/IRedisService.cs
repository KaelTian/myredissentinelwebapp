using StackExchange.Redis;

namespace MyRedisSentinelWebApp.Services
{
    public interface IRedisService
    {
        Task<string?> GetStringAsync(string key);
        Task SetStringAsync(string key, string value);
        Task<IDatabase> GetDatabaseAsync(); // 获取数据库对象，用于其他复杂操作
    }
}
