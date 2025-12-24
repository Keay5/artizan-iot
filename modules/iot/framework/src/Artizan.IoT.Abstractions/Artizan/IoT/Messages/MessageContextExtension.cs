using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Artizan.IoT.Messages;

/// <summary>
/// 上下文扩展字段容器（线程安全版
/// 避免频繁修改上下文类，用键值对承载自定义数据
/// 设计思想：
/// 1. 采用ConcurrentDictionary保证多线程场景下的字段操作安全，避免锁竞争；
/// 2. 遵循.NET官方Dispose模式，管理扩展字段中实现IDisposable的托管资源，防止内存泄漏；
/// 3. 极简API设计（Set/Get/Remove/ToDictionary），降低使用成本；
/// 4. 防御式编程：访问已释放对象时抛出ObjectDisposedException，避免非法操作。
/// 设计模式：
/// - 资源管理模式（.NET官方Dispose模式）：区分手动/GC触发的资源释放场景；
/// - 容器模式：封装键值对存储，对外暴露统一操作接口。
/// </summary>
public class MessageContextExtension : IDisposable
{
    #region 核心字段
    /// <summary>
    /// 线程安全的扩展字段存储容器（多线程读写无锁竞争）
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _data = new ConcurrentDictionary<string, object>();

    /// <summary>
    /// 资源释放标记（防止重复释放/访问已释放对象）
    /// </summary>
    private bool _disposed = false;
    #endregion

    #region 扩展字段操作API（线程安全 + 已释放校验）
    /// <summary>
    /// 设置扩展字段（线程安全）
    /// </summary>
    /// <param name="key">字段键（非空）</param>
    /// <param name="value">字段值（支持实现IDisposable的对象）</param>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    /// <exception cref="ArgumentNullException">key为空时抛出</exception>
    public void Set(string key, object value)
    {
        CheckDisposed();

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "扩展字段键不能为空");
        }

        _data[key] = value;
    }

    /// <summary>
    /// 获取扩展字段（泛型版，避免类型转换）
    /// </summary>
    /// <typeparam name="T">字段值类型</typeparam>
    /// <param name="key">字段键</param>
    /// <returns>匹配类型的值（无匹配时返回default）</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public T? Get<T>(string key)
    {
        CheckDisposed();

        if (_data.TryGetValue(key, out var value) && value is T tValue)
        {
            return tValue;
        }

        return default;
    }

    /// <summary>
    /// 移除扩展字段（线程安全）
    /// </summary>
    /// <param name="key">字段键</param>
    /// <returns>移除成功返回true，否则返回false</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public bool Remove(string key)
    {
        CheckDisposed();

        return _data.TryRemove(key, out _);
    }

    /// <summary>
    /// 转换为只读字典（用于日志输出，避免外部修改内部数据）
    /// </summary>
    /// <returns>只读扩展字段字典</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public IReadOnlyDictionary<string, object> ToDictionary()
    {
        CheckDisposed();

        return _data;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 检查对象是否已释放，若已释放则抛出异常
    /// 设计理念：防御式编程，提前暴露非法操作，避免隐藏bug
    /// </summary>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MessageContextExtension), "扩展字段容器已释放，禁止执行操作");
        }
    }
    #endregion

    #region .NET 官方 Dispose 模式实现
    /// <summary>
    /// 核心资源释放方法（虚方法，支持子类扩展）
    /// 设计理念：
    /// 1. disposing=true：手动释放托管资源（遍历扩展字段释放IDisposable对象）；
    /// 2. disposing=false：仅处理非托管资源（当前类无非托管资源，预留扩展位）；
    /// 3. 线程安全：释放时先标记_disposed，防止多线程并发操作冲突。
    /// </summary>
    /// <param name="disposing">true=手动调用Dispose()，false=GC析构函数触发</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        // ========== 1. 托管资源释放（仅disposing=true时执行） ==========
        if (disposing)
        {
            // 遍历扩展字段，释放所有实现IDisposable的托管资源（线程安全）
            foreach (var kv in _data)
            {
                if (kv.Value is IDisposable disposableValue)
                {
                    try
                    {
                        disposableValue.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // 捕获单个资源释放异常，不影响其他资源释放
                        System.Diagnostics.Debug.WriteLine($"释放扩展字段[{kv.Key}]失败：{ex.Message}");
                    }
                }
            }

            // 清空字典，加速GC回收
            _data.Clear();
        }

        // ========== 2. 非托管资源释放（无论disposing值都执行） ==========
        // 当前类基于纯托管的ConcurrentDictionary实现，无非托管资源，预留扩展位
        // 示例：若未来引入非托管句柄，在此处释放：
        // if (_unmanagedHandle != IntPtr.Zero) { NativeMethods.CloseHandle(_unmanagedHandle); }

        // 标记资源已释放
        _disposed = true;
    }

    /// <summary>
    /// 公共释放入口（符合IDisposable接口规范）
    /// 设计规范：.NET官方要求IDisposable接口必须实现公共无参Dispose方法
    /// </summary>
    public void Dispose()
    {
        // 调用核心释放方法，标记为手动释放（disposing=true）
        Dispose(disposing: true);

        // 通知GC无需调用析构函数（已手动释放所有资源，提升性能）
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 析构函数（GC兜底释放）
    /// 设计理念：防止开发者未手动调用Dispose()时，扩展字段中的非托管资源泄漏
    /// 注意：仅处理非托管资源，禁止访问托管资源（ConcurrentDictionary可能已被GC回收）
    /// </summary>
    ~MessageContextExtension()
    {
        Dispose(disposing: false);
    }
    #endregion
}
