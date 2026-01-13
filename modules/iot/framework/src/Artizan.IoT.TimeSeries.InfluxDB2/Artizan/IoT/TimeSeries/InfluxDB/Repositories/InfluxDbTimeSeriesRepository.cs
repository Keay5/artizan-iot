using Artizan.IoT.TimeSeries.Contracts;
using Artizan.IoT.TimeSeries.Enums;
using Artizan.IoT.TimeSeries.Exceptions;
using Artizan.IoT.TimeSeries.InfluxDB.Options;
using Artizan.IoT.TimeSeries.Models;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.TimeSeries.InfluxDB.Repositories;

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
public class InfluxDbTimeSeriesRepository : ITimeSeriesDataRepository, ITransientDependency
{
    private readonly InfluxDbOptions _options;
    private readonly IInfluxDbClientFactory _clientFactory;
    private readonly ILogger<InfluxDbTimeSeriesRepository> _logger;
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
    public InfluxDbTimeSeriesRepository(
        IOptions<InfluxDbOptions> options,
        IInfluxDbClientFactory clientFactory,
        ILogger<InfluxDbTimeSeriesRepository> logger)
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
            //_writeApi = client.GetWriteApiAsync(new WriteOptions
            //{
            //    BatchSize = _options.BatchSize,
            //    FlushInterval = _options.FlushIntervalMs,
            //    RetryInterval = _options.RetryIntervalMs,
            //    JitterInterval = 1000,
            //    MaxRetries = _options.MaxRetries,
            //    MaxRetryDelay = (int)TimeSpan.FromSeconds(30).TotalMilliseconds,
            //    ExponentialBase = 2
            //});

            var _writeApi = client.GetWriteApiAsync(); // 异步WriteApi

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

