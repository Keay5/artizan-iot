# 问

 \- 根据我们上面的讨论，列出最终的完整版代码。 - 新建的类，推荐类的创建位置目录。 - 关键代码和类添加必要的详细注释，特别是标明设计思路、设计理念，为什么这样设计，是基于什么考虑、设计考量、用了什么设计模式  - 代码规范：如：if语句只有一行也得使用{} - 新增关键代码和类的详细注释，特别是设计思路、理念、原因、考量以及使用的设计模式，还要遵循代码规范，比如 if 语句即使一行也要用 {}。 - 注意优化异常处理、日志等 -----总结和梳理--------------- - 以上是我的想法，你还有什么好的设计或想法可尽量补充。 根据我们上面的讨论，你重新梳理：讨论点、需求、设计变更，逐一罗列出来，以便我排查是否有疏漏、或者有什么可以继续补充。 你也可以根据你丰富的软件架构经验，设计思路、设计理念，设计考量，设计模式等因数进行补充和优化。 

基于Volo.Abp框架分别创建项目  Artizan.IoT.TimeSeries.Abstractions Artizan.IoT.TimeSeries.InfluxDB2 Artizan.IoT.TimeSeries.InfluxDB3 不用适配 InfluxDB V1 



# 一、完整代码实现（基于 Volo.Abp 框架）

## 项目结构与目录规范

```plaintext
├── Artizan.IoT.TimeSeries.Abstractions/  // 抽象层项目
│   ├── Contracts/                        // 核心契约接口
│   │   ├── ITimeSeriesDataRepository.cs
│   │   ├── ITimeSeriesDataReader.cs
│   │   ├── ITimeSeriesDataWriter.cs
│   │   ├── ITimeSeriesTransactional.cs
│   │   ├── ITimeSeriesIndexManager.cs
│   │   └── ITimeSeriesStorageContract.cs
│   ├── Models/                           // 数据模型与参数对象
│   │   ├── TimeSeriesData.cs
│   │   ├── TimeSeriesQueryCriteria.cs
│   │   ├── TimeSeriesAggregateCriteria.cs
│   │   ├── TimeRange.cs
│   │   ├── TimeSeriesWriteOptions.cs
│   │   ├── TimeSeriesWriteResult.cs
│   │   ├── InfluxDbTimeSeriesRepositoryTimeSeriesBatchWriteResult.cs
│   │   └── TimeSeriesDeleteResult.cs
│   ├── Enums/                            // 枚举定义
│   │   ├── TimeSeriesAggregateType.cs
│   │   ├── TimeSeriesConsistencyLevel.cs
│   │   └── CompressionAlgorithm.cs
│   ├── Exceptions/                       // 自定义异常
│   │   ├── TimeSeriesDataException.cs
│   │   ├── TimeSeriesDataWriteException.cs
│   │   └── TimeSeriesDataQueryException.cs
│   └── Constants/                        // 常量定义
│       └── TimeSeriesConsts.cs
├── Artizan.IoT.TimeSeries.InfluxDB2/     // InfluxDB2实现项目
│   ├── Factories/                        // 客户端工厂
│   │   ├── IInfluxDbClientFactory.cs
│   │   └── InfluxDb2ClientFactory.cs
│   ├── Options/                          // 配置选项
│   │   ├── InfluxDb2Options.cs
│   │   └── InfluxDb2OptionsValidator.cs
│   ├── Repositories/                     // 仓储实现
│   │   └── InfluxDb2TimeSeriesRepository.cs
│   └── InfluxDB2Module.cs                // ABP模块类
└── Artizan.IoT.TimeSeries.InfluxDB3/     // InfluxDB3实现项目（结构同InfluxDB2）
    ├── Factories/
    │   ├── IInfluxDbClientFactory.cs
    │   └── InfluxDb3ClientFactory.cs
    ├── Options/
    │   ├── InfluxDb3Options.cs
    │   └── InfluxDb3OptionsValidator.cs
    ├── Repositories/
    │   └── InfluxDb3TimeSeriesRepository.cs
    └── InfluxDB3Module.cs
```



## 1. Artizan.IoT.TimeSeries.Abstractions 项目代码

### 1.1 常量定义（Constants/TimeSeriesConsts.cs）

```C#
using System;

namespace Artizan.IoT.TimeSeries.Abstractions.Constants
{
    /// <summary>
    /// 时序数据常量定义
    /// 设计思路：集中管理魔法值，提升代码可维护性
    /// 设计考量：避免硬编码，统一默认值配置，便于全局修改
    /// </summary>
    public static class TimeSeriesConsts
    {
        /// <summary>
        /// 默认测量表名
        /// </summary>
        public const string DefaultMeasurementName = "iot_telemetry";

        /// <summary>
        /// 默认查询条数限制
        /// </summary>
        public const int DefaultQueryLimit = 1000;

        /// <summary>
        /// 默认时间窗口（1分钟）
        /// </summary>
        public const string DefaultTimeWindow = "1m";

        /// <summary>
        /// 默认是否启用压缩
        /// </summary>
        public const bool DefaultCompressionEnabled = true;

        /// <summary>
        /// 默认压缩级别（Gzip中等压缩）
        /// </summary>
        public const int DefaultCompressionLevel = 6;

        /// <summary>
        /// 默认压缩算法（Lz4：时序库高性能压缩）
        /// </summary>
        public const CompressionAlgorithm DefaultCompressionAlgorithm = CompressionAlgorithm.Lz4;

        /// <summary>
        /// 默认批量写入阈值
        /// </summary>
        public const int DefaultBatchThreshold = 1000;
    }
}
```



### 1.2 枚举定义（Enums/TimeSeriesAggregateType.cs）

```C#
namespace Artizan.IoT.TimeSeries.Abstractions.Enums
{
    /// <summary>
    /// 时序数据聚合类型枚举
    /// 设计思路：标准化聚合操作类型，避免字符串硬编码
    /// 设计考量：适配不同时序库的聚合函数，统一上层调用接口
    /// </summary>
    public enum TimeSeriesAggregateType
    {
        /// <summary>
        /// 求和
        /// </summary>
        Sum,

        /// <summary>
        /// 平均值
        /// </summary>
        Avg,

        /// <summary>
        /// 最大值
        /// </summary>
        Max,

        /// <summary>
        /// 最小值
        /// </summary>
        Min,

        /// <summary>
        /// 计数
        /// </summary>
        Count,

        /// <summary>
        /// 第一条数据
        /// </summary>
        First,

        /// <summary>
        /// 最后一条数据
        /// </summary>
        Last
    }

    /// <summary>
    /// 时序数据一致性级别
    /// 设计思路：适配不同业务场景的一致性要求
    /// 设计考量：IoT场景下写多读少，区分最终一致性（高吞吐）和强一致性（核心数据）
    /// </summary>
    public enum TimeSeriesConsistencyLevel
    {
        /// <summary>
        /// 最终一致性（高吞吐，适合非核心数据）
        /// </summary>
        Eventual,

        /// <summary>
        /// 强一致性（写入成功即持久化，适合核心数据）
        /// </summary>
        Strong
    }

    /// <summary>
    /// 压缩算法枚举
    /// 设计思路：抽象压缩算法类型，支持可插拔切换
    /// 设计考量：时序数据量大，压缩能降低存储和传输成本，不同算法权衡压缩率和性能
    /// </summary>
    public enum CompressionAlgorithm
    {
        /// <summary>
        /// 不压缩
        /// </summary>
        None,

        /// <summary>
        /// Gzip压缩（平衡压缩率和性能）
        /// </summary>
        Gzip,

        /// <summary>
        /// Lz4压缩（时序库首选，高性能）
        /// </summary>
        Lz4,

        /// <summary>
        /// Snappy压缩（Google开源，低延迟）
        /// </summary>
        Snappy
    }
}
```



### 1.3 自定义异常（Exceptions/TimeSeriesDataException.cs）

```c#
using System;

namespace Artizan.IoT.TimeSeries.Abstractions.Exceptions
{
    /// <summary>
    /// 时序数据操作异常基类
    /// 设计思路：自定义异常体系，便于上层精准捕获和处理
    /// 设计模式：异常分层设计，基类统一处理通用逻辑，子类区分具体异常类型
    /// 设计考量：避免捕获通用Exception，提升异常处理的精准性和可维护性
    /// </summary>
    public class TimeSeriesDataException : Exception
    {
        /// <summary>
        /// 异常关联的物标识
        /// </summary>
        public string? ThingIdentifier { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        public TimeSeriesDataException(string message) : base(message)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">内部异常</param>
        public TimeSeriesDataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="thingIdentifier">关联的物标识</param>
        /// <param name="innerException">内部异常</param>
        public TimeSeriesDataException(string message, string thingIdentifier, Exception innerException) : base(message, innerException)
        {
            ThingIdentifier = thingIdentifier;
        }
    }

    /// <summary>
    /// 时序数据写入异常
    /// 设计思路：区分写入异常类型，便于针对性处理（如重试、告警）
    /// </summary>
    public class TimeSeriesDataWriteException : TimeSeriesDataException
    {
        public TimeSeriesDataWriteException(string message) : base(message)
        {
        }

        public TimeSeriesDataWriteException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public TimeSeriesDataWriteException(string message, string thingIdentifier, Exception innerException) : base(message, thingIdentifier, innerException)
        {
        }
    }

    /// <summary>
    /// 时序数据查询异常
    /// 设计思路：区分查询异常类型，便于针对性处理（如超时重试、降级）
    /// </summary>
    public class TimeSeriesDataQueryException : TimeSeriesDataException
    {
        public TimeSeriesDataQueryException(string message) : base(message)
        {
        }

        public TimeSeriesDataQueryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public TimeSeriesDataQueryException(string message, string thingIdentifier, Exception innerException) : base(message, thingIdentifier, innerException)
        {
        }
    }
}
```

### 1.4 数据模型（Models/TimeSeriesData.cs）

