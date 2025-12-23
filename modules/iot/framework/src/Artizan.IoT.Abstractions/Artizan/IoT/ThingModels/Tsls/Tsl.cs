using Artizan.IoT.ThingModels.Tsls.MetaDatas;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Properties;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Volo.Abp;

namespace Artizan.IoT.ThingModels.Tsls;

/// <summary>
/// ThingSpecificationLanguage (TSL) 模型定义
/// 核心规则：
/// 1. 构造函数仅初始化基础属性，不创建任何内置服务/事件
/// 2. 内置服务/事件仅在属性变更时动态创建
/// 3. propertySet服务：仅当存在ReadAndWrite属性时创建/保留
/// 4. propertyGet服务：仅当存在ReadOnly/ReadAndWrite属性时创建/保留
/// 5. propertyPost事件：仅当存在任何属性时创建/保留
/// 6. 直接操作Properties集合（Add/Remove）也触发上述规则
///
/// ---------------------------
/// 参考资料：
/// ThingSpecificationLanguage (TSL) :
/// https://help.aliyun.com/zh/iot/user-guide/tsl-parameters#concept-2070735
/// ThingModelJson:
/// https://help.aliyun.com/zh/iot/developer-reference/data-structure-of-thingmodeljson
/// QueryThingModel:
/// https://help.aliyun.com/zh/iot/developer-reference/api-kft63f
/// 
/// </summary>
public class Tsl
{
    /// <summary>
    /// TSL 模式文件路径
    /// </summary>
    public string Schema { get; set; } = @"Artizan/IoT/ThingModels/Tsls/Resource/thing-models/iot-tsl-schema.json";

    /// <summary>
    /// 产品配置信息
    /// </summary>
    public Profile Profile { get; set; } = new();

    /// <summary>
    /// 可观察的属性集合（监听Add/Remove操作）
    /// </summary>
    private ObservableCollection<Property> _properties;

    /// <summary>
    /// 属性列表（支持直接操作触发同步）
    /// </summary>
    public IList<Property> Properties => _properties;

    /// <summary>
    /// 服务列表（初始化为空，无任何内置服务）
    /// </summary>
    public List<Service> Services { get; set; } = new();

    /// <summary>
    /// 事件列表（初始化为空，无任何内置事件）
    /// </summary>
    public List<Event> Events { get; set; } = new();

    /// <summary>
    /// 功能块ID,即：Identifier
    /// </summary>
    public string FunctionBlockId { get; protected set; }

    /// <summary>
    /// 功能块名称
    /// </summary>
    public string FunctionBlockName { get; protected set; }

    /// <summary>
    /// 是否默认功能块
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 描述信息
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 无参构造函数（用于序列化/反序列化）
    /// </summary>
    protected Tsl()
    {
        _properties = new ObservableCollection<Property>();
        _properties.CollectionChanged += OnPropertiesCollectionChanged;
    }

    /// <summary>
    /// 带参构造函数（仅初始化基础属性，不创建任何服务/事件）
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="productModuleIdentifier">模块标识符</param>
    /// <param name="productModuleName">模块名称</param>
    /// <param name="isDefault">是否默认模块</param>
    /// <param name="version">版本号</param>
    /// <param name="description">描述信息</param>
    public Tsl(
        string productKey,
        string productModuleIdentifier,
        string productModuleName,
        bool isDefault,
        string version,
        string? description)
    {
        // 仅初始化基础属性
        Profile = new Profile
        {
            ProductKey = productKey,
            Version = version
        };

        // 初始化属性的可观察集合，并监听变更
        _properties = new ObservableCollection<Property>();
        _properties.CollectionChanged += OnPropertiesCollectionChanged;

        Services = new List<Service>();
        Events = new List<Event>();

        FunctionBlockId = productModuleIdentifier;
        FunctionBlockName = productModuleName;
        IsDefault = isDefault;
        Description = description;
    }

    public Tsl SetFunctionBlockId(string functionBlockId)
    {
        Check.NotNullOrWhiteSpace(functionBlockId, nameof(functionBlockId));
        //TODO:校验 Identifier
        FunctionBlockId = functionBlockId;
        return this;
    }

