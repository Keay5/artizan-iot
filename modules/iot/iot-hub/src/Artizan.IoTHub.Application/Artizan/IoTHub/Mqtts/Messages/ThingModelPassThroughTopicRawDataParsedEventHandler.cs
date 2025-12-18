using Artizan.IoT.Alinks;
using Artizan.IoT.Alinks.DataObjects;
using Artizan.IoT.Concurrents;
using Artizan.IoTHub.Mqtts.Servers;
using Artizan.IoTHub.Products.MessageParsings;
using Artizan.IoTHub.Products.MessageParsings.Etos;
using Artizan.IoTHub.Topics;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace Artizan.IoTHub.Mqtts.Messages;

public class ThingModelPassThroughTopicRawDataParsedEventHandler :
    ConcurrentPartitionedMessageDispatcher<ThingModelPassThroughTopicRawDataParsedEto>,
    IDistributedEventHandler<ThingModelPassThroughTopicRawDataParsedEto>,
    ISingletonDependency
{
    protected IMqttInternalService MqttInternalService { get; }
    
    protected TopicMessageParsingManager TopicMessageParsingManager { get; }

    public ThingModelPassThroughTopicRawDataParsedEventHandler(
        ILogger<ThingModelPassThroughTopicRawDataParsedEventHandler> logger,
        IMqttInternalService mqttInternalService,
        TopicMessageParsingManager topicMessageParsingManager)
        : base(logger, 10000)
    {
        MqttInternalService = mqttInternalService;
        TopicMessageParsingManager = topicMessageParsingManager;
    }

    public async Task HandleEventAsync(ThingModelPassThroughTopicRawDataParsedEto eventData)
    {
        await EnqueueMessageAsync(eventData);
    }

    protected override string GetPartitionKey(ThingModelPassThroughTopicRawDataParsedEto eventData)
    {
        return $"{eventData.ProductKey}_{eventData.DeviceName}";
    }

    /// <summary>
    /// https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services?spm=a2c4g.11186623.0.0.4d543778oQdoCU#section-g4j-5zg-12b
    /// </summary>
    /// <returns></returns>
    protected override async Task ProcessMessageAsync(ThingModelPassThroughTopicRawDataParsedEto eventData, int consumerId, CancellationToken cancellationToken)
    {
        if (TopicSpeciesConsts.ThingModelCommunication.PropertyReport.Post == eventData.Topic)
        {
            var request = AlinkSerializer.DeserializeObject<PropertyPostRequest>(eventData.AlinkJsonData);
            var response = new PropertyPostResponse()
            {
                Method = request!.Method,
                Id = eventData.MessageId,
                Code = 200,
                Message = "success"
            };

            if (request == null)
            {
                response.Code = 201; //TODO: 定义错误码,
                response.Message = "无法解析请求参数";
            }

            //TODO: 校验数据格式，这里只是简单回复成功，没有做任何校验和处理

            var payload = AlinkSerializer.SerializeObject(response);
            var messageBuilder = new MqttApplicationMessageBuilder()
                .WithPayload(payload)
                .WithTopic(TopicSpeciesConsts.ThingModelCommunication.PropertyReport.PostReply)
                .Build();

            await MqttInternalService.MqttServer.PublishByBrokerAsync(
                messageBuilder

            );

            // TODO: 这里还可以把上报的数据进行处理，比如存储到数据库，或者触发其他业务逻辑
        }
        else if (TopicSpeciesConsts.ThingModelCommunication.PassThrough.UpRaw == eventData.Topic)
        {
            // 示例：https://help.aliyun.com/zh/iot/user-guide/sample-javascript-script?spm=a2c4g.11186623.0.0.c2eb390bDScN5D#concept-2371163

            var request = AlinkSerializer.DeserializeObject<PropertyPostRequest>(eventData.AlinkJsonData);
            var response = new PropertyPostResponse()
            {
                Method = request!.Method,
                Id = eventData.MessageId,
                Code = 200,
                Message = "success"
            };

            if (request == null)
            {
                response.Code = 201; //TODO: 定义错误码,
                response.Message = "无法解析请求参数";
            }

            // TODO: 校验数据格式，这里只是简单回复成功，没有做任何校验和处理

            var payload = await TopicMessageParsingManager.ConvertProtocolDataToRawDataAsync(
                eventData.ProductKey,
                AlinkSerializer.SerializeObject(response), 
                cancellationToken);

            var message = new MqttApplicationMessageBuilder()
                .WithPayload(payload)
                .WithTopic(TopicSpeciesConsts.ThingModelCommunication.PassThrough.UpRawReply)
                .Build();
            await MqttInternalService.MqttServer.PublishByBrokerAsync(message);

            // TODO: 这里还可以把上报的数据进行处理，比如存储到数据库，或者触发其他业务逻辑
        }
    }
}
