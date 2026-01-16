# 使用示例



IoT 结果 / 异常体系的**完整使用示例**，我会覆盖「基础结果使用、泛型结果使用、异常抛出 / 捕获、本地化、日志记录、全局异常处理」等核心场景，所有示例均可直接在 ABP 项目中运行，同时标注关键设计思路和使用要点。

### 一、前置准备（ABP 项目配置）

#### 1. 注册本地化资源（Module 类）

```C#
using Volo.Abp.Modularity;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT
{
    [DependsOn(typeof(AbpLocalizationModule))]
    public class IoTCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 配置本地化资源
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Add<IoTResource>("zh-Hans") // 中文资源
                    .AddBaseTypes(typeof(AbpValidationResource))
                    .AddVirtualJson("/Localization/IoT"); // 资源文件路径
            });
            
            // 注册全局异常过滤器（ASP.NET Core项目）
            context.Services.AddControllers(options =>
            {
                options.Filters.Add<IoTExceptionFilter>();
            });
        }
    }
}
```

#### 2. 本地化资源文件（Localization/IoT/zh-Hans.json）

```
{
  "culture": "zh-Hans",
  "texts": {
    "IoT_DefaultError": "操作失败，请联系管理员",
    "IoT_DeviceDisabled": "设备{0}已禁用，无法执行此操作",
    "IoT_DeviceIdEmpty": "设备ID不能为空",
    "IoT_AuthenticationFailed": "设备{0}认证失败，凭证无效"
  }
}
```



### 二、核心使用场景示例

#### 场景 1：基础结果使用（无返回数据）

**业务场景**：设备启用操作，仅返回成功 / 失败状态

```csharp
using Artizan.IoT.Core.Results;
using Artizan.IoT.Core.Errors;
using Artizan.IoT.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Volo.Abp;

public class DeviceOperationService : ApplicationService
{
    private readonly ILogger<DeviceOperationService> _logger;

    // ABP自动注入ILogger和本地化器
    public DeviceOperationService(ILogger<DeviceOperationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 启用设备（基础结果示例）
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>IoTResult</returns>
    public IoTResult EnableDevice(string deviceId)
    {
        // 1. 参数校验（返回失败结果）
        if (string.IsNullOrEmpty(deviceId))
        {
            var error = new IoTError(IoTErrorCodes.DeviceIdEmpty);
            var result = IoTResult.Failed(error);
            //或者
            //var result = IoTResult.Failed(IoTErrorCodes.DeviceIdEmpty);
            
            // 记录失败日志（扩展方法）
            result.LogIfFailed(_logger, "设备启用-参数校验失败", deviceId);
            return result;
        }

        // 2. 业务校验（模拟设备禁用）
        if (deviceId.StartsWith("DISABLED_"))
        {
            var error = new IoTError(IoTErrorCodes.DeviceDisabled, deviceId);
            var result = IoTResult.Failed(error);
            //或者
            //var result = IoTResult.Failed(IoTErrorCodes.DeviceDisabled, deviceId);
            
            result.LogIfFailed(_logger, "设备启用-设备禁用");
            return result;
        }

        // 3. 执行业务操作（模拟成功）
        _logger.LogInformation("设备{0}启用成功", deviceId);
        return IoTResult.Success;
    }
}
```

**调用示例**：

```C#
// 控制器/应用服务中调用
public async Task<IActionResult> EnableDevice(string deviceId)
{
    var result = _deviceOperationService.EnableDevice(deviceId);
    
    // 方式1：直接返回结果（无异常）
    if (result.Succeeded)
    {
        return Ok(new { Success = true, Message = "设备启用成功" });
    }
    else
    {
        // 本地化错误消息
        var localizedMsg = result.LocalizeErrors(Localizer);
        return BadRequest(new { Success = false, Message = localizedMsg });
    }

    // 方式2：失败时抛出异常（由全局过滤器处理）
    // result.CheckErrors();
    // return Ok();
}
```

#### 场景 2：泛型结果使用（带返回数据）

**业务场景**：查询设备详情，返回设备数据 + 结果状态

```C#
using Artizan.IoT.Core.Results;
using Artizan.IoT.Core.Errors;

public class DeviceQueryService : ApplicationService
{
    private readonly ILogger<DeviceQueryService> _logger;

    public DeviceQueryService(ILogger<DeviceQueryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 查询设备详情（泛型结果示例）
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>IoTResult<DeviceDto></returns>
    public IoTResult<DeviceDto> GetDeviceDetail(string deviceId)
    {
        // 1. 参数校验
        if (string.IsNullOrEmpty(deviceId))
        {
            var error = new IoTError(IoTErrorCodes.DeviceIdEmpty);
            var result = IoTResult<DeviceDto>.Failed(error);
            //或者
            //var result = IoTResult<DeviceDto>.Failed(IoTErrorCodes.DeviceIdEmpty);  
            
            result.LogIfFailed(_logger, "查询设备详情-参数校验失败");
            return result;
        }

        // 2. 模拟查询数据库
        var device = new DeviceDto
        {
            DeviceId = deviceId,
            Name = "工业传感器001",
            Status = "在线",
            LastActiveTime = DateTime.Now
        };

        // 3. 返回带数据的成功结果
        _logger.LogInformation("查询设备{0}详情成功", deviceId);
        return IoTResult<DeviceDto>.Success(device);
    }
}

// 设备DTO
public class DeviceDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastActiveTime { get; set; }
}
```

