using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Products;
using Artizan.IoTHub.Products.Modules;
using Artizan.IoTHub.Products.ProductMoudles;
using Artizan.IoTHub.Products.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Artizan.IoTHub.EntityFrameworkCore;

public static class IoTHubDbContextModelCreatingExtensions
{
    public static void ConfigureIoTHub(
        this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        /* Configure all entities here. Example:

        builder.Entity<Question>(b =>
        {
            //Configure table & schema name
            b.ToTable(IoTHubDbProperties.DbTablePrefix + "Questions", IoTHubDbProperties.DbSchema);

            b.ConfigureByConvention();

            //Properties
            b.Property(q => q.Title).IsRequired().HasMaxLength(QuestionConsts.MaxTitleLength);

            //Relations
            b.HasMany(question => question.Tags).WithOne().HasForeignKey(qt => qt.QuestionId);

            //Indexes
            b.HasIndex(q => q.CreationTime);
        });
        */

        builder.Entity<Product>(b =>
        {
            b.ToTable(IoTHubDbProperties.DbTablePrefix + "Products", IoTHubDbProperties.DbSchema);
           
            b.ConfigureByConvention();

            b.Property(x => x.ProductKey).IsRequired().HasMaxLength(ProductConsts.MaxProductKeyLength).HasColumnName(nameof(Product.ProductKey));
            b.Property(x => x.ProductSecret).IsRequired().HasMaxLength(ProductConsts.MaxProductSecretLength).HasColumnName(nameof(Product.ProductSecret));
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(ProductConsts.MaxProductNameLength).HasColumnName(nameof(Product.ProductName));
            b.Property(x => x.Category).IsRequired().HasColumnName(nameof(Product.Category));
            b.Property(x => x.CategoryName).IsRequired().HasMaxLength(ProductConsts.MaxCategoryNameLength).HasColumnName(nameof(Product.CategoryName));
            b.Property(x => x.NodeType).IsRequired().HasColumnName(nameof(Product.NodeType));
            b.Property(x => x.NetworkingMode).IsRequired(false).HasColumnName(nameof(Product.NetworkingMode));
            b.Property(x => x.AccessGatewayProtocol).IsRequired(false).HasColumnName(nameof(Product.AccessGatewayProtocol));
            b.Property(x => x.DataFormat).IsRequired().HasColumnName(nameof(Product.DataFormat));
            b.Property(x => x.AuthenticationMode).IsRequired().HasColumnName(nameof(Product.AuthenticationMode));
            b.Property(x => x.IsUsingPrivateCACertificate).IsRequired().HasColumnName(nameof(Product.IsUsingPrivateCACertificate));
            b.Property(x => x.IsEnableDynamicRegistration).IsRequired().HasColumnName(nameof(Product.IsEnableDynamicRegistration));
            b.Property(x => x.ProductStatus).IsRequired().HasColumnName(nameof(Product.ProductStatus));
            b.Property(x => x.Description).IsRequired(false).HasMaxLength(ProductConsts.MaxDescriptionLength).HasColumnName(nameof(Product.Description));

            b.ApplyObjectExtensionMappings();

            b.HasIndex(x => new {
                x.ProductKey,
                x.ProductName
            });
        });

        builder.Entity<ProductModule>(b =>
        {
            b.ToTable(IoTHubDbProperties.DbTablePrefix + "ProductModules", IoTHubDbProperties.DbSchema);
           
            b.ConfigureByConvention();

            b.Property(x => x.ProductId).IsRequired().HasColumnName(nameof(ProductModule.ProductId));
            b.Property(x => x.Name).IsRequired().HasMaxLength(ProductModuleConsts.MaxNameLength).HasColumnName(nameof(ProductModule.Name));
            b.Property(x => x.Identifier).IsRequired().HasMaxLength(ProductModuleConsts.MaxIdentifierLength).HasColumnName(nameof(ProductModule.Identifier));
            b.Property(x => x.IsDefault).IsRequired().HasColumnName(nameof(ProductModule.IsDefault));
            b.Property(x => x.Status).IsRequired().HasColumnName(nameof(ProductModule.Status));
            b.Property(x => x.Version).IsRequired(false).HasMaxLength(ProductModuleConsts.MaxVersionLength).HasColumnName(nameof(ProductModule.Version));
            b.Property(x => x.IsCurrentVersion).IsRequired().HasColumnName(nameof(ProductModule.IsCurrentVersion));
            b.Property(x => x.ProductModuleTsl).IsRequired().HasColumnName(nameof(ProductModule.ProductModuleTsl));
            b.Property(x => x.Description).IsRequired(false).HasMaxLength(ProductModuleConsts.MaxDescriptionLength).HasColumnName(nameof(ProductModule.Description));

            b.ApplyObjectExtensionMappings();

            // Relationships
            b.HasOne<Product>().WithMany().IsRequired().HasForeignKey(p => p.ProductId);

            b.HasIndex(x => new {
                x.ProductId,
                x.Name,
                x.Identifier
            });
        });

        builder.Entity<Device>(b =>
        {
            b.ToTable(IoTHubDbProperties.DbTablePrefix + "Devices", IoTHubDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(x => x.ProductId).HasColumnName(nameof(Device.ProductId));
            b.Property(x => x.DeviceName).IsRequired().HasMaxLength(DeviceConsts.MaxDeviceNameLength).HasColumnName(nameof(Device.DeviceName));
            b.Property(x => x.DeviceSecret).IsRequired().HasMaxLength(DeviceConsts.MaxDeviceSecretLength).HasColumnName(nameof(Device.DeviceSecret));
            b.Property(x => x.RemarkName).IsRequired(false).HasMaxLength(DeviceConsts.MaxDeviceRemarkNameLength).HasColumnName(nameof(Device.RemarkName));
            b.Property(x => x.IsActive).IsRequired().HasColumnName(nameof(Device.IsActive));
            b.Property(x => x.IsEnable).IsRequired().HasColumnName(nameof(Device.IsEnable));
            b.Property(x => x.Status).IsRequired().HasColumnName(nameof(Device.Status));

            b.Property(x => x.Description).IsRequired(false).HasMaxLength(DeviceConsts.MaxDescriptionLength).HasColumnName(nameof(Device.Description));

            b.ApplyObjectExtensionMappings();

            // Relationships
            b.HasOne<Product>().WithMany().IsRequired().HasForeignKey(p => p.ProductId);

            b.HasIndex(x => new {
                x.ProductId,
                x.DeviceName
            });
        });

        #region 一对多样板
        //builder.Entity<DeviceMqtt>(b =>
        //{
        //    b.ToTable(IoTHubDbProperties.DbTablePrefix + "DeviceMqtts", IoTHubDbProperties.DbSchema);

        //    b.ConfigureByConvention();

        //    b.Property(x => x.DeviceId).HasColumnName(nameof(DeviceMqtt.DeviceId));
        //    b.Property(x => x.ClientId).IsRequired().HasMaxLength(DeviceMqttConsts.MaxClientIdLength).HasColumnName(nameof(DeviceMqtt.ClientId));
        //    b.Property(x => x.UserName).IsRequired().HasMaxLength(DeviceMqttConsts.MaxUserNameLength).HasColumnName(nameof(DeviceMqtt.UserName));
        //    b.Property(x => x.Password).IsRequired().HasMaxLength(DeviceMqttConsts.MaxPasswordLength).HasColumnName(nameof(DeviceMqtt.Password));

        //    b.ApplyObjectExtensionMappings();

        //    // Relationships
        //    b.HasOne<Device>() // Device 如有导航属性，改为 HasOne(dm => dm.Device)
        //      .WithOne()       // Device 无反向导航属性，直接用无参 WithOne()， 有导航属性：改用.WithOne(d => d.DeviceMqtt)
        //      .IsRequired()    // 关系必需（DeviceMqtt 必须关联一个 Device）
        //      .HasForeignKey<Device>(p => p.Id)  // 明确「依赖表是 DeviceMqtt」
        //      .OnDelete(DeleteBehavior.Cascade); // Cascade：级联删除

        //    b.HasIndex(x => x.DeviceId).IsUnique();
        //}); 
        #endregion
    }
}
