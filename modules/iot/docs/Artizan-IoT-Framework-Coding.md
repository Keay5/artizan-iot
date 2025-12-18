# Artizan.IoT 

创建解决方案：

```bash
iot\framwork> abp new-module Artizan.IoT --template module:ddd  --database-provider ef,mongodb --ui-framework mvc,blazor --version 9.3.6
```



# 添加类库

## Artizan.IoT.Core

创建类库：Artizan.IoT.Core，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.Core --template lib.class-library --solution-name Artizan.IoT.sln --folder src
```

命令行参见：https://abp.io/docs/latest/cli#new-package



## Artizan.IoT.Abstractions

创建类库：Artizan.IoT.Abstractions，在 `*.abpmdl`  （ABP Module 文件）所在目录执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.Abstractions --template lib.class-library --solution-name Artizan.IoT.sln --folder src
```



# MQTT

## Artizan.IoT.Mqtt.Application.Contracts

### 创建

```bash
iot\framework> abp new-package --name Artizan.IoT.Mqtt.Application.Contracts --template lib.application-contracts --solution-name Artizan.IoT.sln --folder src
```



## Artizan.IoT.Mqtt.Application

### 创建

```bash
iot\framework> abp new-package --name Artizan.IoT.Mqtt.Application --template lib.application --with-automapper --solution-name Artizan.IoT.sln --folder src
```



## Artizan.IoT.Mqtt.HttpApi 

### 创建

```bash
iot\framework> abp new-package --name Artizan.IoT.Mqtt.HttpApi --template lib.http-api --solution-name Artizan.IoT.sln --folder src
```



## Artizan.IoT.Mqtt.HttpApi.Client

### 创建

```bash
iot\framework> abp new-package --name Artizan.IoT.Mqtt.HttpApi.Client --template lib.http-api-client --solution-name Artizan.IoT.sln --folder src
```



## Artizan.IoT.Mqtt.HttpApi.TestHost

### 创建

```bash
iot\framework> abp new-package --name Artizan.IoT.Mqtt.HttpApi.TestHost --template host.http-api --with-serilog --with-swagger --solution-name Artizan.IoT.sln --folder apps
```



### 配置 MQTT Server

在 HostModule 中添加如下代码：

- appsettings.json中添加配置节点：

  ```xml
  {
    ......
    "IoTMqttServer": {
      "IpAddress": "localhost",
      "DomainName": "",
      "Port": 2883,
      "EnableTls": "false",
      "TlsPort": 8883,
      "WebSocketPort": 5883
    }
  }
  ```

  

- 配置 MqttServer

  ```C#
   public override void ConfigureServices(ServiceConfigurationContext context)
      {
          ConfigureIoTMqttServer(context, configuration);
      }
  
  
      private void ConfigureIoTMqttServer(ServiceConfigurationContext context, IConfiguration configuration)
      {
          var iotMqttServerOptions = configuration.GetSection("IoTMqttServer").Get<IoTMqttServerOptions>();
  
          context.Services.AddHostedMqttServerWithServices(builder =>
          {
              builder.WithDefaultEndpoint();
              builder.WithDefaultEndpointPort(iotMqttServerOptions!.Port);
          });
  
          context.Services.AddMqttConnectionHandler();
          context.Services.AddConnections();
          context.Services.AddMqttTcpServerAdapter();
          context.Services.AddMqttWebSocketServerAdapter();
      }
  ```



- UseMqttServer

  ```C#
      public override void OnApplicationInitialization(ApplicationInitializationContext context)
      {
  +       app.UseMqttServer();
          app.UseConfiguredEndpoints();
      }
  ```

  UseMqttServer() 为扩展方法：

  ```xml
  public static class MqttServerExtensions
  {
      public static void UseMqttServer(this IApplicationBuilder app)
      {
          app.UseMqttServer(mqttServer =>
          {
              var mqttServerService = app.ApplicationServices.GetRequiredService<IMqttServerService>();
              mqttServerService.ConfigureMqttService(mqttServer);
          });
  
          app.UseEndpoints(endpoint =>
          {
              // endpoint.MapMqtt("/mqtt");
              endpoint.MapConnectionHandler<MqttConnectionHandler>(
                  "/mqtt", // 设置MQTT的访问地址: localhost:端口/mqtt
                  httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                      protocolList => protocolList.FirstOrDefault() ?? string.Empty); // MQTT 支持 HTTP WebSockets
          });
      }
  }
  ```



### 适配安装部署为 Windows服务

目标：可以将应用程序安装为 Window服务



- 引用包：

  ```xm
  <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" />
  ```

  

- 适配部署安装为 Windows服务

  在 Program.cs 添加如下代码

  ```C#
              /*-------------------------------------------------------------------------------------------
               * 适配部署安装为 Windows服务, 
               */
              builder.Host.UseWindowsService();
              /*-------------------------------------------------------------------------------------------*/
  ```