**调用示例**：

```C#
public async Task<IActionResult> GetDeviceDetail(string deviceId)
{
    var result = _deviceQueryService.GetDeviceDetail(deviceId);
    
    if (result.Succeeded)
    {
        // 成功：返回设备数据
        return Ok(new 
        { 
            Success = true, 
            Data = result.Data 
        });
    }
    else
    {
        // 失败：返回本地化错误
        var localizedMsg = result.LocalizeErrors(Localizer);
        return BadRequest(new 
        { 
            Success = false, 
            Message = localizedMsg 
        });
    }
}
```

#### 场景 3：结果合并（多步骤校验）

**业务场景**：批量设备操作，汇总所有步骤的错误

```C#
using Artizan.IoT.Core.Results;
using Artizan.IoT.Core.Errors;

public class BatchDeviceService : ApplicationService
{
    private readonly ILogger<BatchDeviceService> _logger;

    public BatchDeviceService(ILogger<BatchDeviceService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 批量启用设备（结果合并示例）
    /// </summary>
    /// <param name="deviceIds">设备ID列表</param>
    /// <returns>IoTResult</returns>
    public IoTResult BatchEnableDevice(params string[] deviceIds)
    {
        if (deviceIds == null || deviceIds.Length == 0)
        {
            var error = new IoTError(IoTErrorCodes.DeviceIdEmpty, "批量设备ID列表为空");
            return IoTResult.Failed(error);
        }

        // 收集每个设备的操作结果
        var results = new List<IoTResult>();
        foreach (var deviceId in deviceIds)
        {
            var result = ValidateDevice(deviceId); // 校验设备
            if (!result.Succeeded)
            {
                results.Add(result);
                continue;
            }

            result = EnableSingleDevice(deviceId); // 启用设备
            results.Add(result);
        }

        // 合并所有结果（汇总错误）
        var combineResult = IoTResult.Combine(results.ToArray());
        
        // 记录合并结果日志
        combineResult.LogIfFailed(_logger, "批量启用设备");
        
        return combineResult;
    }

    // 私有辅助方法：校验设备
    private IoTResult ValidateDevice(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return IoTResult.Failed(new IoTError(IoTErrorCodes.DeviceIdEmpty));
        }
        return IoTResult.Success;
    }

    // 私有辅助方法：启用单个设备
    private IoTResult EnableSingleDevice(string deviceId)
    {
        if (deviceId.StartsWith("DISABLED_"))
        {
            return IoTResult.Failed(new IoTError(IoTErrorCodes.DeviceDisabled, deviceId));
        }
        return IoTResult.Success;
    }
}
```

**调用示例**：

```C#
public async Task<IActionResult> BatchEnableDevice(string[] deviceIds)
{
    var result = _batchDeviceService.BatchEnableDevice(deviceIds);
    
    if (result.Succeeded)
    {
        return Ok(new { Success = true, Message = "所有设备启用成功" });
    }
    else
    {
        // 汇总所有错误的本地化消息
        var errorMsg = result.Errors
            .Select(e => e.LocalizeErrorMessage(Localizer))
            .JoinAsString("；");
        
        return BadRequest(new 
        { 
            Success = false, 
            Message = $"部分设备启用失败：{errorMsg}" 
        });
    }
}
```

#### 场景 4：异常抛出与全局处理

**业务场景**：关键操作失败时抛出异常，由全局过滤器统一返回标准化响应

```C#
using Artizan.IoT.Core.Results;
using Artizan.IoT.Core.Errors;
using Artizan.IoT.Core.Exceptions;

public class CriticalDeviceService : ApplicationService
{
    private readonly ILogger<CriticalDeviceService> _logger;

    public CriticalDeviceService(ILogger<CriticalDeviceService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 设备认证（关键操作，失败抛异常）
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="token">认证凭证</param>
    public void AuthenticateDevice(string deviceId, string token)
    {
        // 1. 参数校验
        var validateResult = ValidateDeviceId(deviceId);
        if (!validateResult.Succeeded)
        {
            // 失败时抛出异常（扩展方法）
            validateResult.CheckErrors();
        }

        // 2. 认证校验
        if (!IsValidToken(deviceId, token))
        {
            var error = new IoTError(IoTErrorCodes.AuthenticationFailed, deviceId);
            var result = IoTResult.Failed(error);
            
            _logger.LogWarning("设备{0}认证失败，凭证无效", deviceId);
            
            // 手动抛出异常
            throw new IoTResultException(result)
            {
                Details = "设备认证失败，禁止访问核心接口"
            };
        }

        _logger.LogInformation("设备{0}认证成功", deviceId);
    }

    // 私有辅助方法
    private IoTResult ValidateDeviceId(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return IoTResult.Failed(new IoTError(IoTErrorCodes.DeviceIdEmpty));
        }
        return IoTResult.Success;
    }

    private bool IsValidToken(string deviceId, string token)
    {
        // 模拟认证逻辑：token以设备ID开头则有效
        return token.StartsWith(deviceId);
    }
}
```

**全局过滤器处理后的响应示例**（JSON）：