```
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using Artizan.IoT.TimeSeries.Abstractions.Constants;

namespace Artizan.IoT.TimeSeries.Abstractions.Models
{
    /// <summary>
    /// IoT时序数据通用模型
    /// 设计思路：适配时序库Tag/Field特性，兼顾类型安全和扩展性
    /// 设计模式：使用只读集合+方法修改的方式，保证数据不可变（Immutable）
    /// 设计考量：
    /// 1. Tags作为索引字段，使用字符串键值对，适配所有时序库的Tag特性
    /// 2. Fields存储数值型数据，支持多类型值，满足IoT多指标采集需求
    /// 3. 区分采集时间和入库时间，适配IoT设备离线上报场景
    /// 4. 只读集合防止外部随意修改，通过专用方法保证修改的可控性
    /// </summary>
    public class TimeSeriesData
    {
        /// <summary>
        /// 物唯一标识（如ProductKey+DeviceName）
        /// </summary>
        [Required(ErrorMessage = "物唯一标识不能为空")]
        public string ThingIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// 数据采集时间戳（UTC时间，设备端采集时间）
        /// </summary>
        [Required(ErrorMessage = "采集时间不能为空")]
        public DateTime UtcDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 扩展标签（时序库索引字段，用于快速筛选）
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags { get; private set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        /// <summary>
        /// 数值字段（时序库数值字段，用于聚合计算）
        /// </summary>
        public IReadOnlyDictionary<string, object> Fields { get; private set; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        /// <summary>
        /// 测量表名（InfluxDB Measurement / TDengine 超级表名）
        /// </summary>
        public string Measurement { get; set; } = TimeSeriesConsts.DefaultMeasurementName;

        /// <summary>
        /// 数据入库时间（UTC时间，区别于设备采集时间）
        /// </summary>
        public DateTime InsertedUtcTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 设置标签（类型安全的修改方式）
        /// </summary>
        /// <param name="key">标签键</param>
        /// <param name="value">标签值</param>
        /// <exception cref="ArgumentNullException">键为空时抛出</exception>
        public void SetTag(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key), "标签键不能为空");
            }

            var tags = new Dictionary<string, string>(Tags);
            tags[key] = value ?? string.Empty;
            Tags = new ReadOnlyDictionary<string, string>(tags);
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        /// <param name="key">标签键</param>
        /// <exception cref="ArgumentNullException">键为空时抛出</exception>
        public void RemoveTag(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key), "标签键不能为空");
            }

            var tags = new Dictionary<string, string>(Tags);
            if (tags.ContainsKey(key))
            {
                tags.Remove(key);
                Tags = new ReadOnlyDictionary<string, string>(tags);
            }
        }

        /// <summary>
        /// 设置字段值（泛型方法保证类型安全）
        /// </summary>
        /// <typeparam name="T">字段值类型（仅支持数值类型）</typeparam>
        /// <param name="key">字段键</param>
        /// <param name="value">字段值</param>
        /// <exception cref="ArgumentNullException">键为空时抛出</exception>
        /// <exception cref="ArgumentException">值类型非法时抛出</exception>
        public void SetField<T>(string key, T value)
            where T : struct, IComparable, IConvertible, IFormattable
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key), "字段键不能为空");
            }

            // 仅允许数值类型
            Type type = typeof(T);
            if (type != typeof(int) && type != typeof(long) && type != typeof(float) && 
                type != typeof(double) && type != typeof(decimal) && type != typeof(short))
            {
                throw new ArgumentException($"字段值类型{type.Name}不支持，仅支持数值类型", nameof(value));
            }

            var fields = new Dictionary<string, object>(Fields);
            fields[key] = value;
            Fields = new ReadOnlyDictionary<string, object>(fields);
        }

        /// <summary>
        /// 移除字段
        /// </summary>
        /// <param name="key">字段键</param>
        /// <exception cref="ArgumentNullException">键为空时抛出</exception>
        public void RemoveField(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key), "字段键不能为空");
            }

            var fields = new Dictionary<string, object>(Fields);
            if (fields.ContainsKey(key))
            {
                fields.Remove(key);
                Fields = new ReadOnlyDictionary<string, object>(fields);
            }
        }
    }
}
```

### 1.5 查询条件模型（Models/TimeSeriesQueryCriteria.cs）

```C#
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Artizan.IoT.TimeSeries.Abstractions.Constants;

namespace Artizan.IoT.TimeSeries.Abstractions.Models
{
    /// <summary>
    /// 时间范围对象
    /// 设计思路：封装时间范围，提供有效性校验
    /// 设计考量：时序数据查询必带时间范围，统一校验逻辑避免重复代码
    /// </summary>
    public record TimeRange(DateTime StartTimeUtc, DateTime EndTimeUtc)
    {
        /// <summary>
        /// 验证时间范围是否有效
        /// </summary>
        public bool IsValid => EndTimeUtc > StartTimeUtc;

        /// <summary>
        /// 获取时间范围的时长
        /// </summary>
        public TimeSpan Duration => EndTimeUtc - StartTimeUtc;
    }

    /// <summary>
    /// 时序数据查询条件
    /// 设计思路：使用强类型对象封装查询参数，替代零散参数
    /// 设计模式：参数对象模式（Parameter Object）
    /// 设计考量：
    /// 1. 减少方法参数数量，提升代码可读性
    /// 2. 便于扩展新的查询条件，无需修改方法签名
    /// 3. 内置参数校验，提前发现非法查询条件
    /// </summary>
    public class TimeSeriesQueryCriteria
    {
        /// <summary>
        /// 物唯一标识（必填）
        /// </summary>
        [Required(ErrorMessage = "物唯一标识不能为空")]
        public string ThingIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// 时间范围（必填）
        /// </summary>
        [Required(ErrorMessage = "查询时间范围不能为空")]
        public TimeRange TimeRange { get; set; } = new TimeRange(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

        /// <summary>
        /// 测量表名（为空时使用默认值）
        /// </summary>
        public string? Measurement { get; set; }

        /// <summary>
        /// 要查询的字段列表（为空则查询所有字段）
        /// </summary>
        public IList<string> FieldNames { get; set; } = new List<string>();

        /// <summary>
        /// 标签筛选条件（时序库索引查询，高性能）
        /// </summary>
        public IDictionary<string, string> TagFilters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 数据条数限制（防止海量数据查询）
        /// </summary>
        public int Limit { get; set; } = TimeSeriesConsts.DefaultQueryLimit;

        /// <summary>
        /// 是否按时间戳降序排列
        /// </summary>
        public bool OrderByDescending { get; set; } = true;

        /// <summary>
        /// 验证查询条件是否有效
        /// </summary>
        /// <exception cref="ArgumentException">条件无效时抛出</exception>
        public void Validate()
        {
            if (!TimeRange.IsValid)
            {
                throw new ArgumentException("查询时间范围无效，结束时间必须大于开始时间", nameof(TimeRange));
            }

            if (Limit <= 0 || Limit > 10000)
            {
                throw new ArgumentException($"查询条数限制必须在1-10000之间，当前值：{Limit}", nameof(Limit));
            }
        }
    }

    /// <summary>
    /// 时序数据聚合查询条件
    /// 设计思路：继承基础查询条件，扩展聚合相关参数
    /// 设计模式：继承+扩展，保持聚合查询和基础查询的一致性
    /// 设计考量：聚合查询是基础查询的扩展，复用基础参数，减少代码冗余
    /// </summary>
    public class TimeSeriesAggregateCriteria : TimeSeriesQueryCriteria
    {
        /// <summary>
        /// 聚合类型（必填）
        /// </summary>
        [Required(ErrorMessage = "聚合类型不能为空")]
        public TimeSeriesAggregateType AggregateType { get; set; }

        /// <summary>
        /// 聚合字段名（必填）
        /// </summary>
        [Required(ErrorMessage = "聚合字段名不能为空")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 时间窗口（如 1m/5m/1h，遵循时序库标准格式）
        /// </summary>
        [Required(ErrorMessage = "聚合时间窗口不能为空")]
        public string TimeWindow { get; set; } = TimeSeriesConsts.DefaultTimeWindow;

        /// <summary>
        /// 验证聚合查询条件是否有效
        /// </summary>
        /// <exception cref="ArgumentException">条件无效时抛出</exception>
        public new void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(FieldName))
            {
                throw new ArgumentException("聚合字段名不能为空", nameof(FieldName));
            }

            if (string.IsNullOrWhiteSpace(TimeWindow))
            {
                throw new ArgumentException("聚合时间窗口不能为空", nameof(TimeWindow));
            }
        }
    }

    /// <summary>
    /// 时序数据删除条件
    /// 设计思路：极简设计，仅包含删除必需的参数
    /// 设计考量：时序数据建议归档而非删除，限制删除条件，降低误删风险
    /// </summary>
    public class TimeSeriesDeleteCriteria
    {
        /// <summary>
        /// 物唯一标识（必填）
        /// </summary>
        [Required(ErrorMessage = "物唯一标识不能为空")]
        public string ThingIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// 时间范围（必填）
        /// </summary>
        [Required(ErrorMessage = "删除时间范围不能为空")]
        public TimeRange TimeRange { get; set; } = new TimeRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        /// <summary>
        /// 测量表名（为空时使用默认值）
        /// </summary>
        public string? Measurement { get; set; }

        /// <summary>
        /// 验证删除条件是否有效
        /// </summary>
        /// <exception cref="ArgumentException">条件无效时抛出</exception>
        public void Validate()
        {
            if (!TimeRange.IsValid)
            {
                throw new ArgumentException("删除时间范围无效，结束时间必须大于开始时间", nameof(TimeRange));
            }

            // 限制删除时间范围，最大不超过30天，降低误删风险
            if (TimeRange.Duration > TimeSpan.FromDays(30))
            {
                throw new ArgumentException("删除时间范围不能超过30天，如需删除更多数据请联系管理员", nameof(TimeRange));
            }
        }
    }
}
```

