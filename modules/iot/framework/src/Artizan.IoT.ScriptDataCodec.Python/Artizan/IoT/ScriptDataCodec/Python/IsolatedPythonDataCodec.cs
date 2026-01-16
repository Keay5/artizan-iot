//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace Artizan.IoT.ScriptDataCodec.Python;

///// <summary>
///// 独立上下文的Python数据编解码器
///// 设计模式：包装器模式（包装Python.NET）
///// 设计思路：
///// 1. 解决Python GIL锁并发问题：每个实例对应独立Python线程状态
///// 2. 支持动态调用任意方法：通过MethodName参数灵活指定
///// 3. 沙箱安全：禁用危险模块（os/subprocess），限制导入权限
///// 设计考量：
///// - 线程状态隔离：每个实例创建独立的Python上下文，避免GIL竞争
///// - GIL锁管理：使用Py.GIL()确保Python代码执行时持有锁
///// - 资源释放：完善Dispose逻辑，释放Python模块和线程状态
///// </summary>
//public class IsolatedPythonDataCodec : IDataCodec
//{
//    private readonly string _scriptContent;
//    private readonly dynamic _scriptModule;
//    private readonly IntPtr _pythonThreadState;
//    private int _isInUse;
//    private int _ownerThreadId;
//    private bool _disposed;

//    public IsolatedPythonDataCodec(string scriptContent)
//    {
//        if (string.IsNullOrEmpty(scriptContent))
//        {
//            throw new ArgumentNullException(nameof(scriptContent), "Python脚本内容不能为空");
//        }

//        _scriptContent = scriptContent;
//        InitializePythonEngine();

//        // 创建独立线程状态
//        _pythonThreadState = PythonEngine.BeginThread();

//        try
//        {
//            using (Py.GIL())
//            {
//                // 加载脚本到独立模块
//                var moduleName = $"iot_python_codec_{Guid.NewGuid():N}";
//                _scriptModule = PyModule.FromString(moduleName, scriptContent);

//                // 沙箱加固：禁用危险模块
//                SetupSandbox();

//                // 校验必要方法（可选，根据业务需求）
//                ValidateMethods();
//            }
//        }
//        catch (Exception ex)
//        {
//            throw new CodecException("Python引擎初始化失败", ex);
//        }

//        _isInUse = 0;
//        _ownerThreadId = -1;
//        _disposed = false;
//    }

//    /// <summary>
//    /// 初始化Python运行时（全局单例）
//    /// </summary>
//    private static void InitializePythonEngine()
//    {
//        if (!PythonEngine.IsInitialized)
//        {
//            PythonEngine.Initialize(new PythonEngineOptions
//            {
//                RedirectStdout = false,
//                RedirectStderr = false
//            });
//            PythonEngine.BeginAllowThreads();
//        }
//    }

//    /// <summary>
//    /// Python沙箱加固：禁用危险模块和函数
//    /// </summary>
//    private void SetupSandbox()
//    {
//        using (Py.GIL())
//        {
//            dynamic builtins = Py.Import("__builtins__");
//            // 移除危险函数
//            var dangerousFuncs = new[] { "__import__", "eval", "exec" };
//            foreach (var func in dangerousFuncs)
//            {
//                if (builtins.__dict__.Contains(func))
//                {
//                    builtins.__dict__.pop(func);
//                }
//            }

//            // 自定义导入函数，只允许安全模块
//            var allowedModules = new List<string> { "math", "json" };
//            builtins.__dict__["__import__"] = new Func<string, dynamic>((moduleName) =>
//            {
//                if (!allowedModules.Contains(moduleName))
//                {
//                    throw new Exception($"禁止导入模块: {moduleName}");
//                }
//                return Py.Import(moduleName);
//            });
//        }
//    }

//    /// <summary>
//    /// 校验脚本是否包含必要方法（可选）
//    /// </summary>
//    private void ValidateMethods()
//    {
//        using (Py.GIL())
//        {
//            if (!_scriptModule.HasAttr("decode"))
//            {
//                CodecLogger.LogError(new ScriptExecutionContext(), "Validate", "Python脚本缺少decode方法");
//            }
//            if (!_scriptModule.HasAttr("encode"))
//            {
//                CodecLogger.LogError(new ScriptExecutionContext(), "Validate", "Python脚本缺少encode方法");
//            }
//        }
//    }

//    public bool TryAcquire()
//    {
//        if (_disposed)
//        {
//            return false;
//        }

//        var original = System.Threading.Interlocked.CompareExchange(ref _isInUse, 1, 0);
//        if (original == 0)
//        {
//            _ownerThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
//            return true;
//        }
//        return false;
//    }

//    public void Release()
//    {
//        if (_disposed)
//        {
//            return;
//        }

//        if (System.Threading.Thread.CurrentThread.ManagedThreadId == _ownerThreadId)
//        {
//            System.Threading.Interlocked.Exchange(ref _isInUse, 0);
//            _ownerThreadId = -1;
//        }
//    }

