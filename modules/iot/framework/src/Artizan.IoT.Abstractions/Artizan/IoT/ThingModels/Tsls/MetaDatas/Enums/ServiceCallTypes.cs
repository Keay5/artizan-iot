using System.Runtime.Serialization;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;

/// <summary>
/// 服务调用方式。
/// - 异步调用是指云端执行调用后直接返回，不会关心设备的回复消息，
/// - 如果服务为同步调用，云端会等待设备回复，否则会调用超时。
/// </summary>
public enum ServiceCallTypes
{
    [EnumMember(Value = "async")]
    Async,

    [EnumMember(Value = "sync")]
    Sync
}
