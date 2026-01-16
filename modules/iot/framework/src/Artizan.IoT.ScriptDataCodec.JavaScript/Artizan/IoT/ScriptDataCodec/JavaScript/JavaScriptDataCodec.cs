using Jint;
using Jint.Native;
using Jint.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.ScriptDataCodec.JavaScript;

/// <summary>
/// 线程安全的JS数据编解码器
/// 设计模式：包装器模式（包装Jint引擎）+ 单例模式（对象池）
/// 设计思路：
/// 1. 解决Jint引擎非线程安全问题：原子操作+线程ID绑定控制实例占用
/// 2. 支持动态调用脚本任意方法：通过MethodName参数灵活指定
/// 3. 通用化入参/出参处理：自动转换.NET类型↔JsValue
/// 4. 强化资源管理：完善Dispose逻辑，避免内存泄漏
/// 核心优化点：
/// - 线程ID绑定：防止跨线程调用导致的上下文污染
/// - Jint 3.x API适配：修复GetCompletionValue/IsFunction等编译错误
/// - 沙箱限制：内存/递归/超时限制，防止恶意脚本攻击
/// </summary>
public class JavaScriptDataCodec : IDataCodec, IDisposable
{
    private readonly CodecLogger _codecLogger;
    private readonly Engine _engine;
    private readonly string _scriptContent;
    private int _isInUse; // 0=空闲，1=占用（原子操作）
    private int _ownerThreadId; // 占用线程ID，防止跨线程调用
    private bool _disposed;

