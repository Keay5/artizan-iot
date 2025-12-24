namespace Artizan.IoT.Settings;

public static class IoTSettings
{
    public const string Prefix = "Artizan.IoT";
    
    // TODO: 添加熔断Setting，搜索类 CircuitBreakerPostProcessorDecorator

    public static class Message
    {
        private const string MessagePrefix = Prefix + ".Message";

        public static class Cache
        {
            private const string CachePrefix = MessagePrefix + ".Cache";

            public const string Enabled = CachePrefix + ".Enabled";        // 启用
            public const string BatchSize = CachePrefix + ".BatchSize";        // 批量写入阈值
            public const string BatchTimeoutSeconds = CachePrefix + ".BatchTimeoutSeconds"; // 批量写入超时时间
            public const string LatestDataExpireSeconds = CachePrefix + ".LatestDataExpireSeconds"; // 设备最新值缓存过期时间
            public const string HistoryDataRetainSeconds = CachePrefix + ".HistoryDataRetainSeconds"; // 设备历史数据保留时长
        }
    }

}