### 1.6 写入配置与结果模型（Models/TimeSeriesWriteOptions.cs）



```C#
using System;
using Artizan.IoT.TimeSeries.Abstractions.Constants;
using Artizan.IoT.TimeSeries.Abstractions.Enums;

namespace Artizan.IoT.TimeSeries.Abstractions.Models
{
    /// <summary>
    /// 时序数据写入选项
    /// 设计思路：封装写入行为配置，支持精细化控制写入策略
    /// 设计模式：选项模式（Options Pattern），适配.NET配置系统
    /// 设计考量：
    /// 1. 支持幂等写入，避免重复数据
    /// 2. 可配置压缩策略，平衡性能和存储
    /// 3. 区分一致性级别，适配不同业务场景
    /// 4. 批量写入阈值，支持本地缓存批量提交
    /// </summary>
    public class TimeSeriesWriteOptions
    {
        /// <summary>
        /// 是否启用幂等写入（通过 ThingIdentifier + UtcDateTime 去重）
        /// </summary>
        public bool EnableIdempotency { get; set; } = true;

        /// <summary>
        /// 是否启用数据压缩
        /// </summary>
        public bool EnableCompression { get; set; } = TimeSeriesConsts.DefaultCompressionEnabled;

        /// <summary>
        /// 压缩算法
        /// </summary>
        public CompressionAlgorithm CompressionAlgorithm { get; set; } = TimeSeriesConsts.DefaultCompressionAlgorithm;

        /// <summary>
        /// 压缩级别（1-9，越高压缩率越高但耗时越长）
        /// </summary>
        public int CompressionLevel { get; set; } = TimeSeriesConsts.DefaultCompressionLevel;

        /// <summary>
        /// 写入一致性级别
        /// </summary>
        public TimeSeriesConsistencyLevel ConsistencyLevel { get; set; } = TimeSeriesConsistencyLevel.Eventual;

        /// <summary>
        /// 批量写入阈值（小于该值时本地缓存，达到阈值后批量提交）
        /// </summary>
        public int BatchThreshold { get; set; } = TimeSeriesConsts.DefaultBatchThreshold;

        /// <summary>
        /// 写入超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// 单条写入结果
    /// 设计思路：结构化返回写入结果，包含丰富的状态信息
    /// 设计考量：
    /// 1. 明确写入是否成功
    /// 2. 标记重复数据（幂等写入场景）
    /// 3. 记录写入时间，便于问题排查
    /// </summary>
    public class TimeSeriesWriteResult
    {
        /// <summary>
        /// 是否写入成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息（失败时非空）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 存储引擎生成的唯一ID（不同引擎类型不同）
        /// </summary>
        public string DataPointId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 实际写入时间（UTC）
        /// </summary>
        public DateTime WrittenTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否为幂等去重的重复数据
        /// </summary>
        public bool IsDuplicate { get; set; }
    }

    /// <summary>
    /// 批量写入结果
    /// 设计思路：批量操作需区分成功/失败条数，便于监控和重试
    /// 设计考量：
    /// 1. 统计总条数、成功条数、失败条数
    /// 2. 记录错误信息，便于定位失败原因
    /// 3. 批量写入时间，用于性能分析
    /// </summary>
    public class TimeSeriesBatchWriteResult
    {
        /// <summary>
        /// 总提交条数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功写入条数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败条数
        /// </summary>
        public int FailedCount => TotalCount - SuccessCount;

        /// <summary>
        /// 错误消息列表（失败时非空）
        /// </summary>
        public IList<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// 批量写入完成时间（UTC）
        /// </summary>
        public DateTime BatchWrittenTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 批量ID（用于追踪整个批量操作）
        /// </summary>
        public string BatchId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 删除操作结果
    /// 设计思路：删除操作需明确是否成功及影响条数
    /// 设计考量：部分时序库不返回删除条数，用-1标识未知
    /// </summary>
    public class TimeSeriesDeleteResult
    {
        /// <summary>
        /// 是否删除成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 成功删除的条数（-1表示无法获取）
        /// </summary>
        public int DeletedCount { get; set; } = -1;

        /// <summary>
        /// 错误消息（失败时非空）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
```

### 1.7 核心契约接口（Contracts/ITimeSeriesStorageContract.cs）

```C#
using System;
using System.Threading;
using System.Threading.Tasks;
using Artizan.IoT.TimeSeries.Abstractions.Models;

namespace Artizan.IoT.TimeSeries.Abstractions.Contracts
{
    /// <summary>
    /// 时序数据存储基础契约
    /// 设计思路：定义所有存储实现必须遵守的核心契约
    /// 设计模式：接口隔离原则（ISP），最小接口设计
    /// 设计考量：
    /// 1. 所有实现必须支持健康检查，便于监控
    /// 2. 统一标识存储引擎类型，便于日志和监控
    /// 3. 实现IDisposable，保证资源释放
    /// </summary>
    public interface ITimeSeriesStorageContract : IDisposable
    {
        /// <summary>
        /// 存储引擎名称（如 InfluxDB v2、InfluxDB v3）
        /// </summary>
        string StorageEngine { get; }

        /// <summary>
        /// 检查存储连接状态
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接是否正常</returns>
        Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 时序数据读取接口
    /// 设计思路：分离读写接口，遵循接口隔离原则
    /// 设计模式：接口隔离模式（ISP）
    /// 设计考量：
    /// 1. 只读场景只需依赖此接口，降低耦合
    /// 2. 统一查询和聚合方法签名，便于替换实现
    /// </summary>
    public interface ITimeSeriesDataReader : ITimeSeriesStorageContract
    {
        /// <summary>
        /// 按条件查询原始数据
        /// </summary>
        /// <param name="criteria">查询条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>时序数据列表</returns>
        Task<IReadOnlyList<TimeSeriesData>> QueryAsync(
            TimeSeriesQueryCriteria criteria,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 按条件聚合查询
        /// </summary>
        /// <param name="criteria">聚合查询条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聚合结果（键：窗口起始时间，值：聚合值）</returns>
        Task<IReadOnlyDictionary<DateTime, double>> AggregateAsync(
            TimeSeriesAggregateCriteria criteria,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 时序数据写入接口
    /// 设计思路：分离读写接口，支持只写场景的极简实现
    /// 设计模式：接口隔离模式（ISP）
    /// 设计考量：
    /// 1. IoT场景写多读少，单独封装写入接口提升性能
    /// 2. 区分单条和批量写入，批量写入优化性能
    /// 3. 删除方法增加警告，引导归档而非删除
    /// </summary>
    public interface ITimeSeriesDataWriter : ITimeSeriesStorageContract
    {
        /// <summary>
        /// 单条写入（支持幂等）
        /// </summary>
        /// <param name="data">时序数据</param>
        /// <param name="options">写入选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>写入结果</returns>
        Task<TimeSeriesWriteResult> WriteAsync(
            TimeSeriesData data,
            TimeSeriesWriteOptions options = default,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量写入（高并发场景首选）
        /// </summary>
        /// <param name="dataList">时序数据列表</param>
        /// <param name="options">写入选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量写入结果</returns>
        Task<TimeSeriesBatchWriteResult> BatchWriteAsync(
            IEnumerable<TimeSeriesData> dataList,
            TimeSeriesWriteOptions options = default,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 按条件删除数据（慎用，时序数据建议归档而非删除）
        /// </summary>
        /// <param name="criteria">删除条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        Task<TimeSeriesDeleteResult> DeleteAsync(
            TimeSeriesDeleteCriteria criteria,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 时序数据事务接口
    /// 设计思路：适配支持事务的存储引擎，统一事务接口
    /// 设计模式：事务模式（Transaction Pattern）
    /// 设计考量：
    /// 1. 部分时序库（如TimescaleDB）支持事务，需统一接口
    /// 2. 不支持事务的引擎需抛出明确异常，避免误用
    /// 3. 事务接口需支持异步释放，符合.NET最佳实践
    /// </summary>
    public interface ITimeSeriesTransactional : ITimeSeriesStorageContract
    {
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="isolationLevel">隔离级别</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>事务对象</returns>
        Task<ITimeSeriesTransaction> BeginTransactionAsync(
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 时序数据事务对象
    /// 设计思路：封装事务操作，支持提交/回滚
    /// 设计模式：事务模式（Transaction Pattern）
    /// 设计考量：
    /// 1. 事务ID用于追踪和日志
    /// 2. 支持异步释放，避免资源泄露
    /// 3. 仅包含事务内的核心操作
    /// </summary>
    public interface ITimeSeriesTransaction : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 事务ID
        /// </summary>
        string TransactionId { get; }

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>提交结果</returns>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>回滚结果</returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 事务内单条写入
        /// </summary>
        /// <param name="data">时序数据</param>
        /// <param name="options">写入选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>写入结果</returns>
        Task<TimeSeriesWriteResult> WriteAsync(
            TimeSeriesData data,
            TimeSeriesWriteOptions options = default,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 事务内批量写入
        /// </summary>
        /// <param name="dataList">时序数据列表</param>
        /// <param name="options">写入选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量写入结果</returns>
        Task<TimeSeriesBatchWriteResult> BatchWriteAsync(
            IEnumerable<TimeSeriesData> dataList,
            TimeSeriesWriteOptions options = default,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 时序数据索引管理接口
    /// 设计思路：抽象索引管理操作，适配不同引擎的索引特性
    /// 设计模式：策略模式（Strategy Pattern）
    /// 设计考量：
    /// 1. 不同时序库索引语法不同，通过接口统一
    /// 2. 支持创建/删除索引，优化查询性能
    /// 3. 索引策略可插拔，便于扩展
    /// </summary>
    public interface ITimeSeriesIndexManager : ITimeSeriesStorageContract
    {
        /// <summary>
        /// 创建索引
        /// </summary>TimeSeriesBatchWriteResult
        /// <param name="strategies">索引策略列表</param>
        /// <param name="measurement">测量表名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建结果</returns>
        Task CreateIndexAsync(
            IEnumerable<ITimeSeriesIndexStrategy> strategies,
            string measurement,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="indexName">索引名称</param>
        /// <param name="measurement">测量表名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        Task DropIndexAsync(
            string indexName,
            string measurement,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 索引策略接口
    /// 设计思路：封装索引创建逻辑，支持不同引擎的差异化实现
    /// 设计模式：策略模式（Strategy Pattern）
    /// 设计考量：
    /// 1. 索引名称和字段标准化
    /// 2. 生成特定引擎的创建语句
    /// 3. 便于扩展新的索引策略
    /// </summary>
    public interface ITimeSeriesIndexStrategy
    {
        /// <summary>
        /// 索引名称
        /// </summary>
        string IndexName { get; }

        /// <summary>
        /// 索引字段列表
        /// </summary>
        IList<string> IndexFields { get; }

        /// <summary>
        /// 生成创建索引的命令
        /// </summary>
        /// <param name="measurement">测量表名</param>
        /// <returns>索引创建命令</returns>
        string GenerateCreateIndexCommand(string measurement);
    }

    /// <summary>
    /// 完整仓储接口（组合所有能力）
    /// 设计思路：组合读写、事务、索引管理接口，提供完整能力
    /// 设计模式：接口组合模式（Interface Composition）
    /// 设计考量：
    /// 1. 上层应用可直接依赖此接口，获取完整能力
    /// 2. 底层实现可按需实现不同子接口
    /// 3. 符合里氏替换原则，可替换不同存储实现
    /// </summary>
    public interface ITimeSeriesDataRepository : ITimeSeriesDataReader, ITimeSeriesDataWriter, ITimeSeriesTransactional, ITimeSeriesIndexManager
    {
    }
}
```