    public Tsl SetFunctionBlockName(string functionBlockName)
    {
        Check.NotNullOrWhiteSpace(functionBlockName, nameof(functionBlockName));
        //TODO:校验 FunctionBlockName
        FunctionBlockName = functionBlockName;
        return this;
    }

    #region Property

    /// <summary>
    /// 属性集合变更事件处理器（Add/Remove时触发同步）
    /// </summary>
    private void OnPropertiesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncBuiltInServicesAndEvents();
    }

    /// <summary>
    /// 添加属性（兼容原有API）
    /// </summary>
    /// <param name="property">属性实例</param>
    /// <exception cref="ArgumentNullException">属性为空时抛出</exception>
    public void AddProperty(Property property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property), "属性实例不能为空");
        }

        _properties.Add(property);
    }

    /// <summary>
    /// 移除属性（兼容原有API）
    /// </summary>
    /// <param name="identifier">属性标识符</param>
    /// <returns>是否移除成功</returns>
    /// <exception cref="ArgumentException">标识符为空时抛出</exception>
    public bool RemoveProperty(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("属性标识符不能为空或空白", nameof(identifier));
        }

        var property = _properties.FirstOrDefault(p => p.Identifier == identifier);
        if (property == null)
        {
            return false;
        }

        _properties.Remove(property);
        return true;
    }

    /// <summary>
    /// 批量设置属性（触发同步）
    /// </summary>
    /// <param name="properties">属性列表</param>
    public void SetProperties(IEnumerable<Property> properties)
    {
        _properties.Clear();
        if (properties != null)
        {
            foreach (var property in properties)
            {
                _properties.Add(property);
            }
        }
    }

    /// <summary>
    /// 同步内置服务和事件（严格按属性类型动态管理）
    /// </summary>
    private void SyncBuiltInServicesAndEvents()
    {
        SyncPropertySetService();   // 仅ReadAndWrite属性触发
        SyncPropertyGetService();   // ReadOnly/ReadAndWrite属性触发
        SyncPropertyPostEvent();    // 任意属性触发
    }

    /// <summary>
    /// 同步propertySet服务:thing.service.property.set
    /// （仅当存在ReadAndWrite属性时创建/保留）
    /// </summary>
    private void SyncPropertySetService()
    {
        var hasReadWriteProps = _properties.Any(p => p.AccessMode == AccessModes.ReadAndWrite);
        var setService = Services.FirstOrDefault(s => s.Method == ServiceMethodGenerator.BuiltInPropertySet);

        if (hasReadWriteProps)
        {
            if (setService == null)
            {
                var method = ServiceMethodGenerator.GetBuiltInPropertySetMethod();
                setService = new Service
                {
                    Identifier = "set", // 内置的固定值,
                    Name = "set",
                    Required = true,
                    CallType = ServiceCallTypes.Async, // 固定为异步
                    Method = method,
                    Desc = "属性设置",
                    OutputData = new List<OutputParam>() // 初始化空outputData
                };
                Services.Add(setService);
            }

            setService.InputData = Properties
                .Where(p => p.AccessMode == AccessModes.ReadAndWrite)
                .Select(p => new CommonInputParam
                {
                    Identifier = p.Identifier,
                    Name = p.Name,
                    DataType = p.DataType,
                    Required = p.Required
                })
                .Cast<IInputParam>()
                .ToList();

            // 补充propertySet的outputData（通常返回成功状态）
            setService.OutputData = new List<OutputParam>();

        }
        else if (setService != null)
        {
            Services.Remove(setService);
        }
    }

    /// <summary>
    /// 同步propertyGet服务:thing.service.property.get
    /// （仅当存在ReadOnly/ReadAndWrite属性时创建/保留）
    /// </summary>
    private void SyncPropertyGetService()
    {
        var hasReadableProps = Properties.Any(p =>
            p.AccessMode == AccessModes.ReadOnly ||
            p.AccessMode == AccessModes.ReadAndWrite);

        var getService = Services.FirstOrDefault(s => s.Method == ServiceMethodGenerator.BuiltInPropertyGet);

        if (hasReadableProps)
        {
            if (getService == null)
            {
                var method = ServiceMethodGenerator.GetBuiltInPropertyGetMethod();
                getService = new Service
                {
                    Identifier = "get", // 内置的固定值,
                    Name = "get",
                    Required = true,
                    CallType = ServiceCallTypes.Async,  // 固定为异步
                    Method = method,
                    Desc = "属性获取"
                };
                Services.Add(getService);
            }

            // 使用PropertyIdentifierParam（严格匹配Schema的anyOf规则）
            getService.InputData = Properties
                .Where(p => p.AccessMode == AccessModes.ReadOnly || p.AccessMode == AccessModes.ReadAndWrite)
                .Select(p => new PropertyIdentifierParam
                {
                    Identifier = p.Identifier // 仅设置Identifier，无其他字段
                })
                .Cast<IInputParam>()
                .ToList();

            getService.OutputData = Properties
                .Where(p => p.AccessMode == AccessModes.ReadOnly || p.AccessMode == AccessModes.ReadAndWrite)
                .Select(p => new OutputParam
                {
                    Identifier = p.Identifier,
                    Name = p.Name,
                    DataType = p.DataType
                }).ToList();
        }
        else if (getService != null)
        {
            Services.Remove(getService);
        }
    }

    /// <summary>
    /// 同步propertyPost事件: thing.service.property.post
    /// （仅当存在任何属性时创建/保留）
    /// </summary>
    private void SyncPropertyPostEvent()
    {
        var hasAnyProps = _properties.Any();
        var postEvent = Events.FirstOrDefault(e => e.Method == EventMethodGenerator.BuiltInPropertyPost);

        if (hasAnyProps)
        {
            if (postEvent == null)
            {
                // 动态创建propertyPost事件
                var (identifier, name, method, desc) = EventMethodGenerator.GetBuiltInPropertyPostMethod();
                postEvent = new Event
                {
                    Identifier = identifier,
                    Name = name,
                    Type = EventTypes.Info,
                    Required = true,
                    Desc = desc,
                    Method = method,
                };
                Events.Add(postEvent);
            }

            // 同步参数（所有属性）
            postEvent.OutputData = _properties
                .Select(p => new OutputParam
                {
                    Identifier = p.Identifier,
                    Name = p.Name,
                    DataType = p.DataType
                }).ToList();
        }
        else if (postEvent != null)
        {
            // 无属性时移除事件
            Events.Remove(postEvent);
        }
    }

    #endregion

    #region  Service

    /// <summary>
    /// 添加服务
    /// </summary>
    /// <param name="service">服务实例</param>
    /// <exception cref="ArgumentNullException">属性为空时抛出</exception>
    public void AddService(Service service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service), "服务实例不能为空");
        }

        Services.Add(service);
    }

    /// <summary>
    /// 移除服务
    /// </summary>
    /// <param name="identifier">服务标识符</param>
    /// <returns>是否移除成功</returns>
    /// <exception cref="ArgumentException">标识符为空时抛出</exception>
    public bool RemoveService(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("服务标识符不能为空或空白", nameof(identifier));
        }

        var service = Services.FirstOrDefault(p => p.Identifier == identifier);
        if (service == null)
        {
            return false;
        }

        Services.Remove(service);
        return true;
    }

    #endregion

    #region  Event
    /// <summary>
    /// 添加事件
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <exception cref="ArgumentNullException">属性为空时抛出</exception>
    public void AddEvent(Event @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event), "事件实例不能为空");
        }

        Events.Add(@event);
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    /// <param name="identifier">事件标识符</param>
    /// <returns>是否移除成功</returns>
    /// <exception cref="ArgumentException">标识符为空时抛出</exception>
    public bool RemoveEvent(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("事件标识符不能为空或空白", nameof(identifier));
        }

        var @event = Events.FirstOrDefault(p => p.Identifier == identifier);
        if (@event == null)
        {
            return false;
        }

        Events.Remove(@event);
        return true;
    }
    #endregion
}