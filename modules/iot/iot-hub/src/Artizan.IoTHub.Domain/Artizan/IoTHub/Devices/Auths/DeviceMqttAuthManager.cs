using Artizan.IoT;
using Artizan.IoT.Mqtt.Auth;
using Artizan.IoTHub.Devices.Caches;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Products.Caches;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Devices.Auths;

public class DeviceMqttAuthManager : DomainService
{
    protected ILogger<DeviceMqttAuthManager> Logger { get; }

    protected IMqttSignAuthManager MqttSignAuthManager { get; }

    // TODO: 创建 Product密钥和 Device密钥 服务，用于获取产品密钥和设备密钥
    protected ProductCache ProductCache { get; }
    protected DeviceCache DeviceCache { get; }

    public DeviceMqttAuthManager(
        ILogger<DeviceMqttAuthManager> logger,
        IStringLocalizer<IoTHubResource> localizer,
        IGuidGenerator guidGenerator,
        IMqttSignAuthManager mqttSignAuthManager,
        ProductCache productCache,
        DeviceCache deviceCache,
        IDistributedEventBus distributedEventBus)
    {
        Logger = logger;
        MqttSignAuthManager = mqttSignAuthManager;
        ProductCache = productCache;
        DeviceCache = deviceCache;
    }

    public async Task<MqttAuthResult> ValidateMqttSignAsync(string clientId, string userName, string password)
    {
        var parseResult = MqttSignAuthManager.ParseMqttSignParams(clientId, userName);
        if (!parseResult.Succeeded)
        {
            return parseResult;
        }

        var errorResults = new List<MqttAuthResult>();
        var signParams = parseResult.Data!;
        string secret = string.Empty;
        if (signParams.AuthType.IsOneDeviceOnSecretAuth())
        {
            // 一机一密：使用设备密钥
            secret = await GetDeviceSecretAsync(signParams.ProductKey, signParams.DeviceName);
        }
        else if (signParams.AuthType.IsOneProductOnSecretAuth())
        {
            // 一型一密：使用产品密钥
            secret = await GetProductSecretAsync(signParams.ProductKey);
        }
        else
        {
            var errorMsg = $"不支持的认证类型：{signParams.AuthType}";
            Logger.LogWarning("[MQTT 连接认证] 失败 | 原因：{0}", errorMsg);

            return MqttAuthResult.Failed(IoTMqttErrorCodes.AuthTypeInvalid, errorMsg);
        }

        var connectParams = new MqttConnectParams
        {
            ClientId = clientId,
            UserName = userName,
            Password = password
        };
        var authResult = MqttSignAuthManager.VerifyMqttSign(connectParams, secret);
        if (!authResult.Succeeded)
        {
            errorResults.Add(authResult);
        }

        return errorResults.Any()
            ? MqttAuthResult.Combine(errorResults.ToArray())
            : MqttAuthResult.Success(signParams);
    }

    /// <summary>
    /// 获取设备密钥（一机一密）
    /// 需要根据实际业务实现从数据库或缓存中查询
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="deviceName">设备名称</param>
    /// <returns>设备密钥</returns>
    protected virtual async Task<string> GetDeviceSecretAsync(string productKey, string deviceName)
    {
        /*
         ProductKey: a1B2c3D4e5
         DeviceName: Device_001
         DeviceSecret: sEcReT1234567890abcdef
      
         1.一机一密(无 timeStamp、无random)
            MQTT ClientId: a1B2c3D4e5.Device_001|authType=1,secureMode=2,signMethod=HmacSha256|
            MQTT UserName: Device_001&a1B2c3D4e5
            MQTT Password: 770D2790034C1058B63D50982CD2819AE1E809885E9D82A9018992E5A02CC478

         2.测试一机一密(有timeStamp、无random)
            MQTT ClientId: a1B2c3D4e5.Device_001|authType=1,secureMode=2,signMethod=HmacSha256,timestamp=1765963456467|
            MQTT UserName: Device_001&a1B2c3D4e5
            MQTT Password: 46C2113951A72CC11E4666FD0FC5A136A9DAB90AA1B73A3BB0F61C017F67D928

         3.测试一机一密(无timeStamp、有random)
            MQTT ClientId: a1B2c3D4e5.Device_001|authType=1,secureMode=2,signMethod=HmacSha256,random=0987654321|
            MQTT UserName: Device_001&a1B2c3D4e5
            MQTT Password: 5E60F4846F5FDB1CF1C82BAD6CBC1B0D3588DC0D3A579C264FD84A2D35B81D91
         */

        //TODO: 测试环境可返回固定密钥，生产环境必须替换为实际实现
        return await Task.FromResult("sEcReT1234567890abcdef");

        var deviceSecret = await DeviceCache.GetDeviceSecretAsync(productKey, deviceName);
        return deviceSecret;
    }

    /// <summary>
    /// 获取产品密钥（一型一密）
    /// 需要根据实际业务实现从数据库或缓存中查询
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <returns>产品密钥</returns>
    protected virtual async Task<string> GetProductSecretAsync(string productKey)
    {
        var productSecret = await ProductCache.GetProductSecretAsync(productKey);
        return productSecret;
    }
}
