using Artizan.IoT;
using Artizan.IoT.Mqtt;
using Artizan.IoT.Thing;
using Artizan.IoTHub.Devices.Caches;
using Artizan.IoTHub.Products.Caches;
using Microsoft.Extensions.DependencyInjection;
using System;
using Volo.Abp.AutoMapper;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(IoTHubDomainSharedModule),
    typeof(AbpAutoMapperModule),
    typeof(IoTAbstractionsModule),
    typeof(IoTThingModule),
    typeof(IoTMqttModule)
)]
public class IoTHubDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<IoTHubDomainModule>();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<IoTHubDomainModule>(validate: true);
        });

        Configure<ProductCacheOptions>(options =>
        {
            options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
        });
        //Configure<ProductTslCacheOptions>(options =>
        //{
        //    options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
        //});
        //Configure<ProductMessageParserCacheOptions>(options =>
        //{
        //    options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
        //});
        Configure<DeviceCacheOptions>(options =>
        {
            options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
        });
    }
}