## 2. Artizan.IoT.TimeSeries.InfluxDB2 项目代码

### 2.1 配置选项（Options/InfluxDb2Options.cs）

```C#
using System;
using Artizan.IoT.TimeSeries.Abstractions.Constants;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Options
{
    /// <summary>
    /// InfluxDB 2.x 配置选项
    /// 设计思路：继承基础配置，扩展InfluxDB2专属配置
    /// 设计模式：选项模式（Options Pattern），适配ABP配置系统
    /// 设计考量：
    /// 1. 区分通用配置和版本专属配置，便于扩展
    /// 2. 内置默认值，降低配置复杂度
    /// 3. 包含连接池和批量写入配置，优化性能
    /// </summary>
    public class InfluxDb2Options
    {
        /// <summary>
        /// InfluxDB服务地址（如 http://localhost:8086）
        /// </summary>
        public string Url { get; set; } = "http://localhost:8086";

        /// <summary>
        /// 认证Token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 组织ID
        /// </summary>
        public string Org { get; set; } = string.Empty;

        /// <summary>
        /// 桶名称（对应V1的Database）
        /// </summary>
        public string Bucket { get; set; } = "iot_telemetry";

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 查询超时时间（秒）
        /// </summary>
        public int QueryTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 批量写入批次大小
        /// </summary>
        public int BatchSize { get; set; } = TimeSeriesConsts.DefaultBatchThreshold;

        /// <summary>
        /// 批量写入刷新间隔（毫秒）
        /// </summary>
        public int FlushIntervalMs { get; set; } = 1000;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 重试间隔（毫秒）
        /// </summary>
        public int RetryIntervalMs { get; set; } = 5000;

        /// <summary>
        /// 每个服务器最大连接数（连接池配置）
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = 100;
    }

    /// <summary>
    /// InfluxDB2配置验证器
    /// 设计思路：提前验证配置有效性，避免运行时异常
    /// 设计模式：验证器模式（Validator Pattern）
    /// 设计考量：
    /// 1. 启动时验证核心配置，快速发现配置错误
    /// 2. 符合ABP配置验证规范，集成到配置系统
    /// </summary>
    public class InfluxDb2OptionsValidator : AbpOptionsValidator<InfluxDb2Options>
    {
        public override void Validate(InfluxDb2Options options)
        {
            if (string.IsNullOrWhiteSpace(options.Url))
            {
                ThrowValidationError($"{nameof(options.Url)} 不能为空");
            }

            if (string.IsNullOrWhiteSpace(options.Token))
            {
                ThrowValidationError($"{nameof(options.Token)} 不能为空");
            }

            if (string.IsNullOrWhiteSpace(options.Org))
            {
                ThrowValidationError($"{nameof(options.Org)} 不能为空");
            }

            if (string.IsNullOrWhiteSpace(options.Bucket))
            {
                ThrowValidationError($"{nameof(options.Bucket)} 不能为空");
            }

            if (options.TimeoutSeconds <= 0 || options.TimeoutSeconds > 300)
            {
                ThrowValidationError($"{nameof(options.TimeoutSeconds)} 必须在1-300之间");
            }

            if (options.BatchSize <= 0 || options.BatchSize > 10000)
            {
                ThrowValidationError($"{nameof(options.BatchSize)} 必须在1-10000之间");
            }
        }
    }
}
```

### 2.2 客户端工厂（Factories/IInfluxDbClientFactory.cs）

```C#
using System;
using System.Threading;
using System.Threading.Tasks;
using InfluxDB.Client;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Factories
{
    /// <summary>
    /// InfluxDB客户端工厂接口
    /// 设计思路：封装客户端创建逻辑，保证单例复用
    /// 设计模式：工厂模式（Factory Pattern）+ 单例模式（Singleton）
    /// 设计考量：
    /// 1. 客户端是重量级对象，需单例复用避免性能损耗
    /// 2. 懒加载创建，避免应用启动时不必要的资源消耗
    /// 3. 统一管理客户端生命周期，确保资源正确释放
    /// </summary>
    public interface IInfluxDbClientFactory : IDisposable
    {
        /// <summary>
        /// 获取客户端实例（单例）
        /// </summary>
        /// <returns>InfluxDBClient实例</returns>
        InfluxDBClient GetClient();

        /// <summary>
        /// 检查客户端连接状态
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接是否正常</returns>
        Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// InfluxDB 2.x 客户端工厂实现
    /// 设计思路：
    /// 1. 单例模式：标记ISingletonDependency，保证应用生命周期内仅一个实例
    /// 2. 懒加载：首次使用时创建客户端，避免启动开销
    /// 3. 线程安全：双重检查锁保证多线程下仅创建一次客户端
    /// 4. 资源管理：实现IDisposable，应用退出时释放客户端
    /// 设计模式：工厂模式 + 单例模式 + 懒加载模式
    /// 设计考量：
    /// - InfluxDBClient是线程安全的，适合单例复用
    /// - 连接池配置优化高并发场景的性能
    /// - 配置验证提前发现错误，避免运行时异常
    /// </summary>
    public class InfluxDb2ClientFactory : IInfluxDbClientFactory, ISingletonDependency
    {
        private readonly InfluxDb2Options _options;
        private InfluxDBClient _client;
        private readonly object _lockObj = new object();
        private readonly ILogger<InfluxDb2ClientFactory> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">InfluxDB2配置</param>
        /// <param name="logger">日志器</param>
        public InfluxDb2ClientFactory(
            IOptions<InfluxDb2Options> options,
            ILogger<InfluxDb2ClientFactory> logger)
        {
            _options = options.Value;
            _logger = logger;

            // 验证配置
            var validator = new InfluxDb2OptionsValidator();
            validator.Validate(_options);

            _logger.LogInformation("InfluxDb2ClientFactory initialized with Url: {Url}, Org: {Org}, Bucket: {Bucket}",
                _options.Url, _options.Org, _options.Bucket);
        }

        /// <summary>
        /// 线程安全的懒加载创建客户端
        /// 设计思路：双重检查锁（Double-Check Locking）保证线程安全且性能最优
        /// 设计考量：
        /// 1. 第一次检查避免每次获取都加锁，提升性能
        /// 2. 加锁保证多线程下仅创建一次
        /// 3. 第二次检查防止锁等待期间已创建客户端
        /// </summary>
        /// <returns>InfluxDBClient单例实例</returns>
        public InfluxDBClient GetClient()
        {
            // 第一次检查，无锁，提升性能
            if (_client != null)
            {
                return _client;
            }

            // 加锁保证线程安全
            lock (_lockObj)
            {
                // 第二次检查，防止锁等待期间已创建
                if (_client == null)
                {
                    _logger.LogInformation("Creating InfluxDB 2.x client for Url: {Url}", _options.Url);

                    try
                    {
                        // 自定义HTTP客户端，优化连接池
                        var httpClient = new HttpClient(new HttpClientHandler
                        {
                            MaxConnectionsPerServer = _options.MaxConnectionsPerServer,
                            KeepAlivePingTimeout = TimeSpan.FromMinutes(5),
                            UseCookies = false
                        })
                        {
                            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
                        };

                        // 创建InfluxDB客户端
                        _client = new InfluxDBClient(httpClient, new InfluxDBClientOptions(_options.Url)
                        {
                            Token = _options.Token,
                            Org = _options.Org,
                            Bucket = _options.Bucket,
                            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                            AllowHttpRedirects = true,
                            VerifySsl = true
                        });

                        _logger.LogInformation("InfluxDB 2.x client created successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create InfluxDB 2.x client");
                        throw new TimeSeriesDataException("创建InfluxDB 2.x客户端失败", ex);
                    }
                }
            }

            return _client;
        }

        /// <summary>
        /// 检查客户端连接健康状态
        /// 设计思路：通过Ping操作验证连接，提供统一的健康检查接口
        /// 设计考量：
        /// 1. 捕获异常返回false，避免健康检查导致应用崩溃
        /// 2. 记录日志，便于排查连接问题
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接是否正常</returns>
        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetClient();
                var isHealthy = await client.PingAsync(cancellationToken);

                if (isHealthy)
                {
                    _logger.LogInformation("InfluxDB 2.x client health check passed");
                }
                else
                {
                    _logger.LogWarning("InfluxDB 2.x client health check failed: Ping returned false");
                }

                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InfluxDB 2.x client health check failed");
                return false;
            }
        }

        /// <summary>
        /// 释放客户端资源
        /// 设计思路：单例工厂负责客户端的生命周期管理
        /// 设计考量：
        /// 1. 加锁保证线程安全，防止多线程同时释放
        /// 2. 释放后置空客户端，下次获取时重新创建
        /// 3. 记录日志，便于追踪资源释放
        /// </summary>
        public void Dispose()
        {
            lock (_lockObj)
            {
                if (_client != null)
                {
                    _logger.LogInformation("Disposing InfluxDB 2.x client");
                    _client.Dispose();
                    _client = null;
                    _logger.LogInformation("InfluxDB 2.x client disposed successfully");
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
```



