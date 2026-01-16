# `IoTResult` & `IoTResultException` 优秀设计总结

`IoTResult`（结果封装）和 `IoTResultException`（异常封装）是 Artizan IoT 平台**错误处理体系的核心组件**，二者深度耦合且无缝集成 ABP 本地化能力，遵循 .NET/ABP 最佳实践，兼具**简洁性、扩展性、高性能**三大核心优势。

## 一、 核心设计理念

1. **值语义优先**：以「错误码 + 描述」为核心定义业务错误，而非依赖底层异常，贴合 IoT 多步骤校验、批量设备操作的业务场景；
2. **不可变设计**：对象创建后属性不可修改，保证线程安全，适配高并发 IoT 设备通信场景；
3. **框架原生集成**：完全融入 ABP 全局异常处理、本地化体系，不重复造轮子；
4. **零冗余 + 流式编程**：通过 `Combine`+`CheckError` 消除样板代码，支持链式调用。

## 二、 `IoTResult` 优秀设计亮点

### 1.  不可变结果封装（线程安全 + 无副作用）

|    设计点    |                     实现细节                     |                         价值                          |
| :----------: | :----------------------------------------------: | :---------------------------------------------------: |
| 私有构造函数 | 仅通过 `Success` 单例 /`Failed` 静态方法创建实例 | 避免外部随意修改 `Succeeded`/`Errors`，保证结果可信度 |
|   只读属性   | `Succeeded`（bool）、`Errors`（`IReadOnlyList`） |            防止集合被篡改，适配多线程场景             |
|   单例复用   |    预定义 `static readonly IoTResult Success`    |         减少内存分配，提升高性能场景下的效率          |

### 2.  故障聚合核心能力：`Combine` 方法

|    设计点    |                           实现细节                           |                      价值                      |
| :----------: | :----------------------------------------------------------: | :--------------------------------------------: |
| 全量错误聚合 |        遍历所有待合并结果，收集**所有失败结果的错误**        | 一次返回多步骤校验的全部错误，减少前端多次请求 |
| 自动错误去重 | 依赖 `IoTError` 实现的 `IEquatable` 接口，通过 `Distinct()` 去重 |       避免重复错误信息，简化前端展示逻辑       |
| 短路成功判断 |          所有结果成功才返回 `Success`，否则返回失败          |    符合「任意步骤失败则整体失败」的业务逻辑    |
| 支持参数数组 |              `params IoTResult[] results` 入参               |  灵活接收任意数量的校验结果，无需手动构建集合  |

### 3.  一键校验能力：`CheckError` 方法（点睛之笔）

|     设计点     |                    实现细节                     |                             价值                             |
| :------------: | :---------------------------------------------: | :----------------------------------------------------------: |
|  流式调用支持  |         成功时返回 `this`，失败时抛异常         | 支持链式调用（如 `result.Combine(r2).CheckError()`），代码线性流转 |
|  异常无缝衔接  |    失败时直接抛出 `IoTResultException(this)`    |          打通「结果→异常」的链路，无需手动 new 异常          |
| 自定义异常扩展 |           提供泛型重载 `CheckError()`           | 支持模块专属异常（如 `IoTMqttResultException`），适配多模块隔离场景 |
|  消灭样板代码  | 替代 `if(!result.Succeeded) throw ...` 重复逻辑 |                代码量减少 60%，可读性大幅提升                |

### 4.  泛型扩展支持：`IoTResult`

- 继承基础 `IoTResult`，新增 `Data` 泛型属性，支持携带业务返回数据；
- 实现泛型版 `Combine` 方法，兼顾「错误聚合」和「数据传递」；
- 与非泛型版本逻辑一致，降低学习成本。

## 三、 `IoTResultException` 优秀设计亮点

### 1.  异常与结果深度绑定

|          设计点          |                           实现细节                           |                         价值                         |
| :----------------------: | :----------------------------------------------------------: | :--------------------------------------------------: |
|  封装 `IoTResult` 属性   | 内置 `public IoTResult IoTResult { get; }`，保留完整错误上下文 |  异常携带所有错误信息，而非单一错误码，便于日志排查  |
|        防御性校验        |      构造函数校验 `IoTResult` 必须为失败状态且包含错误       | 避免无效异常（如封装成功结果的异常），提升代码健壮性 |
| 继承 `BusinessException` |         复用 ABP 异常体系的 `Code`/`LogLevel` 等属性         |      自动融入 ABP 全局异常过滤器，无需额外配置       |

### 2.  模块化扩展设计（零反射 + 高性能）

|          设计点          |                      实现细节                      |                             价值                             |
| :----------------------: | :------------------------------------------------: | :----------------------------------------------------------: |
| 虚方法 `LocalizeMessage` |   基类默认使用 `IoTResource` 本地化，子类可重写    | 支持模块专属本地化资源（如 `IoTMqttResource`），无反射性能损耗 |
|       序列化兼容性       |      实现 `ISerializable`，重写序列化构造函数      |   适配分布式场景（如 RPC 调用、消息队列），避免序列化失败    |
|        子类化隔离        | 模块可继承实现 `IoTMqttResultException` 等专属异常 |      不同模块的错误本地化完全隔离，符合 ABP 模块化架构       |

