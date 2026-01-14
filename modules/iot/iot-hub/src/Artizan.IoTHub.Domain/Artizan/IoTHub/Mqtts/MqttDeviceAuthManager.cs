using Artizan.IoT.Mqtt.Auths;
using Artizan.IoTHub.Devices.Caches;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Products.Caches;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Mqtts;

public class MqttDeviceAuthManager : DomainService
{
    protected ILogger<MqttDeviceAuthManager> Logger { get; }

    // TODO: 创建 Product密钥和 Device密钥 服务，用于获取产品密钥和设备密钥
    protected ProductCache ProductCache { get; }
    protected DeviceCache DeviceCache { get; }

    public MqttDeviceAuthManager(
        ILogger<MqttDeviceAuthManager> logger,
        IStringLocalizer<IoTHubResource> localizer,
        IGuidGenerator guidGenerator,
        ProductCache productCache,
        DeviceCache deviceCache,
        IDistributedEventBus distributedEventBus)
    {
        Logger = logger;
        ProductCache = productCache;
        DeviceCache = deviceCache;
    }

    /// <summary>
    /// 验证设备MQTT连接签名（核心领域规则）
    /// </summary>
    public async Task<MqttAuthValidationResult> ValidateMqttSignAsync(string clientId, string userName, string password)
    {
        // 校验输入参数基础合法性
        var paramCheckResult = MqttSignAuthManager.ValidateBaseParameters(clientId, userName, password);
        if (!paramCheckResult.IsSuccess)
        {
            return new MqttAuthValidationResult(false, null, paramCheckResult.Message);
        }

        // 解析ClientId和UserName，提取认证参数
        var authParams = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);
        if (authParams == null)
        {
            return new MqttAuthValidationResult(false, null, paramCheckResult.Message);
        }

        return await ValidateMqttSignAsync(authParams, clientId, userName, password);
    }

    #region 辅助方法

    ///// <summary>
    ///// 解析认证参数
    ///// 从ClientId和UserName中解析出ProductKey、DeviceName和认证类型等信息
    ///// </summary>
    ///// <param name="clientId">MQTT ClientId</param>
    ///// <param name="userName">MQTT UserName</param>
    ///// <returns></returns>
    //private async Task<(MqttAuthParams? AuthParams, bool Success, string? errorMessage)> ParseMqttAuthParamsAsync(string clientId, string userName)
    //{
    //    /*TODO:定义错误码，并于MQTTnet 错误码映射*/
    //    /* TODO:使用 MqttAuthResult、 MqttAuthErrorCode？ */

    //    var authParams = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

    //    // 防御性校验：避免空指针/非法参数导致后续逻辑异常
    //    if (authParams == null || string.IsNullOrWhiteSpace(authParams.ProductKey) || string.IsNullOrWhiteSpace(authParams.DeviceName))
    //    {
    //        var errorMessage = "无法解析认证参数，请检查「ClientId」和「UserName」格式是否正确";
    //        Logger.LogWarning("[MQTT 连接认证] 认证失败 | 无法解析认证参数 | ClientId={ClientId} | UserName={UserName} | 原因：{ErrorMessage}",
    //            clientId, userName, errorMessage);

    //        return (null, false, errorMessage);
    //    }

    //    return (authParams, true, null);
    //}

    ///// <summary>
    ///// 验证基础连接参数
    ///// 检查ClientId、UserName和Password是否为空
    ///// </summary>
    ///// <param name="clientId">MQTT ClientId</param>
    ///// <param name="userName">MQTT UserName</param>
    ///// <returns>验证是否通过</returns>
    //private async Task<(bool Success, string? errorMessage)> ValidateBasicParamsAsync(string clientId, string userName, string password)
    //{
    //    /*TODO?:定义错误码，并于MQTTnet 错误码映射*/
    //    /* TODO?:使用 MqttAuthResult、 MqttAuthErrorCode？ */

    //    if (string.IsNullOrWhiteSpace(clientId))
    //    {
    //        var errorMessage = "ClientId不能为空";
    //        //eventArgs.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
    //        //eventArgs.ReasonString = ReasonString;
    //        Logger.LogInformation("[MQTT 连接认证] 认证失败 | 原因：{ErrorMessage}}", errorMessage);

    //        return (false, errorMessage);
    //    }

    //    if (string.IsNullOrWhiteSpace(userName))
    //    {
    //        var errorMessage = "UserName不能为空";
    //        //eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
    //        //eventArgs.ReasonString = ReasonString;
    //        Logger.LogInformation("[MQTT 连接认证] 认证失败 | 原因：{ErrorMessage}}", errorMessage);

    //        return (false, errorMessage);
    //    }

    //    if (string.IsNullOrWhiteSpace(userName))
    //    {
    //        var errorMessage = "Password不能为空";
    //        //eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
    //        //eventArgs.ReasonString = ReasonString;
    //        Logger.LogInformation("[MQTT 连接认证] 认证失败 | 原因：{ErrorMessage}}", errorMessage);

    //        return (false, errorMessage);
    //    }

    //    return (true, null);
    //}

    #endregion

    /// <summary>
    /// 验证设备MQTT连接签名（核心领域规则）
    /// </summary>
    protected async Task<MqttAuthValidationResult> ValidateMqttSignAsync(MqttAuthParams authParams, string clientId, string userName, string password)
    {
        /*TODO:定义错误码，并MQTTnet 错误码映射*/

        Check.NotNullOrEmpty(authParams.ProductKey, nameof(authParams.ProductKey));
        Check.NotNullOrEmpty(authParams.DeviceName, nameof(authParams.DeviceName));
        Check.NotNullOrEmpty(clientId, nameof(clientId));
        Check.NotNullOrEmpty(userName, nameof(userName));
        Check.NotNullOrEmpty(password, nameof(password));

        string secret;
        if (authParams.AuthType.IsOneDeviceOnSecretAuth())
        {
            // 一机一密：使用设备密钥
            secret = await GetDeviceSecretAsync(authParams.ProductKey, authParams.DeviceName);
        }
        else if (authParams.AuthType.IsOneProductOnSecretAuth())
        {
            // 一型一密：使用产品密钥
            secret = await GetProductSecretAsync(authParams.ProductKey);
        }
        else
        {
            var errorMsg = $"不支持的认证类型：{authParams.AuthType}";
            Logger.LogWarning("[MQTT 连接认证] 失败 | 原因：{0}", errorMsg);

            throw new MqttAuthenticationFailedException(errorMsg);
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            var errorMsg = $"设备密钥不存在";
            Logger.LogWarning("[MQTT 连接认证] 失败 | 原因：{0}", errorMsg);
            throw new MqttAuthenticationFailedException("设备密钥不存在");
        }

        // 签名验证（签名算法校验）
        var authResult = MqttSignAuthManager.ValidateSign(
            authParams.AuthType, clientId, userName, password, secret
        );

        if (!authResult.IsSuccess)
        {
            throw new MqttAuthenticationFailedException(
                $"认证失败: {authResult.Message} (错误码: {authResult.Code})");
        }

        return new MqttAuthValidationResult(true, authResult.Params, null);
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