### 2.3 仓储实现（Repositories/InfluxDb2TimeSeriesRepository.cs）

```C#
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Artizan.IoT.TimeSeries.Abstractions.Constants;
using Artizan.IoT.TimeSeries.Abstractions.Contracts;
using Artizan.IoT.TimeSeries.Abstractions.Enums;
using Artizan.IoT.TimeSeries.Abstractions.Exceptions;
using Artizan.IoT.TimeSeries.Abstractions.Models;
using Artizan.IoT.TimeSeries.InfluxDB2.Factories;
using Artizan.IoT.TimeSeries.InfluxDB2.Options;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Logging;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Repositories
{
    /// <summary>
    /// InfluxDB 2.x 时序数据仓储实现
    /// 设计思路：
    /// 1. 适配InfluxDB2的API和查询语法（Flux）
    /// 2. 封装底层实现，对外暴露统一的抽象接口
    /// 3. 优化异常处理和日志，提升可维护性
    /// 4. 适配IoT高并发写入场景，优化批量写入性能
    /// 设计模式：适配器模式（Adapter Pattern）- 将InfluxDB2的API适配到通用仓储接口
    /// 设计考量：
    /// - InfluxDB2社区版不支持事务，需明确抛出异常
    /// - Tag自动索引，无需手动创建索引
    /// - Flux查询语法需适配不同的查询条件
    /// - 批量写入使用官方WriteApiAsync，优化性能和重试
    /// </summary>
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ITimeSeriesDataRepository))]
    public class InfluxDb2TimeSeriesRepository : ITimeSeriesDataRepository, ITransientDependency
    {
        private readonly InfluxDb2Options _options;
        private readonly IInfluxDbClientFactory _clientFactory;
        private readonly ILogger<InfluxDb2TimeSeriesRepository> _logger;
        private WriteApiAsync _writeApi;
        private QueryApi _queryApi;

        /// <summary>
        /// 存储引擎标识
        /// </summary>
        public string StorageEngine => "InfluxDB 2.x Community";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">InfluxDB2配置</param>
        /// <param name="clientFactory">客户端工厂</param>
        /// <param name="logger">日志器</param>
        public InfluxDb2TimeSeriesRepository(
            IOptions<InfluxDb2Options> options,
            IInfluxDbClientFactory clientFactory,
            ILogger<InfluxDb2TimeSeriesRepository> logger)
        {
            _options = options.Value;
            _clientFactory = clientFactory;
            _logger = logger;

            // 初始化WriteApi和QueryApi
            InitializeApis();
        }

        /// <summary>
        /// 初始化InfluxDB API客户端
        /// 设计思路：复用工厂的单例客户端，初始化专用API
        /// 设计考量：WriteApiAsync是批量写入的核心，需配置合适的批量参数
        /// </summary>
        private void InitializeApis()
        {
            try
            {
                var client = _clientFactory.GetClient();

                // 初始化批量写入API
                _writeApi = client.GetWriteApiAsync(new WriteOptions
                {
                    BatchSize = _options.BatchSize,
                    FlushInterval = _options.FlushIntervalMs,
                    RetryInterval = _options.RetryIntervalMs,
                    JitterInterval = 1000,
                    MaxRetries = _options.MaxRetries,
                    MaxRetryDelay = (int)TimeSpan.FromSeconds(30).TotalMilliseconds,
                    ExponentialBase = 2
                });

                // 初始化查询API
                _queryApi = client.GetQueryApi();

                _logger.LogInformation("InfluxDB 2.x APIs initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize InfluxDB 2.x APIs");
                throw new TimeSeriesDataException("初始化InfluxDB 2.x API失败", ex);
            }
        }

        #region ITimeSeriesDataReader 实现
        /// <summary>
        /// 按条件查询原始数据
        /// 设计思路：将通用查询条件转换为Flux查询语句
        /// 设计考量：
        /// 1. Flux查询语法需适配时间范围、Tag筛选、字段筛选
        /// 2. 限制返回条数，防止海量数据查询
        /// 3. 转换查询结果为通用模型，屏蔽底层差异
        /// </summary>
        /// <param name="criteria">查询条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>时序数据列表</returns>
        /// <exception cref="ArgumentNullException">条件为空时抛出</exception>
        /// <exception cref="TimeSeriesDataQueryException">查询失败时抛出</exception>
        public async Task<IReadOnlyList<TimeSeriesData>> QueryAsync(
            TimeSeriesQueryCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            if (criteria == null)
            {
                throw new ArgumentNullException(nameof(criteria), "查询条件不能为空");
            }

            try
            {
                // 验证查询条件
                criteria.Validate();

                _logger.LogInformation("Starting InfluxDB 2.x query for thing: {ThingId}, time range: {StartTime} - {EndTime}",
                    criteria.ThingIdentifier,
                    criteria.TimeRange.StartTimeUtc,
                    criteria.TimeRange.EndTimeUtc);

                // 构建Flux查询语句
                var fluxQuery = BuildFluxQuery(criteria);
                _logger.LogDebug("Flux query: {FluxQuery}", fluxQuery);

                // 执行查询
                var tables = await _queryApi.QueryAsync(fluxQuery, _options.Org, cancellationToken);

                // 转换结果为通用模型
                var result = new List<TimeSeriesData>();
                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        result.Add(MapRecordToTimeSeriesData(record));
                    }
                }

                _logger.LogInformation("InfluxDB 2.x query completed, returned {Count} records for thing: {ThingId}",
                    result.Count, criteria.ThingIdentifier);

                return result.AsReadOnly();
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Query argument null: {ParamName}", ex.ParamName);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid query criteria for thing: {ThingId}", criteria.ThingIdentifier);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InfluxDB 2.x query failed for thing: {ThingId}", criteria.ThingIdentifier);
                throw new TimeSeriesDataQueryException("InfluxDB 2.x查询失败", criteria.ThingIdentifier, ex);
            }
        }

        /// <summary>
        /// 按条件聚合查询
        /// 设计思路：将通用聚合条件转换为Flux聚合查询
        /// 设计考量：
        /// 1. 不同聚合类型对应不同的Flux函数
        /// 2. 时间窗口需适配Flux的window函数语法
        /// 3. 聚合结果转换为通用字典格式
        /// </summary>
        /// <param name="criteria">聚合查询条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聚合结果</returns>
        /// <exception cref="ArgumentNullException">条件为空时抛出</exception>
        /// <exception cref="TimeSeriesDataQueryException">查询失败时抛出</exception>
        public async Task<IReadOnlyDictionary<DateTime, double>> AggregateAsync(
            TimeSeriesAggregateCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            if (criteria == null)
            {
                throw new ArgumentNullException(nameof(criteria), "聚合查询条件不能为空");
            }

            try
            {
                // 验证聚合条件
                criteria.Validate();

                _logger.LogInformation("Starting InfluxDB 2.x aggregate query for thing: {ThingId}, field: {Field}, aggregate: {Aggregate}, time window: {TimeWindow}",
                    criteria.ThingIdentifier,
                    criteria.FieldName,
                    criteria.AggregateType,
                    criteria.TimeWindow);

                // 构建聚合Flux查询
                var fluxQuery = BuildAggregateFluxQuery(criteria);
                _logger.LogDebug("Aggregate Flux query: {FluxQuery}", fluxQuery);

                // 执行查询
                var tables = await _queryApi.QueryAsync(fluxQuery, _options.Org, cancellationToken);

                // 转换聚合结果
                var result = new Dictionary<DateTime, double>();
                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        var time = record.GetTime().GetValueOrDefault().ToUniversalTime();
                        var value = Convert.ToDouble(record.GetValue(), CultureInfo.InvariantCulture);
                        result[time] = value;
                    }
                }

                _logger.LogInformation("InfluxDB 2.x aggregate query completed, returned {Count} aggregated records for thing: {ThingId}",
                    result.Count, criteria.ThingIdentifier);

                return new ReadOnlyDictionary<DateTime, double>(result);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Aggregate query argument null: {ParamName}", ex.ParamName);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid aggregate criteria for thing: {ThingId}", criteria.ThingIdentifier);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InfluxDB 2.x aggregate query failed for thing: {ThingId}", criteria.ThingIdentifier);
                throw new TimeSeriesDataQueryException("InfluxDB 2.x聚合查询失败", criteria.ThingIdentifier, ex);
            }
        }
        #endregion

        #region ITimeSeriesDataWriter 实现
        /// <summary>
        /// 单条写入时序数据
        /// 设计思路：转换通用模型为InfluxDB Point，使用WriteApi写入
        /// 设计考量：
        /// 1. 幂等写入通过ThingIdentifier+UtcDateTime去重
        /// 2. 捕获冲突异常，标记为重复数据
        /// 3. 返回结构化结果，包含写入状态和错误信息
        /// </summary>
        /// <param name="data">时序数据</param>
        /// <param name="options">写入选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>写入结果</returns>
        /// <exception cref="ArgumentNullException">数据为空时抛出</exception>
        /// <exception cref="TimeSeriesDataWriteException">写入失败时抛出</exception>
        public async Task<TimeSeriesWriteResult> WriteAsync(
            TimeSeriesData data,
            TimeSeriesWriteOptions options = default,
            CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "写入数据不能为空");
            }

            options ??= new TimeSeriesWriteOptions();

            try
            {
                _logger.LogDebug("Writing single data point to InfluxDB 2.x for thing: {ThingId}, time: {Time}",
                    data.ThingIdentifier, data.UtcDateTime);

                // 转换为InfluxDB Point
                var point = MapTimeSeriesDataToPoint(data, options);

                // 单条写入
                await _writeApi.WritePointAsync(point, _options.Bucket, _options.Org, cancellationToken);

                var result = new TimeSeriesWriteResult
                {
                    Success = true,
                    WrittenTimeUtc = DateTime.UtcNow,
                    DataPointId = $"{data.ThingIdentifier}_{data.UtcDateTime:yyyyMMddHHmmssfff}",
                    IsDuplicate = false
                };

                _logger.LogInformation("Single data point written to InfluxDB 2.x successfully for thing: {ThingId}",
                    data.ThingIdentifier);

                return result;
            }
            catch (InfluxException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // 幂等写入冲突，标记为重复数据
                _logger.LogWarning(ex, "Duplicate data point for thing: {ThingId}, time: {Time}",
                    data.ThingIdentifier, data.UtcDateTime);

                return new TimeSeriesWriteResult
                {
                    Success = true,
                    ErrorMessage = "数据已存在（幂等去重）",
                    WrittenTimeUtc = DateTime.UtcNow,
                    DataPointId = $"{data.ThingIdentifier}_{data.UtcDateTime:yyyyMMddHHmmssfff}",
                    IsDuplicate = true
                };
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Write argument null: {ParamName}", ex.ParamName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write single data point to InfluxDB 2.x for thing: {ThingId}",
                    data.ThingIdentifier);
                throw new TimeSeriesDataWriteException("InfluxDB 2.x单条写入失败", data.ThingIdentifier, ex);
            }
        }

        /// <summary>
        /// 批量写入时序数据
        /// 设计思路：转换数据列表为Point列表，使用批量写入API优化性能
        /// 设计考量：
        /// 1. 批量写入是IoT高并发场景的核心优化点
        /// 2. 统计成功/失败条数，便于监控和重试
        /// 3. 捕获异常时记录错误信息，不中断整个批量操作
        /// </summary>
        /// <param name="dataList">时序数据列表</param>
        /// <param name="options">写入选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量写入结果</returns>
        /// <exception cref="ArgumentNullException">数据列表为空时抛出</exception>
        public async Task<TimeSeriesBatchWriteResult> BatchWriteAsync(
            IEnumerable<TimeSeriesData> dataList,
            TimeSeriesWriteOptions options = default,
            CancellationToken cancellationToken = default)
        {
            if (dataList == null)
            {
                throw new ArgumentNullException(nameof(dataList), "批量写入数据列表不能为空");
            }

            var dataArray = dataList.ToArray();
            if (dataArray.Length == 0)
            {
                _logger.LogWarning("Batch write called with empty data list");
                return new TimeSeriesBatchWriteResult
                {
                    TotalCount = 0,
                    SuccessCount = 0,
                    BatchWrittenTimeUtc = DateTime.UtcNow
                };
            }

            options ??= new TimeSeriesWriteOptions();

            var result = new TimeSeriesBatchWriteResult
            {
                TotalCount = dataArray.Length,
                BatchWrittenTimeUtc = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting batch write to InfluxDB 2.x, total records: {Count}", dataArray.Length);

                // 转换为Point列表
                var points = new List<PointData>();
                foreach (var data in dataArray)
                {
                    try
                    {
                        points.Add(MapTimeSeriesDataToPoint(data, options));
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessages.Add($"转换数据失败 - ThingId: {data.ThingIdentifier}, Error: {ex.Message}");
                        _logger.LogError(ex, "Failed to map data point for thing: {ThingId}", data.ThingIdentifier);
                    }
                }

                // 批量写入
                if (points.Count > 0)
                {
                    await _writeApi.WritePointsAsync(points, _options.Bucket, _options.Org, cancellationToken);
                    result.SuccessCount = points.Count;
                }

                _logger.LogInformation("Batch write to InfluxDB 2.x completed - Total: {Total}, Success: {Success}, Failed: {Failed}",
                    result.TotalCount, result.SuccessCount, result.FailedCount);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"批量写入失败: {ex.Message}");
                _logger.LogError(ex, "Batch write to InfluxDB 2.x failed - Total: {Total}", dataArray.Length);
                return result;
            }
        }

        /// <summary>
        /// 按条件删除数据
        /// 设计思路：使用InfluxDB DeleteApi删除指定条件的数据
        /// 设计考量：
        /// 1. InfluxDB删除不返回影响条数，标记为-1
        /// 2. 限制删除时间范围，降低误删风险
        /// 3. 时序数据建议归档而非删除，方法内增加警告日志
        /// </summary>
        /// <param name="criteria">删除条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        /// <exception cref="ArgumentNullException">条件为空时抛出</exception>
        /// <exception cref="TimeSeriesDataException">删除失败时抛出</exception>
        public async Task<TimeSeriesDeleteResult> DeleteAsync(
            TimeSeriesDeleteCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            if (criteria == null)
            {
                throw new ArgumentNullException(nameof(criteria), "删除条件不能为空");
            }

            try
            {
                // 验证删除条件
                criteria.Validate();

                _logger.LogWarning("Deleting data from InfluxDB 2.x for thing: {ThingId}, time range: {StartTime} - {EndTime} (WARNING: Time series data should be archived instead of deleted)",
                    criteria.ThingIdentifier,
                    criteria.TimeRange.StartTimeUtc,
                    criteria.TimeRange.EndTimeUtc);

                var client = _clientFactory.GetClient();
                var deleteApi = client.GetDeleteApi();

                // 构建删除条件
                var start = criteria.TimeRange.StartTimeUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var stop = criteria.TimeRange.EndTimeUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var predicate = $"thing_identifier = \"{criteria.ThingIdentifier}\"";

                // 执行删除
                await deleteApi.DeleteAsync(start, stop, predicate, _options.Bucket, _options.Org, cancellationToken);

                _
```

