namespace MyRedisSentinelWebApp.Models
{
    public class RedisSettings
    {
        public string[] SentinelNodes { get; set; } = Array.Empty<string>();
        public string MasterName { get; set; } = string.Empty;
        public int ConnectionRetryDelaySeconds { get; set; } = 5;
        public int DefaultDatabase { get; set; } = 0;
    }
}