//    private ScriptExecutionResult InvokePythonMethod(string methodName, params object[] args)
//    {
//        try
//        {
//            using (Py.GIL())
//            {
//                if (!_scriptModule.HasAttr(methodName))
//                {
//                    var errorMsg = $"Python脚本中未找到方法：{methodName}";
//                    return ScriptExecutionResult.Fail(errorMsg);
//                }

//                // 转换入参
//                var pyArgs = new List<PyObject>();
//                foreach (var arg in args)
//                {
//                    if (arg is byte[] bytes)
//                    {
//                        pyArgs.Add(bytes.ToPython());
//                    }
//                    else
//                    {
//                        pyArgs.Add(PyObject.FromManagedObject(arg));
//                    }
//                }

//                // 执行方法
//                var pyResult = _scriptModule.GetAttr(methodName).Invoke(pyArgs.ToArray());

//                // 处理返回结果
//                return ProcessPythonResult(pyResult);
//            }
//        }
//        catch (Exception ex)
//        {
//            var errorMsg = $"调用Python方法[{methodName}]失败：{ex.Message}";
//            return ScriptExecutionResult.Fail(errorMsg);
//        }
//    }

//    private ScriptExecutionResult ProcessPythonResult(PyObject pyResult)
//    {
//        using (Py.GIL())
//        {
//            // 列表 → byte[]
//            if (pyResult.IsInstance(PyListType))
//            {
//                var list = pyResult.As<List<int>>();
//                var bytes = new List<byte>();
//                foreach (var item in list)
//                {
//                    if (item < 0 || item > 255)
//                    {
//                        return ScriptExecutionResult.Fail($"元素{item}超出byte范围");
//                    }
//                    bytes.Add((byte)item);
//                }
//                return ScriptExecutionResult.SuccessWithRaw(bytes.ToArray());
//            }

//            // 字典 → JSON
//            if (pyResult.IsInstance(PyDictType))
//            {
//                var dict = pyResult.As<Dictionary<string, object>>();
//                var json = JsonSerializer.Serialize(dict);
//                return ScriptExecutionResult.SuccessWithProtocol(json);
//            }

//            // 其他类型 → 字符串
//            var str = pyResult.ToString();
//            return ScriptExecutionResult.SuccessWithProtocol(str);
//        }
//    }

//    public async Task<ScriptExecutionResult> DecodeAsync(ScriptExecutionContext context)
//    {
//        if (_isInUse != 1 || System.Threading.Thread.CurrentThread.ManagedThreadId != _ownerThreadId || _disposed)
//        {
//            var errorMsg = "Python编解码器实例状态异常，禁止解码";
//            CodecLogger.LogError(context, "Decode", errorMsg);
//            return ScriptExecutionResult.Fail(errorMsg);
//        }

//        return await Task.Run(() =>
//        {
//            var methodName = string.IsNullOrEmpty(context.MethodName) ? "decode" : context.MethodName;
//            var result = InvokePythonMethod(methodName, context.RawData);
//            if (result.Success)
//            {
//                CodecLogger.LogSuccess(context, "Decode");
//            }
//            return result;
//        });
//    }

//    public async Task<ScriptExecutionResult> EncodeAsync(ScriptExecutionContext context)
//    {
//        if (_isInUse != 1 || System.Threading.Thread.CurrentThread.ManagedThreadId != _ownerThreadId || _disposed)
//        {
//            var errorMsg = "Python编解码器实例状态异常，禁止编码";
//            CodecLogger.LogError(context, "Encode", errorMsg);
//            return ScriptExecutionResult.Fail(errorMsg);
//        }

//        return await Task.Run(() =>
//        {
//            var methodName = string.IsNullOrEmpty(context.MethodName) ? "encode" : context.MethodName;
//            var result = InvokePythonMethod(methodName, context.ProtocolData);
//            if (result.Success)
//            {
//                CodecLogger.LogSuccess(context, "Encode");
//            }
//            return result;
//        });
//    }

//    public bool IsDisposed => _disposed;

//    public void Dispose()
//    {
//        Dispose(true);
//        GC.SuppressFinalize(this);
//    }

//    protected virtual void Dispose(bool disposing)
//    {
//        if (_disposed)
//        {
//            return;
//        }

//        if (disposing)
//        {
//            using (Py.GIL())
//            {
//                _scriptModule?.Dispose();
//            }
//            Release();
//        }

//        if (_pythonThreadState != IntPtr.Zero)
//        {
//            PythonEngine.EndThread(_pythonThreadState);
//        }

//        if (PythonEngine.IsInitialized)
//        {
//            PythonEngine.Shutdown();
//        }

//        _disposed = true;
//    }

//    ~IsolatedPythonDataCodec()
//    {
//        Dispose(false);
//    }
//}


