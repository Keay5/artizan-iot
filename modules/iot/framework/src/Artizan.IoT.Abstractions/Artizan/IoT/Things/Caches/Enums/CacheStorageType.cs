namespace Artizan.IoT.Things.Caches.Enums;

/// <summary>
/// 缓存存储类型
/// 设计思路：
/// 1. 枚举支持的存储介质，配合策略模式实现存储层解耦
/// 2. 预留扩展空间（如后续可添加MongoDB/ETCD）
/// 设计考量：
/// - 本地内存：单实例部署、低延迟场景（开发/测试环境）
/// - Redis：分布式部署、多实例共享缓存场景（生产环境）
/// </summary>
public enum CacheStorageType
{
    /// <summary>
    /// 本地内存缓存（ConcurrentDictionary实现，线程安全）
    /// </summary>
    LocalMemory,

    /// <summary>
    /// Redis分布式缓存（支持集群部署，多实例共享）
    /// </summary>
    Redis
}