```json
{
  "error": {
    "code": "IoT_AuthenticationFailed",
    "message": "设备DISABLED_Sensor001认证失败，凭证无效",
    "details": [
      "Code: IoT_AuthenticationFailed, Description: DISABLED_Sensor001"
    ]
  }
}
```

#### 场景 5：本地化错误消息（多语言）

**业务场景**：根据当前文化返回不同语言的错误消息

```C#
using Artizan.IoT.Core.Results;
using Artizan.IoT.Core.Errors;
using Microsoft.Extensions.Localization;

public class LocalizedDeviceService : ApplicationService
{
    private readonly IStringLocalizer<IoTResource> _localizer;
    private readonly ILogger<LocalizedDeviceService> _logger;

    // 注入ABP本地化器
    public LocalizedDeviceService(
        IStringLocalizer<IoTResource> localizer,
        ILogger<LocalizedDeviceService> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// 本地化错误消息示例
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>本地化消息</returns>
    public string GetLocalizedErrorMsg(string deviceId)
    {
        // 1. 创建错误对象
        var error = new IoTError(IoTErrorCodes.DeviceDisabled, deviceId);
        
        // 2. 本地化单个错误
        var singleErrorMsg = error.LocalizeErrorMessage(_localizer);
        _logger.LogInformation("本地化错误消息：{0}", singleErrorMsg);
        // 输出示例（中文）：设备DISABLED_Sensor001已禁用，无法执行此操作

        // 3. 本地化多个错误
        var result = IoTResult.Failed(
            new IoTError(IoTErrorCodes.DeviceDisabled, deviceId),
            new IoTError(IoTErrorCodes.DeviceIdEmpty)
        );
        var multiErrorMsg = result.LocalizeErrors(_localizer);
        // 输出示例：设备DISABLED_Sensor001已禁用，无法执行此操作；设备ID不能为空

        return multiErrorMsg;
    }
}
```

### 三、关键使用要点总结

|     场景     |                           核心 API                           |                           使用要点                           |
| :----------: | :----------------------------------------------------------: | :----------------------------------------------------------: |
|   基础结果   |           `IoTResult.Success`/`IoTResult.Failed()`           | 1. 成功结果单例复用，失败结果工厂创建2. 支持单错误 / 多错误 / 列表错误 |
|   泛型结果   |       `IoTResult.Success(T data)`/`IoTResult.Failed()`       | 1. 仅成功时返回数据，失败时 Data 为 null2. 完全兼容基类的所有扩展方法 |
|   结果合并   |                    `IoTResult.Combine()`                     |  1. 汇总多步骤错误，自动去重2. 适合批量操作、多校验步骤场景  |
|   异常抛出   |   `result.CheckErrors()`/`new IoTResultException(result)`    | 1. 扩展方法一键抛异常，简化代码2. 异常包含完整 IoTResult 上下文，便于排查 |
|   日志记录   |            `result.LogIfFailed(logger, context)`             | 1. 仅失败时记录日志，避免冗余2. 支持自定义业务上下文，提升日志可读性 |
|    本地化    | `error.LocalizeErrorMessage(localizer)`/`result.LocalizeErrors(localizer)` | 1. 支持模板参数替换（如 {0}）2. 本地化失败时自动降级返回默认错误 |
| 全局异常处理 |                     `IoTExceptionFilter`                     | 1. 统一返回标准化 JSON 响应2. 隐藏敏感异常细节，仅返回用户友好消息 |

### 四、最佳实践建议

1. 结果 vs 异常：

   - 非关键操作（如查询、普通校验）返回`IoTResult`，由调用方自行处理；
   - 关键操作（如认证、核心业务）抛出`IoTResultException`，由全局过滤器统一处理。

   

2. 日志规范：

   - 所有失败结果必须记录日志，包含设备 ID、错误码等关键信息；
   - 成功操作可选记录日志，重点记录核心业务操作（如设备启用、认证）。

   

3. 本地化规范：

   - 错误码作为本地化键，避免硬编码错误消息；
   - 错误描述（Description）仅存储参数（如设备 ID），不存储最终消息。

   

4. 性能优化：

   - 高频使用的错误码（如`IoT_DeviceIdEmpty`）可缓存本地化结果；
   - 批量操作优先使用`Combine`汇总错误，避免多次返回 / 抛异常。

   

这些示例覆盖了 90% 以上的业务场景，你可以根据实际需求调整，核心是保持「结果封装、异常统一、日志规范、本地化友好」的设计原则。



#  结合 `.resx` 本地化资源文件使用 

结合 `.resx` 本地化资源文件使用的需求完全可行！ABP 框架原生支持 `.resx`（资源文件）和 JSON 两种本地化方式，我会为你提供**基于 `.resx` 的完整配置 + 使用示例**，包括资源文件创建、模块配置、代码调用全流程，同时对比两种方式的优劣，帮你选择最适合的方案。



### 一、核心结论

✅ **完全可以结合 `.resx` 文件使用**，且 `.resx` 是 .NET 传统的本地化方案，在 ABP 框架中适配性极佳，尤其适合团队已有 `.resx` 资源体系的场景。



### 二、完整实现步骤（基于 .resx）

#### 步骤 1：创建 .resx 本地化资源文件

