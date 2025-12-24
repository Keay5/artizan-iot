namespace Artizan.IoT.Things.Caches.Enums;

/// <summary>
/// 缓存过期模式
/// 设计思路：
/// 1. 枚举化两种核心过期策略，避免字符串硬编码导致的解析错误
/// 2. 适配IoT属性数据的不同场景需求
/// 设计考量：
/// - 绝对过期：适用于实时性要求高的属性（如设备状态），避免缓存过期不及时
/// - 滑动过期：适用于高频访问的静态属性（如设备型号），减少缓存重建开销
/// </summary>
public enum CacheExpireMode
{
    /// <summary>
    /// 绝对过期：从缓存创建时开始计时，到达指定时间后立即过期
    /// </summary>
    Absolute,

    /// <summary>
    /// 滑动过期：每次访问缓存时重置过期时间，长时间未访问则过期
    /// </summary>
    Sliding
}
