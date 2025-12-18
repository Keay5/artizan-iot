using Artizan.IoTHub.Mqtts.Servers;
using MQTTnet;
using MQTTnet.Server;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Mqtts.Services;

public abstract class MqttServiceBase : IMqttService
{
    protected const string AuthParamsSessionItemKey = "AuthParams";

    public MqttServer MqttServer { get; private set; }

    public MqttServiceBase()
    {
    }

    public virtual void ConfigureMqttServer(MqttServer mqttServer)
    {
        MqttServer = mqttServer;
    }

    protected virtual async Task<MqttClientStatus?> GetClientStatusAsync(string clientId)
    {
        var allClientStatuses = await MqttServer.GetClientsAsync();
        return allClientStatuses.FirstOrDefault(cs => cs.Id == clientId);
    }

    protected virtual string GetClientIdFromPayload(MqttApplicationMessage message)
    {
        var payload = Encoding.UTF8.GetString(message.PayloadSegment);
        // TODO: for JSON type data transfer get clientId from json payload
        return payload;
    }

    protected virtual async Task DisconnectClientAsync(string clientId)
    {
        var clientStatus = await GetClientStatusAsync(clientId);
        if (clientStatus != null)
        {
            await clientStatus.DisconnectAsync();
        }
    }


}
