using Artizan.IoT.Messages.PostProcessors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Settings;

namespace Artizan.IoT.Mqtts.Messages.PostProcessors;

/// <summary>
/// 示例MQTT时序库入库插件（示例：具体策略实现）
/// </summary>
public class SmapleMqttTimeseriesProcessor : IMessagePostProcessor<MqttMessageContext>
{
    /* private readonly IBatchTimeseriesClient _batchClient;  // TODO: 这是后续设计实现了*/
    private readonly ISettingProvider _settingProvider;    //TODO: ？使用SettingManager才是最佳实践
    private readonly ConcurrentQueue<PropertyData> _batchQueue = new();
    private readonly SemaphoreSlim _batchLock = new(1, 1);

    public int Priority => 100;
    public bool IsEnabled => _settingProvider.GetAsync<bool>("Timeseries:Mqtt:Enabled").Result;

    public SmapleMqttTimeseriesProcessor(/* IBatchTimeseriesClient batchClient, */ ISettingProvider settingProvider)
    {
        //_batchClient = batchClient;   // TODO: 这是后续设计实现了
        _settingProvider = settingProvider;
    }

    /// <summary>
    /// 批量入库逻辑（累计100条或超时1秒触发）
    /// </summary>
    public async Task ProcessAsync(MqttMessageContext context, CancellationToken cancellationToken)
    {
        var propertyData = ExtractPropertyData(context);
        _batchQueue.Enqueue(propertyData);

        // 触发批量条件
        if (_batchQueue.Count >= 100 /*||  超时判断 */) // TODO: 这是后续设计实现了
        {
            await _batchLock.WaitAsync(cancellationToken);
            try
            {
                var batch = new List<PropertyData>();
                while (_batchQueue.TryDequeue(out var item) && batch.Count < 100)
                {
                    batch.Add(item);
                }

                if (batch.Count > 0)
                {
                    // await _batchClient.BatchWriteAsync(batch); // TODO: 这是后续设计实现了
                }
            }
            finally { _batchLock.Release(); }
        }
    }

    private PropertyData ExtractPropertyData(MqttMessageContext context)
    {
        // 从上下文提取属性数据（示例逻辑）
        return new PropertyData
        {
            DeviceId = $"{context.ProductKey}/{context.DeviceName}",
            Data = context.ParsedData,
            Timestamp = context.ReceiveTimeUtc
        };
    }

    private class PropertyData
    {
        public string DeviceId { get; internal set; }
        public object? Data { get; internal set; }
        public DateTime Timestamp { get; internal set; }
    }
}