# （续）仓储实现与模块配置

## 2.3 仓储实现（Repositories/InfluxDb2TimeSeriesRepository.cs）

### 剩余方法实现

csharp



运行









```
                var deleteResult = new TimeSeriesDeleteResult
                {
                    Success = true,
                    DeletedCount = -1, // InfluxDB不返回删除条数
                    ErrorMessage = null
                };

                _logger.LogInformation("Delete operation completed for thing: {ThingId}", criteria.ThingIdentifier);
                return deleteResult;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Delete argument null: {ParamName}", ex.ParamName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete operation failed for thing: {ThingId}", criteria.ThingIdentifier);
                throw new TimeSeriesDataException("InfluxDB 2.x数据删除失败", criteria.ThingIdentifier, ex);
            }
        }
        #endregion

        #region ITimeSeriesTransactional 实现
        /// <summary>
        /// 开启事务（InfluxDB 2.x社区版不支持）
        /// 设计思路：明确抛出不支持异常，避免上层误用
        /// 设计考量：社区版无事务能力，企业版可扩展实现
        /// </summary>
        /// <param name="isolationLevel">隔离级别</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>无返回，直接抛异常</returns>
        /// <exception cref="NotSupportedException">始终抛出此异常</exception>
        public Task<ITimeSeriesTransaction> BeginTransactionAsync(
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            var message = "InfluxDB 2.x Community Edition does not support transactions. Use InfluxDB Enterprise or other time-series databases that support transactions.";
            _logger.LogError(message);
            throw new NotSupportedException(message);
        }
        #endregion

        #region ITimeSeriesIndexManager 实现
        /// <summary>
        /// 创建索引（InfluxDB Tag自动索引，无需手动创建）
        /// 设计思路：空实现，记录日志即可
        /// 设计考量：InfluxDB的Tag天然是索引字段，无需额外创建
        /// </summary>
        public Task CreateIndexAsync(
            IEnumerable<ITimeSeriesIndexStrategy> strategies,
            string measurement,
            CancellationToken cancellationToken = default)
        {
            if (strategies == null)
            {
                throw new ArgumentNullException(nameof(strategies), "索引策略列表不能为空");
            }

            if (string.IsNullOrWhiteSpace(measurement))
            {
                measurement = TimeSeriesConsts.DefaultMeasurementName;
            }

            foreach (var strategy in strategies)
            {
                _logger.LogInformation("InfluxDB 2.x automatically indexes tags, manual index creation is not required. Index strategy: {IndexName}, Measurement: {Measurement}",
                    strategy.IndexName, measurement);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除索引（InfluxDB不支持手动删除Tag索引）
        /// 设计思路：空实现，记录警告日志
        /// 设计考量：Tag索引与Tag字段绑定，删除Tag字段才会删除索引
        /// </summary>
        public Task DropIndexAsync(
            string indexName,
            string measurement,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(indexName))
            {
                throw new ArgumentNullException(nameof(indexName), "索引名称不能为空");
            }

            if (string.IsNullOrWhiteSpace(measurement))
            {
                measurement = TimeSeriesConsts.DefaultMeasurementName;
            }

            _logger.LogWarning("InfluxDB 2.x does not support manual index deletion. Index: {IndexName}, Measurement: {Measurement}",
                indexName, measurement);

            return Task.CompletedTask;
        }
        #endregion

        #region ITimeSeriesStorageContract 实现
        /// <summary>
        /// 健康检查（复用工厂的健康检查方法）
        /// 设计思路：代理模式，委托工厂实现健康检查
        /// 设计考量：工厂统一管理客户端，健康检查逻辑应内聚在工厂
        /// </summary>
        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing health check via InfluxDb2ClientFactory");
            return await _clientFactory.CheckHealthAsync(cancellationToken);
        }

        /// <summary>
        /// 释放资源
        /// 设计思路：仅释放仓储自身的API资源，客户端由工厂统一管理
        /// 设计考量：客户端是单例，仓储作为瞬态对象不能释放单例资源
        /// </summary>
        public void Dispose()
        {
            _writeApi?.Dispose();
            _queryApi = null;
            GC.SuppressFinalize(this);
            _logger.LogDebug("InfluxDb2TimeSeriesRepository disposed");
        }
        #endregion

        #region 私有工具方法
        /// <summary>
        /// 构建Flux查询语句
        /// 设计思路：将通用查询条件动态拼接为Flux语法
        /// 设计考量：
        /// 1. 时间范围使用绝对时间筛选，避免时区问题
        /// 2. Tag筛选使用==运算符，支持多条件组合
        /// 3. 字段筛选使用keep()函数，提升查询性能
        /// 4. 限制返回条数，防止内存溢出
        /// </summary>
        private string BuildFluxQuery(TimeSeriesQueryCriteria criteria)
        {
            var measurement = string.IsNullOrWhiteSpace(criteria.Measurement) 
                ? TimeSeriesConsts.DefaultMeasurementName 
                : criteria.Measurement;

            // 基础查询模板
            var fluxTemplate = @"
from(bucket: ""{Bucket}"")
  |> range(start: {StartTime}, stop: {StopTime})
  |> filter(fn: (r) => r._measurement == ""{Measurement}"")
  |> filter(fn: (r) => r.thing_identifier == ""{ThingId}"")
  {TagFilters}
  {FieldFilters}
  {Sort}
  |> limit(n: {Limit})";

            // 替换基础参数
            var fluxQuery = fluxTemplate
                .Replace("{Bucket}", _options.Bucket)
                .Replace("{StartTime}", FormatDateTime(criteria.TimeRange.StartTimeUtc))
                .Replace("{StopTime}", FormatDateTime(criteria.TimeRange.EndTimeUtc))
                .Replace("{Measurement}", measurement)
                .Replace("{ThingId}", criteria.ThingIdentifier)
                .Replace("{Limit}", criteria.Limit.ToString());

            // 构建Tag筛选条件
            var tagFilters = new List<string>();
            foreach (var (key, value) in criteria.TagFilters)
            {
                tagFilters.Add($"|> filter(fn: (r) => r.{key} == \"{value}\")");
            }
            fluxQuery = fluxQuery.Replace("{TagFilters}", string.Join("\n  ", tagFilters));

            // 构建字段筛选条件
            string fieldFilters = string.Empty;
            if (criteria.FieldNames.Any())
            {
                var fields = string.Join(", ", criteria.FieldNames.Select(f => $"\"{f}\""));
                fieldFilters = $"|> keep(columns: [""_time"", ""_value"", ""_field"", ""thing_identifier""{fields}])";
            }
            fluxQuery = fluxQuery.Replace("{FieldFilters}", fieldFilters);

            // 构建排序条件
            string sort = criteria.OrderByDescending 
                ? "|> sort(columns: [""_time""], desc: true)" 
                : "|> sort(columns: [""_time""], desc: false)";
            fluxQuery = fluxQuery.Replace("{Sort}", sort);

            return fluxQuery.Trim();
        }

        /// <summary>
        /// 构建聚合Flux查询语句
        /// 设计思路：根据聚合类型选择对应的Flux聚合函数
        /// 设计考量：
        /// 1. 使用window()函数按时间窗口分组
        /// 2. 聚合函数与枚举值一一对应
        /// 3. 填充空窗口，保证时间序列连续性
        /// </summary>
        private string BuildAggregateFluxQuery(TimeSeriesAggregateCriteria criteria)
        {
            var measurement = string.IsNullOrWhiteSpace(criteria.Measurement) 
                ? TimeSeriesConsts.DefaultMeasurementName 
                : criteria.Measurement;

            // 聚合函数映射
            var aggregateFunctionMap = new Dictionary<TimeSeriesAggregateType, string>
            {
                { TimeSeriesAggregateType.Sum, "sum()" },
                { TimeSeriesAggregateType.Avg, "mean()" },
                { TimeSeriesAggregateType.Max, "max()" },
                { TimeSeriesAggregateType.Min, "min()" },
                { TimeSeriesAggregateType.Count, "count()" },
                { TimeSeriesAggregateType.First, "first()" },
                { TimeSeriesAggregateType.Last, "last()" }
            };

            if (!aggregateFunctionMap.TryGetValue(criteria.AggregateType, out var aggFunc))
            {
                throw new NotSupportedException($"Aggregate type {criteria.AggregateType} is not supported by InfluxDB 2.x");
            }

            // 聚合查询模板
            var fluxTemplate = @"
from(bucket: ""{Bucket}"")
  |> range(start: {StartTime}, stop: {StopTime})
  |> filter(fn: (r) => r._measurement == ""{Measurement}"")
  |> filter(fn: (r) => r.thing_identifier == ""{ThingId}"")
  |> filter(fn: (r) => r._field == ""{FieldName}"")
  |> window(every: {TimeWindow})
  |> {AggFunc}(column: ""_value"")
  |> yield(name: ""{AggName}"")";

            return fluxTemplate
                .Replace("{Bucket}", _options.Bucket)
                .Replace("{StartTime}", FormatDateTime(criteria.TimeRange.StartTimeUtc))
                .Replace("{StopTime}", FormatDateTime(criteria.TimeRange.EndTimeUtc))
                .Replace("{Measurement}", measurement)
                .Replace("{ThingId}", criteria.ThingIdentifier)
                .Replace("{FieldName}", criteria.FieldName)
                .Replace("{TimeWindow}", criteria.TimeWindow)
                .Replace("{AggFunc}", aggFunc)
                .Replace("{AggName}", criteria.AggregateType.ToString().ToLower())
                .Trim();
        }

        /// <summary>
        /// 格式化时间为Flux兼容格式
        /// 设计思路：转换为UTC时间的ISO 8601格式，带时区标识
        /// 设计考量：InfluxDB默认使用UTC时间，避免时区转换错误
        /// </summary>
        private string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        /// <summary>
        /// 通用模型转换为InfluxDB Point
        /// 设计思路：映射Tag/Field/Time到InfluxDB的Point结构
        /// 设计考量：
        /// 1. thing_identifier作为核心Tag，必须存在
        /// 2. Time字段使用UTC时间，精度到纳秒
        /// 3. Field仅支持数值类型，非数值类型会被过滤
        /// 4. 扩展Tag直接添加，支持多维度筛选
        /// </summary>
        private PointData MapTimeSeriesDataToPoint(TimeSeriesData data, TimeSeriesWriteOptions options)
        {
            var measurement = string.IsNullOrWhiteSpace(data.Measurement) 
                ? TimeSeriesConsts.DefaultMeasurementName 
                : data.Measurement;

            // 构建Point对象
            var pointBuilder = PointData.Measurement(measurement)
                .Tag("thing_identifier", data.ThingIdentifier)
                .Timestamp(data.UtcDateTime, WritePrecision.Ns);

            // 添加扩展Tag
            foreach (var (key, value) in data.Tags)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    pointBuilder = pointBuilder.Tag(key, value);
                }
            }

            // 添加Field（仅支持数值类型）
            foreach (var (key, value) in data.Fields)
            {
                if (string.IsNullOrWhiteSpace(key) || value == null)
                {
                    continue;
                }

                switch (value)
                {
                    case int intValue:
                        pointBuilder = pointBuilder.Field(key, intValue);
                        break;
                    case long longValue:
                        pointBuilder = pointBuilder.Field(key, longValue);
                        break;
                    case float floatValue:
                        pointBuilder = pointBuilder.Field(key, floatValue);
                        break;
                    case double doubleValue:
                        pointBuilder = pointBuilder.Field(key, doubleValue);
                        break;
                    case decimal decimalValue:
                        pointBuilder = pointBuilder.Field(key, (double)decimalValue);
                        break;
                    default:
                        _logger.LogWarning("Unsupported field type {Type} for field {FieldName}, value will be ignored",
                            value.GetType().Name, key);
                        break;
                }
            }

            return pointBuilder;
        }

        /// <summary>
        /// InfluxDB Record转换为通用模型
        /// 设计思路：反向映射，从查询结果构建通用时序数据对象
        /// 设计考量：
        /// 1. 提取核心字段（thing_identifier、_time、_field、_value）
        /// 2. 动态提取扩展Tag，封装到Tags字典
        /// 3. 处理空值，保证模型完整性
        /// </summary>
        private TimeSeriesData MapRecordToTimeSeriesData(FluxRecord record)
        {
            var data = new TimeSeriesData
            {
                ThingIdentifier = record.GetValueByName<string>("thing_identifier") ?? string.Empty,
                UtcDateTime = record.GetTime().GetValueOrDefault().ToUniversalTime(),
                Measurement = record.GetValueByName<string>("_measurement") ?? TimeSeriesConsts.DefaultMeasurementName,
                InsertedUtcTime = DateTime.UtcNow
            };

            // 提取Field
            var fieldName = record.GetValueByName<string>("_field");
            var fieldValue = record.GetValueByName("_value");
            if (!string.IsNullOrWhiteSpace(fieldName) && fieldValue != null)
            {
                switch (fieldValue)
                {
                    case int intValue:
                        data.SetField(fieldName, intValue);
                        break;
                    case long longValue:
                        data.SetField(fieldName, longValue);
                        break;
                    case float floatValue:
                        data.SetField(fieldName, floatValue);
                        break;
                    case double doubleValue:
                        data.SetField(fieldName, doubleValue);
                        break;
                    case decimal decimalValue:
                        data.SetField(fieldName, decimalValue);
                        break;
                }
            }

            // 提取扩展Tag（排除内置字段）
            var excludedKeys = new HashSet<string> { "thing_identifier", "_measurement", "_field", "_value", "_time" };
            foreach (var key in record.Values.Keys)
            {
                if (!excludedKeys.Contains(key) && record.TryGetValue(key, out var value) && value is string strValue)
                {InfluxDbTimeSeriesRepository
                    data.SetTag(key, strValue);
                }
            }

            return data;
        }
        #endregion
    }
}
```

