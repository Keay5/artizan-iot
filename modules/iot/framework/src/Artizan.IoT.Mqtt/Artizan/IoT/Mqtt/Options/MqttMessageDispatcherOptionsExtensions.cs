using Artizan.IoT.Mqtt.Messages.Dispatchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Options;

public static class MqttMessageDispatcherOptionsExtensions
{
    /// <summary>
    /// 注册MQTT消息分发器配置选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置根</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ConfigMqttMessageDispatcherOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // 第一步：绑定配置文件中的Mqtt:Dispatcher节点到配置类
        services.Configure<MqttMessageDispatcherOptions>(configuration.GetSection("IoT:Mqtt:MessageDispatcher"));

        // 第二步：兜底处理，防止配置文件中设置无效值
        services.PostConfigure<MqttMessageDispatcherOptions>(options =>
        {
            // 1. 数值类型兜底（防止配置负数/0导致业务异常）
            options.PartitionCount = options.PartitionCount <= 0 ? Environment.ProcessorCount * 2 : options.PartitionCount;
            options.ChannelCapacity = options.ChannelCapacity <= 0 ? 10000 : options.ChannelCapacity;
            options.BatchSize = options.BatchSize <= 0 ? 100 : options.BatchSize;

            // 2. TimeSpan兜底（防止配置0或无效值导致消息永不超时）
            options.BatchTimeout = options.BatchTimeout == default ? TimeSpan.FromMilliseconds(300) : options.BatchTimeout;

            // 3. 枚举兜底（防止配置无效的枚举值）
            if (!Enum.IsDefined(typeof(MqttMessagePartitionStrategy), options.PartitionStrategy))
            {
                options.PartitionStrategy = MqttMessagePartitionStrategy.DeviceOrdered;
            }

            // 4. bool类型无需兜底：配置绑定未设置时保留默认值true
        });

        return services;
    }
}