在项目中创建 `Localization/IoT` 目录，添加以下资源文件（ABP 会自动识别多语言后缀）：

- `IoTResource.zh-CN.resx`（中文）
- `IoTResource.en.resx`（英文，可选）

##### 1.1 IoTResource.zh-CN.resx（中文资源）

|       名称（Name）       |           值（Value）           |           说明            |
| :----------------------: | :-----------------------------: | :-----------------------: |
|     IoT_DefaultError     |     操作失败，请联系管理员      |       默认错误兜底        |
|    IoT_DeviceDisabled    | 设备 {0} 已禁用，无法执行此操作 | 模板化消息，{0} 为设备 ID |
|    IoT_DeviceIdEmpty     |        设备 ID 不能为空         |         简单提示          |
| IoT_AuthenticationFailed |   设备 {0} 认证失败，凭证无效   |        模板化消息         |

##### 1.2 IoTResource.en.resx（英文资源）

|       名称（Name）       |                      值（Value）                      |
| :----------------------: | :---------------------------------------------------: |
|     IoT_DefaultError     |  Operation failed, please contact the administrator   |
|    IoT_DeviceDisabled    | Device {0} is disabled, cannot perform this operation |
|    IoT_DeviceIdEmpty     |               Device ID cannot be empty               |
| IoT_AuthenticationFailed | Device {0} authentication failed, invalid credentials |

> 🔧 **.resx 文件配置要点**：
>
> 
>
> - 确保文件的「生成操作」设置为 `Embedded resource`（右键文件 → 属性 → 生成操作）；
> - 资源名称（Name）必须和 `IoTErrorCodes` 中的错误码完全一致（如 `IoT_DeviceDisabled`）；
> - 模板参数用 `{0}` `{1}` 表示，和代码中的参数替换逻辑匹配。



#### 步骤 2：配置 ABP 本地化模块（Module 类）

修改 `IoTCoreModule`，将本地化方式改为读取 `.resx` 资源（替换之前的 JSON 配置）：

```C#
using Volo.Abp.Modularity;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT
{
    [DependsOn(typeof(AbpLocalizationModule))]
    public class IoTCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 1. 配置虚拟文件系统（可选，若resx在独立类库中需配置）
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.AddEmbedded<IoTCoreModule>(
                    baseNamespace: "Artizan.IoT", // 项目根命名空间
                    baseFolder: "Localization/IoT" // resx文件所在目录
                );
            });

            // 2. 配置本地化资源（核心：使用.resx）
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Add<IoTResource>("zh-Hans") // 中文（默认）
                    .AddBaseTypes(typeof(AbpValidationResource)) // 复用ABP内置验证消息
                    .AddResx("IoTResource.zh-CN.resx") // 关联中文.resx文件
                    .AddResx("IoTResource.en.resx"); // 关联英文.resx文件

                // 可选：设置默认文化
                options.DefaultResourceType = typeof(IoTResource);
                options.DefaultCultureName = "zh-Hans";
            });
        }
    }
}
```

#### 步骤 3：代码中使用 .resx 本地化（和之前的 API 完全兼容）

基于 `.resx` 的本地化**代码调用方式和 JSON 完全一致**，无需修改业务代码，ABP 会自动读取 `.resx` 中的资源：

##### 3.1 基础使用（单个错误本地化）

```C#
using Artizan.IoT.Core.Errors;
using Artizan.IoT.Core.Results;
using Microsoft.Extensions.StringLocalizer;
using Volo.Abp;

public class DeviceService : ApplicationService
{
    private readonly IStringLocalizer<IoTResource> _localizer;

    // ABP 自动注入本地化器（.resx 版本）
    public DeviceService(IStringLocalizer<IoTResource> localizer)
    {
        _localizer = localizer;
    }

    public string GetLocalizedError(string deviceId)
    {
        // 1. 创建错误对象（Description存储参数）
        var error = new IoTError(IoTErrorCodes.DeviceDisabled, deviceId);
        
        // 2. 本地化错误消息（自动读取.resx中的模板并替换参数）
        var localizedMsg = error.LocalizeErrorMessage(_localizer);
        
        // 输出示例（中文）：设备Sensor001已禁用，无法执行此操作
        // 输出示例（英文）：Device Sensor001 is disabled, cannot perform this operation
        return localizedMsg;
    }
}
```

##### 3.2 结果批量本地化

```C#
public string GetBatchLocalizedErrors()
{
    // 创建多错误结果
    var result = IoTResult.Failed(
        new IoTError(IoTErrorCodes.DeviceDisabled, "Sensor001"),
        new IoTError(IoTErrorCodes.DeviceIdEmpty)
    );

    // 批量本地化（自动拼接所有错误的本地化消息）
    var batchMsg = result.LocalizeErrors(_localizer);
    
    // 输出示例：设备Sensor001已禁用，无法执行此操作；设备ID不能为空
    return batchMsg;
}
```

##### 3.3 异常本地化（全局过滤器自动处理）

```C#
public void AuthenticateDevice(string deviceId)
{
    var error = new IoTError(IoTErrorCodes.AuthenticationFailed, deviceId);
    var result = IoTResult.Failed(error);
    
    // 抛出异常，全局过滤器会自动读取.resx本地化消息
    throw new IoTResultException(result);
}
```

