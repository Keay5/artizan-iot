using System;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Tracings
{
    /// <summary>
    /// 跟踪ID生成器的简单实现，基于Guid.NewGuid()。
    /// 其它更复杂的实现可以继承此类并重写Create()方法。
    /// </summary>
    public class SimpleTrackIdGenerator : ITrackIdGenerator, ISingletonDependency
    {
        /// <summary>
        /// 内存高效：全局只有一个实例，避免频繁创建 / 销毁SimpleGuidGenerator对象（GUID 生成是高频操作，减少对象开销）。
        /// 线程安全：静态只读字段在 CLR 初始化阶段就完成赋值（类加载时创建实例），天然线程安全，无需加锁。
        /// 使用便捷：无需通过 DI 容器或手动new，直接通过SimpleGuidGenerator.Instance.Create() 即可调用，适合框架内部底层使用。
        /// 无依赖：不依赖 DI 容器，即使在 DI 初始化完成前也能使用（比如框架启动初期的 GUID 生成）
        /// 
        /// </summary>
        public static SimpleTrackIdGenerator Instance { get; } = new SimpleTrackIdGenerator();

        /// <summary>
        /// virtual 允许子类重写Create()方法，
        /// </summary>
        /// <returns></returns>
        public virtual string Create()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