## 2.4 ABP 模块配置（InfluxDB2Module.cs）

```
using System;
using Artizan.IoT.TimeSeries.InfluxDB2.Factories;
using Artizan.IoT.TimeSeries.InfluxDB2.Options;
using Artizan.IoT.TimeSeries.InfluxDB2.Repositories;
using InfluxDB.Client;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;

namespace Artizan.IoT.TimeSeries.InfluxDB2
{
    /// <summary>
    /// InfluxDB 2.x 时序数据模块
    /// 设计思路：遵循ABP模块开发规范，实现自动注册和配置
    /// 设计模式：模块模式（Module Pattern），基于ABP框架的模块化设计
    /// 设计考量：
    /// 1. 自动注册配置选项和验证器
    /// 2. 注册单例工厂和瞬态仓储
    /// 3. 模块依赖自动注入，无需手动配置
    /// 4. 支持配置前置和后置处理，增强扩展性
    /// </summary>
    [DependsOn(
        typeof(AbpValidationModule),
        typeof(ArtizanIoTTimeSeriesAbstractionsModule) // 依赖抽象层模块
    )]
    public class ArtizanIoTTimeSeriesInfluxDB2Module : AbpModule
    {
        /// <summary>
        /// 配置服务（注册核心组件）
        /// 设计思路：在服务配置阶段注册所有依赖项
        /// 设计考量：
        /// 1. 配置选项注册为单例，支持从配置文件读取
        /// 2. 配置验证器自动注册，启动时验证配置
        /// 3. 工厂注册为单例，保证客户端全局复用
        /// 4. 仓储注册为瞬态，支持依赖注入
        /// </summary>
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            // 1. 注册配置选项
            services.AddConfigureOptions<InfluxDb2OptionsValidator>();
            services.Configure<InfluxDb2Options>(context.Configuration.GetSection("Artizan:IoT:TimeSeries:InfluxDB2"));

            // 2. 注册客户端工厂（单例）
            services.AddSingleton<IInfluxDbClientFactory, InfluxDb2ClientFactory>();

            // 3. 注册仓储（瞬态，支持替换）
            services.AddTransient<ITimeSeriesDataRepository, InfluxDb2TimeSeriesRepository>();

            // 4. 配置健康检查（可选）
            Configure<AbpHealthChecksBuilderOptions>(options =>
            {
                options.AddHealthCheck("influxdb2", healthCheckBuilder =>
                {
                    healthCheckBuilder.AddInfluxDb(
                        context.Configuration["Artizan:IoT:TimeSeries:InfluxDB2:Url"],
                        context.Configuration["Artizan:IoT:TimeSeries:InfluxDB2:Token"]);
                });
            });

            base.ConfigureServices(context);
        }

        /// <summary>
        /// 模块初始化（应用启动时执行）
        /// 设计思路：初始化客户端连接，验证配置有效性
        /// 设计考量：
        /// 1. 提前创建客户端，发现配置错误
        /// 2. 执行健康检查，确保服务可用
        /// 3. 记录模块启动日志，便于排查问题
        /// </summary>
        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<ArtizanIoTTimeSeriesInfluxDB2Module>>();
            var clientFactory = context.ServiceProvider.GetRequiredService<IInfluxDbClientFactory>();

            try
            {
                // 初始化客户端并执行健康检查
                var client = clientFactory.GetClient();
                var isHealthy = await clientFactory.CheckHealthAsync();

                if (isHealthy)
                {
                    logger.LogInformation("ArtizanIoTTimeSeriesInfluxDB2Module initialized successfully. InfluxDB 2.x connection is healthy.");
                }
                else
                {
                    logger.LogWarning("ArtizanIoTTimeSeriesInfluxDB2Module initialized, but InfluxDB 2.x health check failed.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize ArtizanIoTTimeSeriesInfluxDB2Module");
                throw; // 启动失败时终止应用
            }

            await base.OnApplicationInitializationAsync(context);
        }

        /// <summary>
        /// 模块销毁（应用停止时执行）
        /// 设计思路：释放工厂资源，优雅关闭客户端
        /// 设计考量：
        /// 1. 释放单例工厂，关闭HTTP连接池
        /// 2. 记录模块销毁日志，便于追踪
        /// </summary>
        public override async Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<ArtizanIoTTimeSeriesInfluxDB2Module>>();
            var clientFactory = context.ServiceProvider.GetRequiredService<IInfluxDbClientFactory>();

            try
            {
                clientFactory.Dispose();
                logger.LogInformation("ArtizanIoTTimeSeriesInfluxDB2Module shutdown successfully. InfluxDB 2.x client disposed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while shutting down ArtizanIoTTimeSeriesInfluxDB2Module");
            }

            await base.OnApplicationShutdownAsync(context);
        }
    }
}
```