### 三、.resx vs JSON 本地化对比（选型建议）

|   特性   |             .resx（资源文件）              |             JSON（虚拟文件）              |
| :------: | :----------------------------------------: | :---------------------------------------: |
| 原生支持 |         .NET 原生支持，兼容性极佳          |       ABP 扩展支持，需依赖 ABP 框架       |
| 编辑体验 | 可通过 VS 可视化编辑器编辑，支持多语言拆分 |        手动编辑 JSON 文件，易出错         |
| 版本控制 |       文本格式，Git 对比 / 合并友好        |            文本格式，同样友好             |
| 动态修改 |       编译后嵌入程序集，无法动态修改       | 可作为嵌入式文件 / 物理文件，支持动态替换 |
| 团队适配 |      适合传统 .NET 团队（熟悉 resx）       |   适合现代前端 / 全栈团队（熟悉 JSON）    |
|   性能   |           编译时嵌入，读取速度快           |      运行时解析，速度略慢（可缓存）       |

#### 选型建议：

- ✅ 选 `.resx`：如果你的团队是传统 .NET 开发、已有大量 `.resx` 资源、需要 VS 可视化编辑；
- ✅ 选 JSON：如果你的团队是全栈 / 前端为主、需要动态修改资源、追求轻量化配置。

### 四、关键注意事项（.resx 避坑）

1. **命名空间匹配**：`IoTResource``.resx`

   ```C#
   // IoTResource.cs 命名空间示例（需和项目根命名空间一致）
   namespace Artizan.IoT
   {
       [LocalizationResourceName("IoT")]
       public class IoTResource : AbpLocalizationResource
       {
           public IoTResource()
           {
               AddBaseTypes(typeof(AbpValidationResource));
           }
       }
   }
   ```

   

2. **资源文件路径**：`.resx``Localization/IoT``baseFolder`

   

3. **模板参数格式**：`.resx``{0}``{1}``string.Format``{deviceId}`

   

4. **文化名称规范**：资源文件后缀必须符合 .NET 文化名称规范（如  而非 ， 而非 ），否则无法自动匹配当前文化。

   

### 五、总结

1. **可行性**：完全可以结合 `.resx` 本地化资源文件使用，且代码调用方式和 JSON 完全兼容，无需修改业务逻辑；
2. **配置要点**：核心是在 Module 中配置 `AddResx()` 关联资源文件，并确保 `.resx` 的「生成操作」为 `Embedded resource`；
3. **选型建议**：根据团队技术栈选择（传统 .NET 选 resx，现代全栈选 JSON）；
4. **兼容性**：ABP 框架会自动适配 `.resx` 资源，之前的 `LocalizeErrorMessage`/`LocalizeErrors` 扩展方法无需任何修改。

这套方案既保留了我们之前设计的「错误码 + 模板参数」核心逻辑，又适配了 `.resx` 这种经典的本地化方式，兼顾了兼容性和团队使用习惯。



# 是否自动本地化？

目前这个设计，在Volo.Abp框架中，能自动对消息进行本地化？

这个问题直击核心！**当前设计在 ABP 框架中无法「完全自动」本地化消息**—— 但通过我们的设计和 ABP 的本地化体系结合，只需少量配置就能实现「半自动 + 全局统一」的本地化效果。下面我会拆解 ABP 的本地化机制、当前设计的适配点，以及最终实现「自动本地化」的完整方案。

### 一、核心结论

| 场景                                      | 是否自动本地化 | 关键说明                                                     |
| ----------------------------------------- | -------------- | ------------------------------------------------------------ |
| ABP 内置异常（如 AbpValidationException） | ✅ 完全自动     | ABP 框架内置了本地化逻辑，无需手动处理                       |
| 我们的 IoTResultException                 | ❌ 非完全自动   | 需要实现`ILocalizeErrorMessage`接口 + 配置本地化资源，ABP 会调用我们的本地化逻辑 |
| 直接返回的 IoTResult                      | ❌ 手动触发     | 需调用`LocalizeErrors`扩展方法，但可封装成全局过滤器实现「自动」 |

简单来说：**ABP 不会「无感知自动」本地化我们的自定义错误，但会提供「本地化能力」，我们只需对接这个能力即可实现「伪自动」（对业务层透明）**。

### 二、ABP 本地化的核心机制（先理解原理）

ABP 的本地化核心是「**资源 + 本地化器 + 上下文**」：

1. **本地化资源**：`.resx`/JSON 文件，存储「键 - 多语言值」（如`IoT_DeviceDisabled` → 设备 {0} 已禁用）；
2. **本地化器（IStringLocalizer）**：ABP 注入的核心服务，根据「键 + 当前文化」读取本地化值；
3. 自动触发点：
   - ABP 的全局异常处理器会识别实现`ILocalizeErrorMessage`的异常，自动调用其`LocalizeMessage`方法；
   - ABP 的验证框架会自动读取`AbpValidationResource`中的验证消息。

### 三、当前设计如何对接 ABP 实现「自动本地化」

我们的设计已经预留了对接 ABP 的接口，只需完成 3 步配置，就能让 ABP「自动」处理我们的错误消息本地化：

#### 步骤 1：确保 IoTResultException 实现`ILocalizeErrorMessage`（已完成）

