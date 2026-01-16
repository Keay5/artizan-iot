using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Artizan.IoT.Mqtt.Options;

public static class MqttServerOptionsExtensions
{
    /// <summary>
    /// 注册MQTT服务器配置选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置根</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ConfigMqttServerOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // 第一步：绑定配置文件中的节点
        services.Configure<MqttServerOptions>(configuration.GetSection("IoT:MqttServer"));

        // 第二步：兜底处理（可选，根据业务需求）
        // 场景1：如果IpAddress/DomainName为空，设置默认值（比如本地测试用127.0.0.1）
        services.PostConfigure<MqttServerOptions>(options =>
        {
            // 本地开发环境默认值（生产环境必须配置IpAddress/DomainName）
            if (string.IsNullOrWhiteSpace(options.IpAddress) && string.IsNullOrWhiteSpace(options.DomainName))
            {
                options.IpAddress = "127.0.0.1"; // 本地回环地址，方便开发测试
            }

            // 兜底端口值（防止配置文件中配置了无效值，比如0或负数）
            options.Port = options.Port <= 0 ? 1883 : options.Port;
            options.TlsPort = options.TlsPort <= 0 ? 8883 : options.TlsPort;
            options.WebSocketPort = options.WebSocketPort <= 0 ? 5883 : options.WebSocketPort;
        });

        return services;
    }
}