## 3. Artizan.IoT.TimeSeries.InfluxDB3 项目说明

InfluxDB3 的实现与 InfluxDB2**结构完全一致**，仅需修改以下差异化部分：

1. **配置选项**：`InfluxDb3Options` 移除 `Org`，新增 `Database`（InfluxDB3 用 Database 替代 Bucket）、`FlightEnabled`（支持 Flight SQL 协议）

2. **客户端工厂**：`InfluxDb3ClientFactory` 调整 `InfluxDBClientOptions` 的配置参数，适配 InfluxDB3 的连接协议

3. 查询语法

   ：InfluxDB3 支持两种查询方式

   - Flux（兼容 V2）
   - SQL（新增，推荐用于复杂查询）

   

4. **模块类**：`ArtizanIoTTimeSeriesInfluxDB3Module` 继承相同的模块依赖，注册对应的选项和工厂

# 二、设计总结与补充建议

## 1. 核心讨论点与设计变更梳理

| **讨论阶段** |            **核心问题**            |              **设计方案**              |                  **设计变更**                   |
| :----------: | :--------------------------------: | :------------------------------------: | :---------------------------------------------: |
|   初始设计   | 如何适配 InfluxDB2 的时序数据存储  |  基于通用仓储接口实现 InfluxDB2 仓储   |                       无                        |
|   性能优化   | 频繁创建`InfluxDBClient`的性能问题 |  引入**工厂模式**，实现客户端单例复用  |     从直接创建客户端 → 工厂懒加载单例客户端     |
|   版本适配   |   如何同时支持 InfluxDB2/3 版本    | 采用**模块化设计**，分拆为两个独立项目 |       从单一项目 → 抽象层 + 多版本实现层        |
|  扩展性优化  |        如何提升代码可维护性        |   遵循**接口隔离原则**，拆分读写接口   | 从单一仓储接口 → 读写分离 + 事务 + 索引管理接口 |
|   异常处理   |      如何精准处理时序操作异常      |    设计**自定义异常体系**，分层捕获    |  从通用 Exception → 细分查询 / 写入 / 删除异常  |

## 2. 关键设计理念与模式

|   **设计模式**   |           **应用场景**           |            **核心价值**            |
| :--------------: | :------------------------------: | :--------------------------------: |
|   **工厂模式**   |         客户端创建与管理         |     保证单例复用，降低资源消耗     |
|  **适配器模式**  |       仓储实现适配通用接口       |  屏蔽底层差异，支持多存储引擎替换  |
| **接口隔离原则** | 拆分仓储接口为读写 / 事务 / 索引 |        按需依赖，降低耦合度        |
|   **选项模式**   |             配置管理             | 标准化配置读取，支持验证和动态更新 |
|  **模块化设计**  |            多版本适配            |      版本隔离，独立部署和升级      |

## 3. 补充优化建议

### （1）批量写入性能优化

- **内存缓冲区**：在仓储层增加本地内存缓冲区，积累到阈值后批量提交，降低网络 IO
- **异步重试**：基于 Polly 实现重试策略，处理网络波动导致的写入失败
- **分区写入**：按`thing_identifier`分片写入，提升高并发场景下的吞吐量

### （2）监控与可观测性

- **指标采集**：记录写入延迟、查询 QPS、错误率等指标，接入 Prometheus
- **链路追踪**：集成 OpenTelemetry，追踪从数据写入到查询的完整链路
- **日志增强**：使用结构化日志，记录`thing_identifier`、`measurement`等核心字段

### （3）高可用设计

- **客户端负载均衡**：支持配置多个 InfluxDB 节点，实现故障转移
- **写入降级**：当 InfluxDB 不可用时，降级写入本地文件，恢复后异步同步
- **数据备份**：定期导出数据到对象存储（如 S3），防止数据丢失

### （4）代码生成工具

- 基于 T4 模板生成 InfluxDB3 的仓储代码，减少重复开发
- 自动生成物模型与时序数据模型的映射代码

## 4. 配置文件示例（appsettings.json）

```
{
  "Artizan": {
    "IoT": {
      "TimeSeries": {
        "InfluxDB2": {
          "Url": "http://localhost:8086",
          "Token": "your-influxdb-token",
          "Org": "artizan-iot",
          "Bucket": "iot_telemetry",
          "TimeoutSeconds": 30,
          "BatchSize": 1000,
          "FlushIntervalMs": 1000,
          "MaxRetries": 3
        }
      }
    }
  }
}
```

# 三、最终交付物清单

1. **Artizan.IoT.TimeSeries.Abstractions**：通用抽象层（模型 + 接口 + 异常）
2. **Artizan.IoT.TimeSeries.InfluxDB2**：InfluxDB2 实现层（工厂 + 仓储 + 模块）
3. **Artizan.IoT.TimeSeries.InfluxDB3**：InfluxDB3 实现层（同结构，适配 V3 特性）
4. **配置示例**：appsettings.json 配置模板
5. **设计文档**：架构设计与核心实现说明

是否需要我为你生成**Artizan.IoT.TimeSeries.InfluxDB3**的核心差异代码？