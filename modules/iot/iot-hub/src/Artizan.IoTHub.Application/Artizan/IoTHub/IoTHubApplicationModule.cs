using Artizan.IoT.Mqtt;
using Artizan.IoT.TimeSeries.InfluxDB;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(IoTHubDomainModule),
    typeof(IoTHubApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(IoTMqttModule),
    typeof(IoTTimeSeriesInfluxDB2Module)
)]
public class IoTHubApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<IoTHubApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<IoTHubApplicationModule>(validate: true);
        });
    }
}