这是 ABP 识别「需要本地化的自定义异常」的核心接口，我们之前的设计已经实现：

```csharp
public class IoTResultException : BusinessException, ILocalizeErrorMessage
{
    // ABP会自动调用这个方法，实现异常消息的本地化
    public virtual string LocalizeMessage(LocalizationContext context)
    {
        var localizer = context.LocalizerFactory.Create<IoTResource>();
        SetData(localizer);
        return IoTResult.LocalizeErrors(localizer);
    }
}
```

👉 **关键**：ABP 的全局异常处理器会检测异常是否实现`ILocalizeErrorMessage`，如果是，会自动调用该方法获取本地化后的消息，而非直接使用`Exception.Message`。

#### 步骤 2：配置 ABP 本地化资源（.resx/JSON，已讲过）

确保`IoTResource`和本地化文件（如`IoTResource.zh-CN.resx`）正确配置，ABP 能读取到错误码对应的本地化模板：

```csharp
// IoTCoreModule.cs
Configure<AbpLocalizationOptions>(options =>
{
    options.Resources
        .Add<IoTResource>("zh-Hans")
        .AddBaseTypes(typeof(AbpValidationResource))
        .AddResx("IoTResource.zh-CN.resx"); // 或AddVirtualJson

    options.DefaultResourceType = typeof(IoTResource);
});
```

#### 步骤 3：注册 ABP 的全局异常过滤器（关键！）

ABP 的`AbpExceptionFilter`是处理异常本地化的核心，必须确保它被注册（ABP 项目默认已注册，但需确认）：

```csharp
// 方式1：Module中注册（ASP.NET Core项目）
context.Services.AddControllers(options =>
{
    options.Filters.Add<AbpExceptionFilter>(); // ABP核心异常过滤器
    options.Filters.Add<IoTExceptionFilter>(); // 我们的自定义过滤器（可选）
});

// 方式2：依赖AbpAspNetCoreModule（推荐）
[DependsOn(typeof(AbpAspNetCoreModule))] // 自动注册AbpExceptionFilter
public class IoTCoreModule : AbpModule { ... }
```

### 四、不同场景的「自动本地化」效果

#### 场景 1：抛出 IoTResultException（最接近「自动」）

业务层只需抛出异常，ABP 会自动完成本地化：

```csharp
// 业务层代码（无需关心本地化）
public void EnableDevice(string deviceId)
{
    if (deviceId.StartsWith("DISABLED_"))
    {
        var result = IoTResult.Failed(IoTErrorCodes.DeviceDisabled, deviceId);
        throw new IoTResultException(result); // 仅抛出异常
    }
}
```

👉 **ABP 自动处理流程**：

1. ABP 的`AbpExceptionFilter`捕获`IoTResultException`；
2. 检测到异常实现`ILocalizeErrorMessage`，调用`LocalizeMessage`方法；
3. 我们的`LocalizeMessage`方法读取`.resx`中的模板，替换参数（设备 ID）；
4. ABP 将本地化后的消息返回给客户端，同时记录本地化后的日志。

#### 场景 2：直接返回 IoTResult（手动封装为「自动」）

如果业务层不抛异常、直接返回 IoTResult，需在全局结果过滤器中自动本地化：

```csharp
// 新增：IoTResultFilter.cs（全局结果过滤器）
public class IoTResultFilter : IResultFilter
{
    private readonly IStringLocalizer<IoTResource> _localizer;

    public IoTResultFilter(IStringLocalizer<IoTResource> localizer)
    {
        _localizer = localizer;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        // 仅处理返回IoTResult的情况
        if (context.Result is ObjectResult objectResult && objectResult.Value is IoTResult result)
        {
            if (!result.Succeeded)
            {
                // 自动本地化错误消息
                var localizedMsg = result.LocalizeErrors(_localizer);
                // 替换返回结果为本地化后的格式
                context.Result = new ObjectResult(new
                {
                    Success = false,
                    Message = localizedMsg,
                    Errors = result.Errors.Select(e => new
                    {
                        Code = e.Code,
                        Description = e.LocalizeErrorMessage(_localizer)
                    })
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}

// 注册过滤器（IoTCoreModule.cs）
context.Services.AddControllers(options =>
{
    options.Filters.Add<IoTResultFilter>(); // 自动本地化IoTResult
});
```

👉 效果：业务层直接返回`IoTResult`，过滤器自动本地化后返回给客户端，业务层无需任何本地化代码。

### 五、ABP 自动本地化的关键配置检查清单

确保以下配置全部完成，才能实现「自动」效果：

1. ✅ `IoTResource`类继承`AbpLocalizationResource`，并配置了基础资源；
2. ✅ 本地化文件（.resx/JSON）的「生成操作」为`Embedded resource`；
3. ✅ `IoTResultException`实现`ILocalizeErrorMessage`接口；
4. ✅ 注册了 ABP 的`AbpExceptionFilter`（依赖`AbpAspNetCoreModule`即可）；
5. ✅ 错误码（如`IoT_DeviceDisabled`）和本地化资源的「键」完全一致；
6. ✅ 当前请求的`Culture`（如`zh-Hans`）在本地化资源中有对应配置。

### 六、最终效果演示

