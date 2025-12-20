namespace Artizan.IoT;

public static class IoTErrorCodes
{
    public const string Namespace = "IoT";

    //Add your business exception error codes here...
    public const string DefaultError = $"{Namespace}:DefaultError";

    public static class Mqtt
    {
        public const string DeviceDisabled = $"{Namespace}:Mqtt:DeviceDisabled";
        public const string AuthenticationFailed = $"{Namespace}:Mqtt:AuthenticationFailed";
        public const string DeviceAuthenticationFailed = $"{Namespace}:Mqtt:DeviceAuthenticationFailed";
        public const string TopicCannotBeEmpty = $"{Namespace}:Mqtt:TopicCannotBeEmpty";
        public const string DeviceCanOnlyPublishItsOwnCustomTopic = $"{Namespace}:Mqtt:DeviceCanOnlyPublishItsOwnCustomTopic";

    }
}

