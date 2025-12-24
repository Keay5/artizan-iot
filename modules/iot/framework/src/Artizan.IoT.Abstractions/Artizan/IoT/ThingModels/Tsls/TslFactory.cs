using Artizan.IoT.ThingModels.Tsls;
using Artizan.IoT.ThingModels.Tsls.Builders;
using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Properties;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;
using System;
using System.Collections.Generic;
using System.Linq;

public static class TslFactory
{
    #region Property 属性创建方法
    /// <summary>
    /// 通过建造者模式创建属性
    /// </summary>
    public static PropertyBuilder CreatePropertyBuilder(
        string identifier,
        string name,
        AccessModes accessMode,
        bool required,
        DataTypes dataType,
        string? description = null)
    {
        return new PropertyBuilder(identifier, name, accessMode, required, dataType, description);
    }

    /// <summary>
    /// 直接创建属性（简化版）
    /// </summary>
    public static Property CreateProperty(
        string identifier,
        string name,
        AccessModes accessMode,
        bool required,
        DataTypes dataType,
        ISpecsDo specsDo,
        string? description = null)
    {
        return CreatePropertyBuilder(identifier, name, accessMode, required, dataType, description)
            .WithSpecsDo(specsDo)
            .Build();
    }
    #endregion

    #region Service 创建方法
    /// <summary>
    /// 创建服务（Service）
    /// </summary>
    /// <param name="identifier">服务唯一标识</param>
    /// <param name="name">服务名称</param>
    /// <param name="callType">调用类型（同步/异步）</param>
    /// <param name="inputDatas">输入参数列表</param>
    /// <param name="outputDatas">输出参数列表</param>
    /// <param name="description">描述信息</param>
    /// <returns>Service实例</returns>
    public static Service CreateService(
        string identifier,
        string name,
        ServiceCallTypes callType,
        List<CommonInputParam>? inputDatas = null,
        List<OutputParam>? outputDatas = null,
        string? description = null)
    {
        // 参数校验
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentNullException(nameof(identifier), "服务标识不能为空");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "服务名称不能为空");
        }

        return new Service
        {
            Identifier = identifier,
            Name = name,
            CallType = callType,
            Method = ServiceMethodGenerator.GenerateCustomServiceMethod(identifier),
            InputData = inputDatas?.Cast<IInputParam>().ToList() ?? new List<IInputParam>(),
            OutputData = outputDatas ?? [],
            Desc = description
        };
    }

    #endregion

    #region Event 创建方法
    /// <summary>
    /// 创建事件（Event）
    /// </summary>
    /// <param name="identifier">事件唯一标识</param>
    /// <param name="name">事件名称</param>
    /// <param name="eventType">事件类型（信息/告警/故障）</param>
    /// <param name="outputDatas">输出参数列表</param>
    /// <param name="description">描述信息</param>
    /// <returns>Event实例</returns>
    public static Event CreateEvent(
        string identifier,
        string name,
        EventTypes eventType,
        List<OutputParam>? outputDatas = null,
        string? description = null)
    {
        // 参数校验
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentNullException(nameof(identifier), "事件标识不能为空");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "事件名称不能为空");
        }

        return new Event
        {
            Identifier = identifier,
            Name = name,
            Type = eventType,
            Method = EventMethodGenerator.GenerateCustomEventPostMethod(identifier),
            OutputData = outputDatas ?? [],
            Desc = description
        };
    }

    /// <summary>
    /// 创建事件输出参数（OutputParam）
    /// </summary>
    /// <param name="identifier">参数标识</param>
    /// <param name="name">参数名称</param>
    /// <param name="dataType">数据类型配置</param>
    /// <returns>OutputParam实例</returns>
    public static OutputParam CreateEventOutputParam(
        string identifier,
        string name,
        DataType dataType)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentNullException(nameof(identifier), "事件参数标识不能为空");
        }

        if (dataType == null)
        {
            throw new ArgumentNullException(nameof(dataType), "数据类型配置不能为空");
        }

        return new OutputParam
        {
            Identifier = identifier,
            Name = name,
            DataType = dataType
        };
    }

    #endregion

}

