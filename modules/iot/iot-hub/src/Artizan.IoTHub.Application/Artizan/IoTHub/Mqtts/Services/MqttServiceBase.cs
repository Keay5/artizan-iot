using MQTTnet;
using MQTTnet.Server;
using System.Linq;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Mqtts.Services;

public abstract class MqttServiceBase : IMqttService
{
    protected const string signParamsSessionItemKey = "MqttSignParams";

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

    protected virtual async Task DisconnectClientAsync(string clientId)
    {
        var clientStatus = await GetClientStatusAsync(clientId);
        if (clientStatus != null)
        {
            await clientStatus.DisconnectAsync();
        }
    }

}
