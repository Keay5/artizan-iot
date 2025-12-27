using Artizan.IoT;
using Artizan.IoT.Products.Caches;
using Artizan.IoTHub.Devices.Caches;
using Microsoft.Extensions.DependencyInjection;
using System;
using Volo.Abp.AutoMapper;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.Abp.Security;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpSecurityModule),
    typeof(IoTHubDomainSharedModule),
    typeof(IoTModule)
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
        Configure<ProductTslCacheOptions>(options =>
        {
            options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
        });
        Configure<ProductMessageParserCacheOptions>(options =>
        {
            options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
        });
        Configure<DeviceCacheOptions>(options =>
        {
            options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
        });
    }
}