​    

- 配置 Kestrel

  ```C#
          builder.WebHost.UseKestrel((context, options) =>
          {
              /* MQTT */
              var iotMqttServerOptions = context.Configuration.GetSection("IoTMqttServer").Get<IoTMqttServerOptions>();
  
              // This will allow MQTT connections based on TCP port xxxx .
              options.ListenAnyIP(port: iotMqttServerOptions!.Port, opts => opts.UseMqtt());
  
              // This will allow MQTT connections based on HTTP WebSockets with URI "localhost:xxxx/mqtt"
              // See code below for URI configuration.
              options.ListenAnyIP(iotMqttServerOptions.WebSocketPort); // Default HTTP pipeline
  
              /* Web API 
               * appsettings.json Kestrel 配置中的优先级更高
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
  ```

- 适配 Windows服务的日志

   当你将 **AspNet Core** 应用程序安装为 **Windows服务** 后，服务运行时的工作目录并不是你应用程序的根目录 ，而是默认在 ` C:\Windows\System32 ` 

  在  AspNet Core 应用程序 部署为Windows服务后， 要想将日志输出到在 AspNet Core 应用程序根目录下，你有以下几种做法：

  #### 方法一

  使用 `WindowsServiceHelpers.IsWindowsService()` 检查是否是 Windows服务，如果是，修改

   Windows服务的 `ContentRootPath`  的值为当前应用程序的根目录 (`AppContext.BaseDirectory` ),如下代码所示：

  代码清单：Program.cs

  ```sh
      public async static Task<int> Main(string[] args)
      {
            Log.Logger = new LoggerConfiguration()
              ...
              .WriteTo.Async(c => c.File("Logs/logs.txt"))
              ...
              .CreateLogger();
             
  +           var options = new WebApplicationOptions
  +            { 
  +                Args = args,
  +                ContentRootPath = WindowsServiceHelpers.IsWindowsService() 
  +                   ? AppContext.BaseDirectory 
  +                   : default
  +            };
  
  -            var builder = WebApplication.CreateBuilder(args);
  +            var builder = WebApplication.CreateBuilder(options);
  ```

  

  #### 方法二

   使用 `AppContext.BaseDirectory` 来动态获取当前应用程序的根目录，然后构建日志文件的路径 

  ```sh
      public async static Task<int> Main(string[] args)
      {
            Log.Logger = new LoggerConfiguration()
              ...
  -           .WriteTo.Async(c => c.File("Logs/logs.txt"))
  +           .WriteTo.Async(c => c.File(GetLogFilePath()))
              ...
              .CreateLogger();
      }
  
      private static string GetLogFilePath()
      {
  #if DEBUG
          return $"Logs/log-{DateTime.Now.ToString("yyyy-MM-dd")}.txt";
  #else
          string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
          if (!Directory.Exists(logDirectory))
          {
              Directory.CreateDirectory(logDirectory);
          }
          string fileName = $"log-{DateTime.Now.ToString("yyyy-MM-dd")}.txt";
          return Path.Combine(logDirectory, fileName);
  #endif
      }
  ```

  

  注意：不要使用`Directory.GetCurrentDirectory())`，否则在部署为Winddows服务时有异常 。



- 兼容不使用 Kestrel 的情况 

  在 Moudle中添加适配代码，兼容不使用 Kestrel（例如：部署到IIS) 的情况

  ```C#
          context.Services.AddHostedMqttServerWithServices(builder =>
          {
              /*-------------------------------------------------------------------------------------------
              如果不使用 Kestrel，必须调用 builder.WithDefaultEndpoint()，否则 Mqtt client 无法连接
              */
              builder.WithDefaultEndpoint();
              /*-------------------------------------------------------------------------------------------*/
              builder.WithDefaultEndpointPort(iotMqttServerOptions!.Port);
          });
  ```



- 兼容部署到IIS的情况

  在 Program.cs 添加如下代码，兼容部署到IIS的情况。

  ```xml
           /*-------------------------------------------------------------------------------------------
            * fix bug: 部署到 IIS 后, Mqtt client 无法连接 Mqtt server(broker) 
            * see:https://github.com/dotnet/MQTTnet/issues/1471
            */
           builder.WebHost.UseIIS();
           /*-------------------------------------------------------------------------------------------*/
  ```

  

# IoT.Demo.BackendAdmin

后台管理



## 创建

使用 app 模版创建

```bash
iot\framework\apps> abp new Artizan.IoT.Demo.BackendAdmin --template app --ui-framework mvc --mobile none --database-provider ef --connection-string "Server=localhost;Database=IoTDemoBackendAdmin;User Id=sa;Password=123456;TrustServerCertificate=True;" --database-management-system SqlServer --sample-crud-page --theme leptonx-lite --create-solution-folder --version 9.3.6
```

