using Artizan.IoT.ScriptDataCodec.JavaScript.Pooling;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// JS编解码器依赖注入扩展
/// 设计思路：一键注册所有相关服务，简化业务系统集成
/// </summary>
public static class JaveScriptCodecServiceExtensions
{
    public static IServiceCollection AddJaveScriptDataCodec(this IServiceCollection services)
    {
        // 注册对象池管理器（单例）
        services.AddSingleton<JavaScriptCodecPoolManager>();
        return services;
    }
}