            _logger.LogInformation("InfluxDB 2.x query completed, returned {Count} records for thing: {ThingIdentifier}",
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
                    Instant? instantTime = record.GetTime();
                    DateTime time;
                    if (instantTime.HasValue)
                    {
                        // Instant → UTC DateTime（正确转换方式）
                        time = instantTime.Value.InUtc().ToDateTimeUtc();
                    }
                    else
                    {
                        // 空值兜底：使用当前UTC时间，避免异常
                        time = DateTime.UtcNow;
                        _logger?.LogWarning("FluxRecord has no _time field, using current UTC time: {Time}", time);
                    }

                    var value = Convert.ToDouble(record.GetValue(), CultureInfo.InvariantCulture);
                    result[time] = value;
                }
            }

            _logger.LogDebug("InfluxDB 2.x aggregate query completed, returned {Count} aggregated records for thing: {ThingId}",
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

            _logger.LogDebug("Single data point written to InfluxDB 2.x successfully for thing: {ThingIdentifier}",
                data.ThingIdentifier);

            return result;
        }
        catch (InfluxException ex) when (ex.Code == (int)HttpStatusCode.Conflict)
        {
            // 幂等写入冲突，标记为重复数据
            _logger.LogWarning(ex, "Duplicate data point for thing: {ThingIdentifier}, time: {Time}",
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
            _logger.LogDebug("Starting batch write to InfluxDB 2.x, total records: {Count}", dataArray.Length);

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

            _logger.LogDebug("Batch write to InfluxDB 2.x completed - Total: {Total}, Success: {Success}, Failed: {Failed}",
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
            var start = criteria.TimeRange.StartTimeUtc.ToUniversalTime(); // .ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            var stop = criteria.TimeRange.EndTimeUtc.ToUniversalTime(); // .ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            var predicate = $"{TimeSeriesFieldConsts.ThingIdentifier} = \"{criteria.ThingIdentifier}\"";

            // 执行删除
            await deleteApi.Delete(start: start, stop: stop, predicate: predicate, bucket: _options.Bucket, org: _options.Org, cancellationToken: cancellationToken);

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
        //_writeApi?.Dispose();
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
        var fluxTemplate = @$"
            from(bucket: ""@Bucket"")
              |> range(start: @StartTime, stop: @StopTime)
              |> filter(fn: (r) => r._measurement == ""@Measurement"")
              |> filter(fn: (r) => r.{TimeSeriesFieldConsts.ThingIdentifier} == ""@ThingIdentifier"")
              @TagFilters
              @FieldFilters
              @Sort
              |> limit(n: @Limit)";

        // 替换基础参数
        var fluxQuery = fluxTemplate
            .Replace("@Bucket", _options.Bucket)
            .Replace("@StartTime", FormatDateTime(criteria.TimeRange.StartTimeUtc))
            .Replace("@StopTime", FormatDateTime(criteria.TimeRange.EndTimeUtc))
            .Replace("@Measurement", measurement)
            .Replace("@ThingIdentifier", criteria.ThingIdentifier)
            .Replace("@Limit", criteria.Limit.ToString());

        // 构建Tag筛选条件
        var tagFilters = new List<string>();
        foreach (var (key, value) in criteria.TagFilters)
        {
            tagFilters.Add($"|> filter(fn: (r) => r.{key} == \"{value}\")");
        }
        fluxQuery = fluxQuery.Replace("@TagFilters", string.Join("\n  ", tagFilters));

        // 构建字段筛选条件（修复核心：补充分隔符）
        string fieldFilters = string.Empty;
        if (criteria.FieldNames != null && criteria.FieldNames.Any())
        {
            // 步骤1：拼接自定义字段（带引号）
            var customFields = string.Join(", ", criteria.FieldNames.Select(f => $"\"{f}\""));
            // 步骤2：拼接固定字段 + 自定义字段（添加逗号分隔）
            var allFields = $"\"_time\", \"_value\", \"_field\", \"{TimeSeriesFieldConsts.ThingIdentifier}\", {customFields}";
            // 步骤3：生成合法的keep语句
            fieldFilters = $"|> keep(columns: [{allFields}])";
        }
        fluxQuery = fluxQuery.Replace("@FieldFilters", fieldFilters);

        // 构建排序条件
        string sort = criteria.OrderByDescending
            ? "|> sort(columns: [\"_time\"], desc: true)"
            : "|> sort(columns: [\"_time\"], desc: false)";
        fluxQuery = fluxQuery.Replace("@Sort", sort);

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
        var fluxTemplate = @$"
            from(bucket: ""@Bucket"")
              |> range(start: @StartTime, stop: @StopTime)
              |> filter(fn: (r) => r._measurement == ""@Measurement"")
              |> filter(fn: (r) => r.{TimeSeriesFieldConsts.ThingIdentifier} == ""@ThingIdentifier"")
              |> filter(fn: (r) => r._field == ""@FieldName"")
              |> window(every: @TimeWindow)
              |> @AggFunc(column: ""_value"")
              |> yield(name: ""@AggName"")";

        return fluxTemplate
            .Replace("@Bucket", _options.Bucket)
            .Replace("@StartTime", FormatDateTime(criteria.TimeRange.StartTimeUtc))
            .Replace("@StopTime", FormatDateTime(criteria.TimeRange.EndTimeUtc))
            .Replace("@Measurement", measurement)
            .Replace("@ThingIdentifier", criteria.ThingIdentifier)
            .Replace("@FieldName", criteria.FieldName)
            .Replace("@TimeWindow", criteria.TimeWindow)
            .Replace("@AggFunc", aggFunc)
            .Replace("@AggName", criteria.AggregateType.ToString().ToLower())
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
    /// 1. <see cref="TimeSeriesFieldConsts.ThingIdentifier"/>作为核心Tag，必须存在
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
            .Tag(TimeSeriesFieldConsts.ThingIdentifier, data.ThingIdentifier)
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

            // TODO: 考虑是否需要更严格的类型检查
            pointBuilder = pointBuilder.Field(key, value);

            //switch (value)
            //{
            //    case int intValue:
            //        pointBuilder = pointBuilder.Field(key, intValue);
            //        break;
            //    case long longValue:
            //        pointBuilder = pointBuilder.Field(key, longValue);
            //        break;
            //    case float floatValue:
            //        pointBuilder = pointBuilder.Field(key, floatValue);
            //        break;
            //    case double doubleValue:
            //        pointBuilder = pointBuilder.Field(key, doubleValue);
            //        break;
            //    case decimal decimalValue:
            //        pointBuilder = pointBuilder.Field(key, (double)decimalValue);
            //        break;
            //    default:
            //        _logger.LogWarning("Unsupported field type {Type} for field {FieldName}, value will be ignored",
            //            value.GetType().Name, key);
            //        break;
            //}
        }

        return pointBuilder;
    }

    /// <summary>
    /// InfluxDB Record转换为通用模型
    /// 设计思路：反向映射，从查询结果构建通用时序数据对象
    /// 设计考量：
    /// 1. 提取核心字段（thing_identifier、_time、_field、_value）
    /// 2. 动态提取扩展Tag，封装到Tags字典
    /// 3. 空值安全处理：所有取值都判空，避免NullReferenceException
    /// 4. 类型转换：针对不同字段类型做安全转换，兼容数值/字符串
    /// 5. Tag/Field分离：严格遵循InfluxDB Tag存字符串、Field存数值的规范
    /// </summary>
    /// <param name="record">InfluxDB查询返回的FluxRecord</param>
    /// <returns>通用时序数据模型</returns>
    /// <exception cref="ArgumentNullException">record为null时抛出</exception>
    private TimeSeriesData MapRecordToTimeSeriesData(FluxRecord record)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record), "FluxRecord cannot be null");
        }

        // 1. 初始化核心字段（基于GetValueByKey/原生封装方法）
        var data = new TimeSeriesData
        {
            // 获取设备标识（核心Tag）
            ThingIdentifier = record.GetValueByKey(TimeSeriesFieldConsts.ThingIdentifier) as string ?? string.Empty,

            // 获取时间（转换为UTC DateTime）
            UtcDateTime = record.GetTime()?.InUtc().ToDateTimeUtc() ?? DateTime.UtcNow,

            // 获取测量值名称（使用封装方法，更简洁）
            Measurement = record.GetMeasurement() ?? TimeSeriesConsts.DefaultMeasurementName,

            // 本地插入时间
            InsertedUtcTime = DateTime.UtcNow
        };

        // 2. 提取Field（仅数值类型，可计算）
        try
        {
            // 获取字段名和字段值（使用封装方法）
            string fieldName = record.GetField();
            object fieldValue = record.GetValue();

            // TODO: 考虑是否需要更严格的类型检查
            if (!string.IsNullOrWhiteSpace(fieldName) && fieldValue != null)
            {
                data.SetField(fieldName, fieldValue);
            }

            // TODO: 考虑是否限制类型
            //if (!string.IsNullOrWhiteSpace(fieldName) && fieldValue != null)
            //{
            //    // 根据值类型设置Field（兼容所有数值类型+布尔转数值）
            //    switch (fieldValue)
            //    {
            //        case int intValue:
            //            data.SetField(fieldName, intValue);
            //            break;
            //        case long longValue:
            //            data.SetField(fieldName, longValue);
            //            break;
            //        case float floatValue:
            //            data.SetField(fieldName, floatValue);
            //            break;
            //        case double doubleValue:
            //            data.SetField(fieldName, doubleValue);
            //            break;
            //        case decimal decimalValue:
            //            data.SetField(fieldName, decimalValue);
            //            break;
            //        case short shortValue:
            //            data.SetField(fieldName, shortValue);
            //            break;
            //        case bool boolValue:
            //            // 布尔类型自动转为1/0，兼容开关场景
            //            data.SetField(fieldName, boolValue ? 1 : 0);
            //            break;
            //        default:
            //            _logger.LogWarning(
            //                "Unsupported field value type '{Type}' for field '{FieldName}' (record table: {Table})",
            //                fieldValue.GetType().Name, fieldName, record.Table);
            //            break;
            //    }
            //}
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract Field from FluxRecord (table: {Table})", record.Table);
        }

        // 3. 提取扩展Tag（排除内置字段，仅存字符串）
        try
        {
            // 内置字段：不视为扩展Tag
            var excludedKeys = new HashSet<string>
        {
            TimeSeriesFieldConsts.ThingIdentifier, // 核心Tag，已单独处理
            "_start", "_stop", "_time", // 时间字段
            "_measurement", "_field", "_value" // 测量/字段值字段
        };

            // 遍历所有键值对，提取扩展Tag
            foreach (var key in record.Values.Keys)
            {
                if (excludedKeys.Contains(key))
                {
                    continue;
                }

                // 获取Tag值并转换为字符串（空值转为空字符串）
                var tagValue = record.GetValueByKey(key)?.ToString() ?? string.Empty;
                data.SetTag(key, tagValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract Tags from FluxRecord (table: {Table})", record.Table);
        }

        return data;
    }
    #endregion
}
