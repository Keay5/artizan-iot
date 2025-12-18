using Artizan.IoT.Mqtts.Options;
using Artizan.IoTHub.Mqtts.Servers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet.AspNetCore;
using MQTTnet.Server;
using System;
using System.Linq;

namespace Artizan.IoTHub.Mqtt.AspNetCore.Extensions;

public static class MqttServerExtensions
{
    public static void UseIoTHubMqttServer(this IApplicationBuilder app, Action<MqttServer>? configure = null)
    {
        app.UseMqttServer(mqttServer =>
        {
            var mqttServerService = app.ApplicationServices.GetRequiredService<IMqttServerService>();
            mqttServerService.ConfigureMqttService(mqttServer);

            configure?.Invoke(mqttServer);
        });

        app.UseEndpoints(endpoint =>
        {
            // TODO: bug, 获取到null
            var ioTHubMqttOptions = app.ApplicationServices
               .GetRequiredService<IOptions<IoTHubMqttAspNetcoreOptions>>()
               .Value;

            // endpoint.MapMqtt("/mqtt");
            endpoint.MapConnectionHandler<MqttConnectionHandler>(
                ioTHubMqttOptions.IoTHubMqttServerEndpointRouteUrl, // 设置MQTT的访问地址,如：localhost:端口/mqtt
                httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                    protocolList => protocolList.FirstOrDefault() ?? string.Empty); // MQTT 支持 HTTP WebSockets
        });
    }

    public static void ConfigureIoTHubMqttServer(
        this WebApplicationBuilder builder,
        Action<KestrelServerOptions>? configureKestrelServerOptions = null)
    {
        /*-------------------------------------------------------------------------------------------
         * fix bug: 部署到 IIS 后, Mqtt client 无法连接 Mqtt server(broker) 
         * see:https://github.com/dotnet/MQTTnet/issues/1471
         */
        builder.WebHost.UseIIS();
        /*-------------------------------------------------------------------------------------------*/

        /*-------------------------------------------------------------------------------------------
         * 适配部署安装为 Windows服务, 
         */
        builder.Host.UseWindowsService();
        /*-------------------------------------------------------------------------------------------*/

        builder.WebHost.UseKestrel((context, options) =>
        {
            /* MQTT */
            var iotMqttServerOptions = context.Configuration.
                GetSection(IoTHubMqttAspNetCoreConsts.IoTHubMqttServerConfigName)
                .Get<IoTMqttServerOptions>();

            // This will allow MQTT connections based on TCP port xxxx .
            options.ListenAnyIP(port: iotMqttServerOptions!.Port, opts => opts.UseMqtt());

            // This will allow MQTT connections based on HTTP WebSockets with URI "localhost:xxxx/mqtt"
            // See code below for URI configuration.
            options.ListenAnyIP(iotMqttServerOptions.WebSocketPort); // Default HTTP pipeline

            configureKestrelServerOptions?.Invoke(options);

            /* Web API 
             * appsettings.json 中 的Kestrel 配置优先级更高
             */
            //options.ListenAnyIP(9397); // 监听所有网络接口的该端口
            //options.ListenAnyIP(44397, listenOptions =>
            //{
            //    /*------------------------------------------------------------------------------
            //     1.生成自签名证书：可以使用 OpenSSL 或者 Windows 自带的 makecert 工具生成自签名证书。

            //       以下是使用 OpenSSL 生成自签名证书的示例命令：
            //         openssl req -newkey rsa:2048 -nodes -keyout key.pem -x509 -days 3650 -out certificate.pem`
            //       这将生成一个包含私钥和证书的 PEM 文件。

            //     2.将证书和私钥转换为 certificate.pfx：使用以下 OpenSSL 命令将 PEM 文件转换为 certificate.pfx 文件：
            //         openssl pkcs12 -export -out certificate.pfx -inkey key.pem -in certificate.pem`
            //       这将提示你输入一个密码，并生成包含证书和私钥的 certificate.pfx 文件。

            //     3.使用权威机构颁发的证书：如果你需要使用由权威机构颁发的证书，你需要购买并按照权威机构的指示进行申请。一般来说，你需要提供证书请求（CSR）以及其他身份验证材料。
            //       权威机构将会审核你的请求并在验证通过后颁发证书。
            //     4.获取证书：权威机构颁发的证书通常以多种格式提供，其中包括 certificate.pfx。你可以从权威机构的管理界面或者提供的下载链接中获取证书。  
            //     */
            //    listenOptions.UseHttps("certificate.pfx", "1234567890");
            //});

        });
    }
}