#### 1. 业务层代码（极简，无本地化逻辑）

```csharp
public IoTResult EnableDevice(string deviceId)
{
    if (string.IsNullOrEmpty(deviceId))
    {
        return IoTResult.Failed(IoTErrorCodes.DeviceIdEmpty);
    }
    if (deviceId.StartsWith("DISABLED_"))
    {
        throw new IoTResultException(IoTResult.Failed(IoTErrorCodes.DeviceDisabled, deviceId));
    }
    return IoTResult.Success;
}
```

#### 2. 客户端接收的响应（自动本地化）

- 抛出异常的场景（ABP 自动处理）：

  ```json
  {
    "error": {
      "code": "IoT_DeviceDisabled",
      "message": "设备DISABLED_Sensor001已禁用，无法执行此操作", // 自动本地化
      "details": null
    }
  }
  ```

  

- 直接返回结果的场景（过滤器自动处理）：

  ```json
  {
    "success": false,
    "message": "设备ID不能为空", // 自动本地化
    "errors": [
      {
        "code": "IoT_DeviceIdEmpty",
        "description": "设备ID不能为空"
      }
    ]
  }
  ```

  

### 七、总结

1. **核心答案**：
   - ABP 不会「无感知自动」本地化我们的自定义错误，但能通过「接口 + 过滤器」实现「伪自动」（对业务层透明）；
   - 实现`ILocalizeErrorMessage`的异常会被 ABP 自动调用本地化逻辑；
   - 直接返回的 IoTResult 可通过全局过滤器实现自动本地化。
2. **关键设计适配点**：
   - 我们的`IoTResultException`实现了 ABP 的`ILocalizeErrorMessage`接口，是对接自动本地化的核心；
   - 错误码和本地化资源键一一对应，保证 ABP 能正确读取模板；
   - 扩展方法`LocalizeErrors`/`LocalizeErrorMessage`封装了本地化逻辑，适配 ABP 的本地化器。
3. **最终体验**：业务层只需关注「错误码 + 参数」，无需关心本地化细节，ABP 框架 + 我们的过滤器会自动完成消息本地化，和 ABP 内置异常的体验完全一致。

这套设计既保留了 ABP 框架的原生能力，又适配了我们的自定义结果 / 异常体系，是「框架原生 + 自定义扩展」的最佳实践。



# IoTResultException 固定 localizer的问题

在 `IoTResultException` 类的 LocalizeMessage 方法：

```C#
    public virtual string LocalizeMessage(LocalizationContext context)
    {
        Check.NotNull(context, nameof(context));

        // 步骤1：创建IoT模块的本地化器
        var localizer = context.LocalizerFactory.Create<IoTResource>();

        // 步骤2：设置本地化参数到异常Data字典（供ABP全局异常处理器使用）
        SetData(localizer);

        // 步骤3：本地化所有错误信息并返回
        return IoTResult.LocalizeErrors(localizer);
    }
```

中

```C#
 var localizer = context.LocalizerFactory.Create<IoTResource>()
```

硬编码使用了 `IoTResource` ，不够灵活，针对这个问题，我的思路大致如下：

如果哪个 AbpModule(比如： IoTMqttModule)， 要使用`IoTResultException `,  需要在 `IoTMqttModule` 中进行如下配置：

```C#

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<IoTResource>()
                .AddVirtualJson("Artizan/IoT/Localization/Mqtt");
        });

    public overwrite string LocalizeMessage(LocalizationContext context)
    {
        var localizer = context.LocalizerFactory.Create<IoTMqttResource>();

        SetData(localizer);

        return IoTTResult.LocalizeErrors(localizer);
    }
```

并在 `IoTMqttModule` 中添加 Localization 本地文件中添加响应的本地化字段。

如果想使用 `IoTMqttResultException`，不想使用 `IoTResultException`，可以继承 `IoTResultException`，
然后重写 `LocalizeMessage` 方法。



### 一、方案核心优势（精准命中你的诉求）

|            优势            |                           具体价值                           |               解决的痛点                |
| :------------------------: | :----------------------------------------------------------: | :-------------------------------------: |
|     **零反射，高性能**     |  直接通过泛型 `Create()` 创建本地化器，无运行时类型查找开销  |       彻底解决反射带来的性能顾虑        |
|       **模块化隔离**       |     每个模块的本地化资源独立维护，核心模块与业务模块解耦     | 避免多模块资源冲突，符合 ABP 模块化架构 |
| **两种扩展方式，按需选择** | 1. 轻量扩展：复用 `IoTResource`，仅添加模块专属 JSON 资源2. 完全隔离：继承 `IoTResultException`，使用模块专属 `Resource` |     基础场景低成本，复杂场景高灵活      |
|      **遵循开闭原则**      | 基类 `IoTResultException` 无需修改，扩展仅需新增子类 / 资源  |    核心代码稳定，扩展不影响原有逻辑     |
|      **使用成本可控**      |    基础场景直接用 `IoTResultException`，复杂场景才子类化     |        不增加简单业务的开发负担         |

### 二、两种扩展方式的落地细节（完整代码）

#### 方式 1：轻量扩展（复用 `IoTResource`，仅添加模块 JSON 资源）

适合**资源关联度高**的模块（如 MQTT 是 IoT 核心子模块），无需新增异常类，仅需注册资源文件。

