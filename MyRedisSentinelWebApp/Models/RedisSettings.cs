namespace MyRedisSentinelWebApp.Models
{
    public class RedisSettings
    {
        public string[] SentinelNodes { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 哨兵分组名字
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;
        public int ConnectionRetryDelaySeconds { get; set; } = 5;
        public int DefaultDatabase { get; set; } = 0;
    }
}
