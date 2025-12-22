using Artizan.IoT.ThingModels.Tsls.MetaDatas;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Properties;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;
using System.Collections.Generic;

namespace Artizan.IoTHub.Things;

public interface IThing
{
    /// <summary>
    /// 检查物模型是否初始化完成
    /// </summary>
    /// <returns>初始化状态</returns>
    bool IsThingInited();

    /// <summary>
    /// 获取所有属性定义
    /// </summary>
    /// <returns>属性列表</returns>
    IList<Property> GetProperties();

    /// <summary>
    /// 获取所有服务定义
    /// </summary>
    /// <returns>服务列表</returns>
    IList<Service> GetServices();

    /// <summary>
    /// 获取所有事件定义
    /// </summary>
    /// <returns>事件列表</returns>
    IList<Event> GetEvents();

    /// <summary>
    /// 获取指定属性的值
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <returns>属性值包装器</returns>
    //ValueWrapper GetPropertyValue(string propertyName);

    ///// <summary>
    ///// 获取所有属性的键值对
    ///// </summary>
    ///// <returns>属性名称-值包装器字典</returns>
    //IDictionary<string, ValueWrapper> GetAllPropertyValue();

    ///// <summary>
    ///// 上报属性值
    ///// </summary>
    ///// <param name="propertyValues">属性键值对</param>
    ///// <param name="listener">发布资源回调监听器</param>
    //void ThingPropertyPost(IDictionary<string, ValueWrapper> propertyValues, IPublishResourceListener listener);

    ///// <summary>
    ///// 设置服务处理器
    ///// </summary>
    ///// <param name="serviceName">服务名称</param>
    ///// <param name="handler">资源请求处理器</param>
    //void SetServiceHandler(string serviceName, ITResRequestHandler handler);

    ///// <summary>
    ///// 上报事件
    ///// </summary>
    ///// <param name="eventName">事件名称</param>
    ///// <param name="outputParams">输出参数</param>
    ///// <param name="listener">发布资源回调监听器</param>
    //void ThingEventPost(string eventName, OutputParam outputParams, IPublishResourceListener listener);

    ///// <summary>
    ///// 注册服务处理器
    ///// </summary>
    ///// <param name="serviceName">服务名称</param>
    ///// <param name="handler">资源请求处理器</param>
    //void ThingServiceRegister(string serviceName, ITResRequestHandler handler);

    ///// <summary>
    ///// 上报原始属性数据（字节数组）
    ///// </summary>
    ///// <param name="rawData">原始字节数据</param>
    ///// <param name="listener">设备原始数据回调监听器</param>
    //void ThingRawPropertiesPost(byte[] rawData, IDevRawDataListener listener);

    ///// <summary>
    ///// 设置原始属性变更监听器
    ///// </summary>
    ///// <param name="enable">是否启用监听器</param>
    ///// <param name="handler">原始数据请求处理器</param>
    //void SetRawPropertyChangeListener(bool enable, ITRawDataRequestHandler handler);

    ///// <summary>
    ///// 取消订阅指定主题
    ///// </summary>
    ///// <param name="topic">订阅主题</param>
    ///// <param name="handler">资源请求处理器</param>
    //void ThingUnubscribe(string topic, ITResRequestHandler handler);

    ///// <summary>
    ///// 反初始化物模型
    ///// </summary>
    //void Uninit();
}
