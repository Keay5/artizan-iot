using Artizan.IoT.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Artizan.IoT.Settings;

public class IoTSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        /* Define module settings here.
         * Use names from IoTSettings class.
         */

        // 0. 批量写入阈值
        
       context.Add(new SettingDefinition(
            name: IoTSettings.Message.Cache.Enabled,
            defaultValue: "true",
            displayName: L("DisplayName:Abp.IoT.Message.Cache.Enabled"),// 启用         
            description: L("DisplayName:Abp.IoT.Message.Cache.EnabledDescription"),//"启用设备数据缓存",
            isVisibleToClients: false));
        // 1. 批量写入阈值
        context.Add(new SettingDefinition(
            name: IoTSettings.Message.Cache.BatchSize,
            defaultValue: "100",
            displayName: L("DisplayName:Abp.IoT.Message.Cache.BatchSize"),//"设备数据缓存批量写入阈值",
            description: L("DisplayName:Abp.IoT.Message.Cache.BatchSizeDescription"),//"达到该数量触发批量写入缓存",
            isVisibleToClients: false));

        // 2. 批量写入超时时间（默认1秒）
        context.Add(new SettingDefinition(
            name: IoTSettings.Message.Cache.BatchTimeoutSeconds,
            defaultValue: "1",
            displayName: L("DisplayName:Abp.IoT.Message.Cache.BatchTimeoutSeconds"), // "设备数据缓存批量写入超时时间",
            description: L("DisplayName:Abp.IoT.Message.Cache.BatchTimeoutSecondsDescription"), // "即使未达阈值，超时后也触发批量写入",
            isVisibleToClients: true));

        // 3. 设备最新值缓存过期时间
        context.Add(new SettingDefinition(
            name: IoTSettings.Message.Cache.LatestDataExpireSeconds,
            defaultValue: "3600",
            displayName: L("DisplayName:Abp.IoT.Message.Cache.LatestDataExpireSeconds"), // "设备数据最新值缓存过期时间",
            description: L("DisplayName:Abp.IoT.Message.Cache.LatestDataExpireSecondsDescription"), // "设备数据最新属性数据的缓存过期时间",
            isVisibleToClients: true));

        // 4. 设备历史数据保留时长
        context.Add(new SettingDefinition(
            name: IoTSettings.Message.Cache.HistoryDataRetainSeconds,
            defaultValue: "900",
            displayName: L("DisplayName:Abp.IoT.Message.Cache.HistoryDataRetainSeconds"), //"设备数据历史数据保留时长",
            description: L("DisplayName:Abp.IoT.Message.Cache.HistoryDataRetainSecondsDescription"), //"缓存中保留的历史数据最大时长（前端最大查询维度15分钟）",
            isVisibleToClients: false));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<IoTResource>(name);
    }
}