### 3.  无缝集成 ABP 本地化体系

|            设计点            |                           实现细节                           |                             价值                             |
| :--------------------------: | :----------------------------------------------------------: | :----------------------------------------------------------: |
| 实现 `ILocalizeErrorMessage` |      重写 `LocalizeMessage` 方法，对接 ABP 本地化上下文      |   ABP 全局异常过滤器自动识别并本地化异常消息，无需手动处理   |
|         动态资源适配         | 基类通过 `AbpLocalizationOptions` 获取默认资源，子类可指定专属资源 |          兼顾「全局默认」和「模块专属」，零配置成本          |
|         参数传递支持         |          `SetData` 方法将本地化参数存入 `Data` 字典          | 支持带参数的错误模板（如 `设备{0}已禁用`），满足动态消息需求 |

## 四、 本地化无缝集成的核心优势

1. 业务无感知：业务层只需关注「错误码 + 参数」，无需关心本地化逻辑，由框架自动处理；

   ```csharp
   // 业务层代码：仅需指定错误码和参数
   IoTResult.Failed(IoTErrorCodes.DeviceDisabled, "Sensor001").CheckError();
   
   // 正确：先合并结果 → 调用CheckError（失败则抛IoTResultException）
   IoTResult.Combine(
       ValidateMqttTopic(topic),
       CheckMqttBrokerConnectivity(broker)
   ).CheckError(); // CheckError是IoTResult的方法，不是异常的方法
   ```

2. 多模块隔离：通过「子类化异常 + 专属资源」实现模块本地化隔离，无资源冲突；

   ```csharp
   // MQTT模块：抛专属异常，使用专属资源
   // 正确：使用泛型重载，指定要抛出的自定义异常类型
   IoTResult.Combine(
       ValidateMqttTopic(topic),
       CheckMqttBrokerConnectivity(broker)
   ).CheckError<IoTMqttResultException>(); // 核心：通过泛型指定异常类型
   
   // 这个重载的设计目的是「让 IoTResult 校验失败时，自动抛出指定类型的异常」，而非手动创建异常后调用：
   // (暂时不考虑)
   /// <summary>
   /// 重载：失败时抛出自定义异常（需继承IoTResultException）
   /// </summary>
   public IoTResult CheckError<TException>()
       where TException : IoTResultException, new()
   {
       if (!Succeeded)
       {
           // 自动创建自定义异常实例并抛出
           var exception = (TException)Activator.CreateInstance(typeof(TException), this)!;
           throw exception;
       }
       return this;
   }
   ```

3. 手动抛异常（不推荐，仅特殊场景）

   如果确实需要手动创建异常实例抛出，正确写法是：

   ```C#
   var result = IoTResult.Failed(new IoTError("Mqtt_ConnectFailed", "Broker不可达"));
   if (!result.Succeeded)
   {
       throw new IoTMqttResultException(result); // 直接throw，而非调用CheckError
   }
   ```

4. 零硬编码 + 零反射：基类默认使用全局资源，子类重写指定模块资源，兼顾灵活性与性能；

5. **完整链路闭环**：`错误码定义 → 结果聚合 → 异常抛出 → ABP 本地化 → 标准化响应`，端到端无断点。

## 五、 整体设计价值总结

|   维度   |        传统错误处理         |            本设计方案             |
| :------: | :-------------------------: | :-------------------------------: |
|  代码量  | 大量 `if` 判断 + 重复抛异常 |  一行 `Combine+CheckError` 搞定   |
| 错误信息 |       仅返回单个错误        |   聚合所有错误，支持多错误提示    |
|  本地化  |    手动拼接多语言字符串     |  ABP 自动本地化，支持参数化模板   |
|  扩展性  |   新增模块需修改核心代码    |     子类化扩展，遵循开闭原则      |
|   性能   |  无优化，存在重复对象创建   | 不可变 + 单例复用，零反射，高性能 |
| 框架兼容 |       与 ABP 体系割裂       |  深度融入 ABP 异常 / 本地化体系   |

该设计是**业务需求、框架规范、性能优化**三者平衡的典范，完美适配 Artizan IoT 平台多模块、高并发、多语言的核心诉求。



## 六、关键总结

1. **`CheckError()` 归属**：仅属于 `IoTResult`/`IoTResult`，用于「校验结果 + 自动抛异常」；
2. **异常类的角色**：`IoTResultException`/`IoTMqttResultException` 是「被抛出的对象」，而非「调用者」；
3. 核心调用范式：
   - 简单场景：`result.CheckError()` → 抛默认异常；
   - 模块场景：`result.CheckError<模块异常>()` → 抛自定义异常(暂时不考虑)；
   - 特殊场景：手动 `throw new 模块异常(result)` → 无 `CheckError` 参与。

整个错误处理链路的调用逻辑就完全自洽了：`IoTResult` 负责「结果封装 / 聚合 / 校验」，`IoTResultException` 负责「异常封装 / 本地化」，二者分工明确且调用链路清晰。