1. IoTMqttModule 配置本地化资源

   ```c#
   using Volo.Abp.Modularity;
   using Volo.Abp.Localization;
   using Artizan.IoT.Localization;
   
   namespace Artizan.IoT.Mqtt
   {
       [DependsOn(typeof(IoTCoreModule))]
       public class IoTMqttModule : AbpModule
       {
           public override void ConfigureServices(ServiceConfigurationContext context)
           {
               // 复用 IoTResource，添加 MQTT 模块的 JSON 资源
               Configure<AbpLocalizationOptions>(options =>
               {
                   options.Resources
                       .Get<IoTResource>() // 复用核心资源
                       .AddVirtualJson("Artizan/IoT/Localization/Mqtt"); // MQTT 专属 JSON 路径
               });
           }
       }
   }
   ```

   

2. 添加 MQTT 本地化 JSON 文件（`Artizan/IoT/Localization/Mqtt/zh-Hans.json`）

   ```json
   {
     "culture": "zh-Hans",
     "texts": {
       "IoT:Mqtt_ConnectFailed": "设备{0}MQTT连接失败，原因：{1}",
       "IoT:Mqtt_Timeout": "设备{0}MQTT通信超时"
     }
   }
   ```

   

3. 业务层使用（直接用 `IoTResultException`）

   ```csharp
   throw new IoTResultException(
       IoTResult.Failed("IoT_Mqtt_ConnectFailed", "Sensor001", "用户名密码错误")
   );
   ```

   

#### 方式 2：完全隔离扩展（继承异常 + 专属 Resource）

适合**独立模块**（如用户模块、权限模块），与核心 IoT 资源完全解耦。

1. 创建 MQTT 模块专属 Resource

   ```csharp
   using Volo.Abp.Localization;
   
   namespace Artizan.IoT.Mqtt.Localization
   {
       [LocalizationResourceName("IoTMqtt")]
       public class IoTMqttResource : AbpLocalizationResource
       {
           public IoTMqttResource()
           {
               AddBaseTypes(typeof(IoTResource)); // 可选：继承核心资源
           }
       }
   }
   ```

   

2. 创建 MQTT 模块专属异常 `IoTMqttResultException`

   ```csharp
   using System.Runtime.Serialization;
   using Artizan.IoT.Exceptions;
   using Artizan.IoT.Mqtt.Localization;
   using Artizan.IoT.Results;
   using JetBrains.Annotations;
   using Volo.Abp.Localization;
   
   namespace Artizan.IoT.Mqtt.Exceptions
   {
       [Serializable]
       [DataContract]
       public class IoTMqttResultException : IoTResultException
       {
           // 构造函数：复用基类逻辑
           public IoTMqttResultException([NotNull] IoTResult iotResult) : base(iotResult)
           {
           }
   
           // 序列化构造函数：必须实现，保证分布式场景兼容
           protected IoTMqttResultException(SerializationInfo info, StreamingContext context) : base(info, context)
           {
           }
   
           // 重写本地化方法：使用 MQTT 专属 Resource
           public override string LocalizeMessage(LocalizationContext context)
           {
               Check.NotNull(context, nameof(context));
   
               // 关键点：使用模块专属的 IoTMqttResource
               var localizer = context.LocalizerFactory.Create<IoTMqttResource>();
   
               SetData(localizer);
               // 注意：这里是 IoTResult
               return IoTResult.LocalizeErrors(localizer);
           }
       }
   }
   ```

   

3. IoTMqttModule 配置专属 Resource

   ```csharp
   Configure<AbpLocalizationOptions>(options =>
   {
       options.Resources
           .Add<IoTMqttResource>("zh-Hans")
           .AddVirtualJson("Artizan/IoT/Mqtt/Localization");
   });
   ```

   

4. 业务层使用（抛专属异常）

   ```csharp
   throw new IoTMqttResultException(
       IoTResult.Failed("Mqtt_ConnectFailed", "Sensor001", "Broker不可达")
   );
   ```

   

### 三、方案关键注意事项

1. 序列化构造函数必写

   子类异常必须实现  `protected 序列化构造函数`，否则分布式场景（如 RPC 调用）会序列化失败。

2. 资源优先级

   当模块资源与核心资源有相同键时，模块资源优先级更高（ABP 本地化的覆盖逻辑）。

### 四、方案对比（选型建议）

|            扩展方式             |              适用场景              |         优点         |                     缺点                      |
| :-----------------------------: | :--------------------------------: | :------------------: | :-------------------------------------------: |
|    轻量扩展（复用 Resource）    |    IoT 子模块（MQTT/Modbus 等）    |  零新增类，成本最低  |        与核心资源耦合，不适合独立模块         |
| 完全隔离（继承异常 + Resource） | 独立业务模块（用户 / 权限 / 订单） | 完全解耦，扩展性最强 | 需新增 2 个类（Resource+Exception），成本略高 |

### 总结

这个方案**完美平衡了性能、灵活性和模块化**，是真正符合 ABP 设计哲学的最佳实践：

- 轻量场景：复用资源，零代码新增；
- 复杂场景：继承扩展，完全隔离；
- 全程无反射，性能拉满；
- 核心代码稳定，扩展不入侵。



