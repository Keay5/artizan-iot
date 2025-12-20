namespace Artizan.IoTHub;

/// <summary>
/// 参考资料：
/// https://help.aliyun.com/zh/iot/user-guide/iot-platform-logs?spm=a2c4g.11186623.0.0.6d2518a8R9j9D7
/// </summary>
public static class IoTHubErrorCodes
{
    public const string Namespace = "IoTHub";
    //Add your business exception error codes here...

    //Tips: 重要错误码可以使用数字， 防止向外界暴漏错误的信息。

    //------------------------ 通用 ------------------------
    public const string DescriptionInvalid = $"{Namespace}:DescriptionInvalid";

    //------------------------ 产品 ------------------------
    public const string ProductKeyInvalid = $"{Namespace}:ProductKeyInvalid";
    public const string DuplicateProductKey = $"{Namespace}:DuplicateProductKey";
    public const string ProductNameInvalid = $"{Namespace}:ProductNameInvalid";
    public const string ProducCategoryNameInvalid = $"{Namespace}:ProducCategoryNameInvalid";
    public const string DuplicateProductName = $"{Namespace}:DuplicateProductName";
    public const string CannotUpdatePulishedProduct = $"{Namespace}:CannotUpdatePulishedProduct";
    //-------------------------产品模块

    //------------------------ 产品模块 ------------------------
    public const string ProductModuleNameInvalid = $"{Namespace}:ProductModuleNameInvalid";
    public const string ProductModuleIdentifierInvalid = $"{Namespace}:ProductModuleIdentifierInvalid";
    public const string DuplicateProductModuleIdentifier = $"{Namespace}:DuplicateProductModuleIdentifier";
    public const string DuplicateProductModuleName = $"{Namespace}:DuplicateProductModuleName";

    //------------------------ 设备模块 ------------------------

    public const string DeviceNameInvalid = $"{Namespace}:DeviceNameInvalid";
    public const string DeviceRemarkNameInvalid = $"{Namespace}:DeviceRemarkNameInvalid";
    public const string DuplicateDeviceName = $"{Namespace}:DuplicateDeviceName";
}
