namespace Artizan.IoTDA;

public static class IoTDAErrorCodes
{
    public const string Namespace = "IoTDA";

    //Add your business exception error codes here...
    //Tips: 错误码使用数字，而不是使用具体的单词，是基于安全考虑，防止向外界暴漏错误的信息。
    public const string ProductKeyInvalid = $"{Namespace}:000001";
    public const string ProductNameInvalid = $"{Namespace}:000002" ;
    public const string DeviceTypeNameInvalid = $"{Namespace}:000003";
    public const string DescriptionInvalid = $"{Namespace}:000004";
}
