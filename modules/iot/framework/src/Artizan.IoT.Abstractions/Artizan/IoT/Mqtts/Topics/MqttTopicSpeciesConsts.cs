using Volo.Abp.Reflection;

namespace Artizan.IoT.Mqtts.Topics;

/// <summary>
/// 参见：主题分类介绍
/// https://help.aliyun.com/zh/iot/user-guide/what-is-topic?spm=5176.11485173.console-base_help.dexternal.60fc70d803P07T
/// https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services?spm=a2c4g.11186623.0.0.4d543778oQdoCU#section-g4j-5zg-12b
/// </summary>
public static class MqttTopicSpeciesConsts
{
    public const string Sys = "/sys";
    public const string OTA = "/ota";
    public const string Ext = "/ext";
    public const string Shadow = "/shadow";
    public const string Broadcast = "/broadcast";
    public const string ProductKey = "/${productKey}";
    public const string DeviceName = "/${deviceName}";
    public const string ProductKeyDeviceName = ProductKey + DeviceName;
    public const string TslServiceIdentifier = "/${tsl.service.identifier}";
    public const string TslEventIdentifier = "/${tsl.event.identifier}";

    public static class BasicCommunication
    {
        public static class OTAUpgrade
        {
            public const string OTADevice = OTA + "/device";
            /// <summary>
            /// 发布 设备上报固件升级信息,
            /// </summary>
            public const string Inform = OTADevice + "/inform" + ProductKeyDeviceName;
            /// <summary>
            /// 订阅 固件升级信息下行,
            /// </summary>
            public const string Upgrade = OTADevice + "/upgrade" + ProductKeyDeviceName;
            /// <summary>
            /// 发布 设备上报固件升级进度
            /// </summary>
            public const string Progress = OTADevice + "/progress" + ProductKeyDeviceName;
            /// <summary>
            /// 发布 设备主动拉取固件升级信息
            /// </summary>
            public const string Firmware = Sys + ProductKeyDeviceName + "/thing" + OTA + "firmware/get";
        }

        public static class DeviceLabel
        {
            public const string Default = Sys + ProductKeyDeviceName + "/thing/deviceinfo";
            /// <summary>
            /// 发布 设备上报标签数据
            /// </summary>
            public const string Update = Default + "/update";
            /// <summary>
            /// 订阅 云端响应标签上报
            /// </summary>
            public const string UpdateReply = Default + "/update_reply";
            /// <summary>
            /// 订阅 设备删除标签信息
            /// </summary>
            public const string Delete = Default + "/delete";
            /// <summary>
            /// 发布 云端响应标签删除
            /// </summary>
            public const string DeleteReply = Default + "/delete_reply";
        }

        public static class ClockSync
        {
            public const string ExtNtp = Ext + "/ntp";
            /// <summary>
            /// 发布 NTP 时钟同步请求
            /// </summary>
            public const string ExtNtpRequest = ExtNtp + ProductKeyDeviceName + "/request";
            /// <summary>
            /// 订阅 NTP 时钟同步响应
            /// </summary>
            public const string ExtNtpResponse = ExtNtp + ProductKeyDeviceName + "/response";
        }

        public static class DeviceShadow
        {
            /// <summary>
            /// 发布 设备影子发布
            /// </summary>
            public const string Update = Shadow + "update" + ProductKeyDeviceName;
            /// <summary>
            /// 订阅 设备接收影子变更
            /// </summary>
            public const string Get = Shadow + "get" + ProductKeyDeviceName;
        }

        public static class ConfigUpgrade
        {
            public const string Default = Sys + ProductKeyDeviceName + "/thing/config";
            /// <summary>
            /// 订阅 云端主动下推配置信息
            /// </summary>
            public const string Push = Default + "/push";
            /// <summary>
            /// 发布 设备端查询配置信息
            /// </summary>
            public const string Get = Default + "/get";
            /// <summary>
            /// 订阅 云端响应配置信息
            /// </summary>
            public const string GetReply = Default + "/get_reply";
        }

        public static class Broadcasts
        {
            /// <summary>
            /// 订阅 广播 Topic，identifier 为用户自定义字符串
            /// </summary>
            public const string Identifier = Broadcast + ProductKeyDeviceName + "/${identifier}";
        }
    }

    public static class ThingModelCommunication
    {

        public static class PropertyReport
        {
            public const string Default = Sys + ProductKeyDeviceName + "/thing/event/property";
            /// <summary>
            /// 发布 设备属性上报
            /// </summary>
            public const string Post = Default + "/post";
            /// <summary>
            /// 订阅 云端响应属性上报
            /// </summary>
            public const string PostReply = Default + "/post_reply";
        }

        public static class PropertySetting
        {
            public const string Default = Sys + ProductKeyDeviceName + "/thing/service/property";
            /// <summary>
            /// 订阅 设备属性设置
            /// </summary>
            public const string Set = Default + "/set";
        }

        public static class EventReport
        {
            public const string Default = Sys + ProductKeyDeviceName + "/thing/event" + TslEventIdentifier;
            /// <summary>
            /// 发布 设备事件上报
            /// </summary>
            public const string Post = Default + "/post";
            /// <summary>
            /// 订阅 云端响应事件上报
            /// </summary>
            public const string PostReply = Default + "/post_reply";
        }

        public static class ServiceCalling
        {
            public const string Default = Sys + ProductKeyDeviceName + "/thing/service";
            /// <summary>
            /// 发布 设备服务调用
            /// </summary>
            public const string Identifier = Default + TslServiceIdentifier;
            /// <summary>
            /// 订阅 设备端响应服务调用
            /// </summary>
            public const string IdentifierReply = Default + TslServiceIdentifier;
        }

        /// <summary>
        /// 穿透数据通信
        /// </summary>
        public static class PassThrough
        {
            public const string Default = Sys + ProductKeyDeviceName + "/thing/model";

            #region 数据上行

            /// <summary>
            /// 发布 设备透传数据
            /// </summary>
            public const string UpRaw = Default + "/up_raw";
            /// <summary>
            /// 订阅 云端响应设备透传数据
            /// </summary>
            public const string UpRawReply = Default + "/up_raw_reply";

            #endregion

            #region 数据下行

            /// <summary>
            /// 发布 设备透传数据
            /// </summary>
            public const string DownRaw = Default + "/down_raw";
            /// <summary>
            /// 订阅 云端响应设备透传数据
            /// </summary>
            public const string DownRawReply = Default + "/down_raw_reply";

            #endregion
        }
    }

    public static class Custom
    {
        public const string Default = ProductKeyDeviceName + "/user";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(MqttTopicSpeciesConsts));
    }
}
