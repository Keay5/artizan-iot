namespace Artizan.IoTHub;

/*
                                                        对比分析（为什么数字编码更优）
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 维度	                             {Namespace}:001 格式	                                                         {Namespace}:DeviceNameInvalid 纯字符串格式
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
安全性	            数字编码不暴露业务语义，避免黑客通过错误码推断系统逻辑                                   纯字符串直接暴露业务关键词，存在语义泄露风险
                    （如不会直接泄露 “设备名非法” 的核心规则）                                             纯字符串无分级规则，新增错误码易混乱（如后续加 DeviceNameDuplicate 无规律）
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
扩展性	            数字段可按模块分段（如 001-099 设备、100-199 产品），易维护	                             若误将字符串作为本地化键，易与多语言文案耦合
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
国际化适配	        与 ABP 本地化框架天然兼容（错误码仅为标识，消息通过资源文件配置）                        纯字符串无行业标准，对接外部系统时需额外转换
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
第三方对接	        符合 IoT 行业规范（如阿里云 / 华为云 IoT 平台均用数字错误码）                            纯字符串格式可能导致对接时的理解差异。
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
可读性（团队内)     结合注释 + 文档，数字段可映射成 “错误码字典”，易检索	                                 字符串直观但无分级，量大时难以管理
 ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

第二种写法  {Namespace}:DeviceNameInvalid 仅适用于纯内部调试场景，但存在以下问题：
 - 耦合性高：若后续修改业务术语（如 “设备名” 改为 “设备标识”），需同时修改错误码字符串，易遗漏；
 - 框架适配差：ABP 本地化框架推荐 “错误码标识 + 资源文件文案” 的模式，纯字符串易与文案混淆；
 - 排查效率低：日志中出现 DeviceNameInvalid 不如 IoTHub:101 直观（数字码可快速定位到 “设备模块 - 101 号错误”）。
 
 */
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