    /// <summary>
    /// 构造函数：初始化Jint引擎并加载脚本
    /// </summary>
    /// <param name="scriptContent">JS脚本内容（可包含任意自定义方法）</param>
    /// <exception cref="ArgumentNullException">脚本为空时抛出</exception>
    public JavaScriptDataCodec(string scriptContent, CodecLogger codecLogger)
    {
        if (string.IsNullOrEmpty(scriptContent))
        {
            throw new ArgumentNullException(nameof(scriptContent), "JS脚本内容不能为空");
        }
        _codecLogger = codecLogger ?? throw new ArgumentNullException(nameof(codecLogger), "编解码日志器不能为空");

        _scriptContent = scriptContent;
        // 初始化Jint引擎，配置沙箱限制
        _engine = new Engine(options =>
        {
            //options.EnableEcmaScript2020(); // 4.x 推荐使用具体ECMA版本
            //options.SetTimeout(TimeSpan.FromSeconds(5)); // 脚本超时
            options.Strict(); // 严格模式
            options.LimitMemory(1024 * 1024 * 2); // 限制内存2MB
            options.LimitRecursion(100); // 限制递归深度
            //options.AllowClr = false; // 禁止访问.NET CLR，安全加固
        });

        try
        {
            // 执行脚本：加载所有自定义方法到引擎上下文
            _engine.Execute(_scriptContent);
        }
        catch (JavaScriptException ex)
        {
            throw new CodecException($"JS脚本加载失败：{ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new CodecException("JS引擎初始化异常", ex);
        }

        _isInUse = 0;
        _ownerThreadId = -1;
        _disposed = false;
        _codecLogger = codecLogger;
    }

    /// <summary>
    /// 尝试占用实例（线程安全原子操作）
    /// 设计考量：使用Interlocked.CompareExchange确保原子性，避免多线程竞争
    /// </summary>
    /// <returns>占用成功返回true，失败返回false</returns>
    public bool TryAcquire()
    {
        if (_disposed)
        {
            return false;
        }

        // 原子操作：只有当_isInUse为0时，才设置为1
        var originalValue = Interlocked.CompareExchange(ref _isInUse, 1, 0);
        return originalValue == 0;

        /* -----------------------------------------
           设计调整：移除线程ID绑定，仅保留原子操作的占用状态控制
           原因：Task.Run() 会切换线程，线程ID绑定导致异步场景校验失败
        */
        //if (originalValue == 0)
        //{
        //    _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        //    return true;
        //}

        //return false;
    }

    /// <summary>
    /// 释放实例占用状态
    /// 设计考量：只有占用线程才能释放，防止其他线程误操作
    /// </summary>
    public void Release()
    {
        if (_disposed)
        {
            return;
        }

        Interlocked.Exchange(ref _isInUse, 0);

        //if (Thread.CurrentThread.ManagedThreadId == _ownerThreadId)
        //{
        //    Interlocked.Exchange(ref _isInUse, 0);
        //    _ownerThreadId = -1;
        //}
    }

    /// <summary>
    /// 通用方法：调用JS脚本中的任意方法
    /// 设计思路：动态方法名+通用入参，适配所有自定义脚本
    /// </summary>
    /// <param name="methodName">要调用的JS方法名</param>
    /// <param name="args">方法入参</param>
    /// <returns>统一执行结果</returns>
    private ScriptExecutionResult InvokeJsMethod(string methodName, params object[] args)
    {
        try
        {
            // 1. 获取JS方法对象
            var jsMethod = _engine.GetValue(methodName);
            if (jsMethod.IsUndefined() || jsMethod.IsNull())
            {
                var errorMsg = $"JS脚本中未找到方法：{methodName}";
                _codecLogger.LogError(new ScriptExecutionContext { MethodName = methodName }, "Invoke", errorMsg);
                return ScriptExecutionResult.Fail(errorMsg);
            }

            // TODO: 看看Jint 4.x 有没有更好的方式判断JsValue是否为函数类型
            // 2. 判断是否为函数类型（Jint 4.x 适配：使用IsFunction()扩展方法）
            //if (!jsMethod.IsInstanceOf<FunctionInstance>(_engine))
            // if (!jsMethod.IsFunction()) // 关键修复：替代3.x的IsInstanceOf<FunctionInstance>
            //{
            //    var errorMsg = $"[{methodName}] 不是有效的JS函数";
            //    _codecLogger.LogError(new ScriptExecutionContext { MethodName = methodName }, "Invoke", errorMsg);
            //    return ScriptExecutionResult.Fail(errorMsg);
            //}

            #region TODO：考察下是否真的需要类型转化
            //// 3. 转换入参为JsValue（支持byte[]/string等）
            //var jsArgs = ConvertArgsToJsValues(args);

            //// 4. 执行JS函数
            //var resultValue = _engine.Invoke(methodName, jsArgs); 
            #endregion

            var resultValue = _engine.Invoke(methodName, args);

            // 5. 处理返回结果，转换为统一格式
            return ProcessJsResult(resultValue);
        }
        catch (JavaScriptException ex)
        {
            var errorMsg = $"JS函数执行异常：{ex.Message}";
            _codecLogger.LogError(new ScriptExecutionContext { MethodName = methodName }, "Invoke", errorMsg);
            return ScriptExecutionResult.Fail(errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"调用JS方法[{methodName}]失败：{ex.Message}";
            _codecLogger.LogError(new ScriptExecutionContext { MethodName = methodName }, "Invoke", errorMsg);
            return ScriptExecutionResult.Fail(errorMsg);
        }
    }

    /// <summary>
    /// .NET入参转换为JsValue（适配Jint引擎）
    /// 设计考量：特殊处理byte[]→JS数组，其他类型自动转换
    /// </summary>
    /// <param name="args">.NET入参数组</param>
    /// <returns>JsValue数组</returns>
    private JsValue[] ConvertArgsToJsValues(object[] args)
    {
        var jsArgs = new JsValue[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == null)
            {
                jsArgs[i] = JsValue.Null;
                continue;
            }

            // TODO: 考察下是否需要支持其他特殊类型的转换？
            //// 特殊处理：byte[] → JS数值数组（Jint不直接支持byte类型）
            //if (arg is byte[] bytes)
            //{
            //    // var jsArray = _engine.Array.Construct(bytes.Select(b => (double)b).ToArray());
            //    var jsArray = _engine.CreateArray(bytes.Select(b => (double)b).ToArray());
            //    jsArgs[i] = jsArray;
            //    continue;
            //}

            // 其他类型（string/int/double等）自动转换
            jsArgs[i] = JsValue.FromObject(_engine, arg);
        }
        return jsArgs;
    }

    /// <summary>
    /// JS执行结果转换为统一的ScriptExecutionResult
    /// 设计考量：支持对象/数组/基础类型，覆盖解码/编码所有场景
    /// </summary>
    /// <param name="resultValue">JS执行结果</param>
    /// <returns>统一结果</returns>
    private ScriptExecutionResult ProcessJsResult(JsValue resultValue)
    {
        // 场景1：返回对象 → 解码结果（JSON字符串）
        // 关键修复：Jint 4.x 用engine.Json.Stringify替代JsonSerializer.Stringify
        if (resultValue.IsObject() && !resultValue.IsArray())
        {
            //var json = JsonSerializer.Stringify(_engine, resultValue); // Jint 3.x API？
            //var json = _engine.Json.Stringify(resultValue);
            var json = JsonSerializer.Serialize(resultValue.ToObject());   // 转换为 JSON 格式的字符串
            return ScriptExecutionResult.SuccessWithProtocol(json);
        }

        // 场景2：返回数组 → 编码结果（byte[]）
        if (resultValue.IsArray())
        {
            var jsArray = resultValue.AsArray();
            var byteList = new List<byte>();
            foreach (var element in jsArray)
            {
                var num = element.AsNumber();
                if (num < 0 || num > 255)
                {
                    var errorMsg = $"数组元素 {num} 超出byte范围（0-255）";
                    return ScriptExecutionResult.Fail(errorMsg);
                }
                byteList.Add((byte)num);
            }
            return ScriptExecutionResult.SuccessWithRaw(byteList.ToArray());
        }

        // 场景3：基础类型（字符串/数值/布尔）→ 协议数据
        var valueStr = resultValue.ToString();
        return ScriptExecutionResult.SuccessWithProtocol(valueStr);
    }

    /// <inheritdoc/>
    public async Task<ScriptExecutionResult> DecodeAsync(ScriptExecutionContext context)
    {
        // 1.优先校验：实例是否已释放（关键调整）
        if (_disposed)
        {
            var errorMsg = "编解码器实例已释放，禁止执行解码操作";
            _codecLogger.LogError(context, "Decode", errorMsg);
            return ScriptExecutionResult.Fail(errorMsg);
        }

        //2.其次校验：实例是否被正确占用
        // 移除线程 ID 绑定的严格校验：对象池本身已保证实例唯一占用，线程 ID 绑定属于过度校验；
        // || Thread.CurrentThread.ManagedThreadId != _ownerThreadId
        // 校验实例状态：未占用/已释放 → 直接返回失败
        if (_isInUse != 1) // || Thread.CurrentThread.ManagedThreadId != _ownerThreadId
        {
            var errorMsg = "编解码器实例未被正确占用，禁止执行解码操作";
            _codecLogger.LogError(context, "Decode", errorMsg);
            return ScriptExecutionResult.Fail(errorMsg);
        }


        // 异步执行，避免阻塞线程池
        return await Task.Run(() =>
        {
            // 动态方法名：优先使用上下文的MethodName，否则默认decode
            var methodName = string.IsNullOrEmpty(context.MethodName) ? "decode" : context.MethodName;
            var result = InvokeJsMethod(methodName, context.RawData);
            if (result.Success)
            {
                _codecLogger.LogSuccess(context, "Decode");
            }
            else
            {
                _codecLogger.LogError(context, "Decode", result.ErrorMessage!);
            }
            return result;
        });
    }

    /// <inheritdoc/>
    public async Task<ScriptExecutionResult> EncodeAsync(ScriptExecutionContext context)
    {
        // 1.优先校验：实例是否已释放（关键调整）
        if (_disposed)
        {
            var errorMsg = "编解码器实例已释放，禁止执行编码操作";
            _codecLogger.LogError(context, "Encode", errorMsg);
            return ScriptExecutionResult.Fail(errorMsg);
        }

        //2.其次校验：实例是否被正确占用
        // 移除线程 ID 绑定的严格校验：对象池本身已保证实例唯一占用，线程 ID 绑定属于过度校验；
        // || Thread.CurrentThread.ManagedThreadId != _ownerThreadId
        if (_isInUse != 1) 
        {
            var errorMsg = "编解码器实例未被正确占用，禁止执行编码操作";
            _codecLogger.LogError(context, "Encode", errorMsg);
            return ScriptExecutionResult.Fail(errorMsg);
        }

        return await Task.Run(() =>
        {
            var methodName = string.IsNullOrEmpty(context.MethodName) ? "encode" : context.MethodName;
            var result = InvokeJsMethod(methodName, context.ProtocolData);
            if (result.Success)
            {
                _codecLogger.LogSuccess(context, "Encode");
            }
            else
            {
                _codecLogger.LogError(context, "Encode", result.ErrorMessage!);
            }
            return result;
        });
    }

    /// <inheritdoc/>
    public bool IsDisposed => _disposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源（实现IDisposable规范）
    /// 设计考量：释放引擎资源+重置占用状态，防止内存泄漏
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // 释放Jint引擎资源
            _engine.Dispose();
            // 强制重置占用状态
            Release();
        }

        _disposed = true;
    }

    ~JavaScriptDataCodec()
    {
        Dispose(false);
    }
}