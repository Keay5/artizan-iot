using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 文件上传请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/file-upload-2
/// Method：thing.file.upload.request
/// </summary>
public class FileUploadRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.file.upload.request";

    [JsonPropertyName("params")]
    public FileUploadParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/file/upload/request";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Params.FileName))
        {
            return ValidateResult.Failed("文件名不能为空");
        }
        if (Params.FileSize <= 0)
        {
            return ValidateResult.Failed("文件大小必须大于0");
        }
        if (string.IsNullOrWhiteSpace(Params.FileType))
        {
            return ValidateResult.Failed("文件类型不能为空");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 文件上传参数
/// </summary>
public class FileUploadParams
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; } // 字节

    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = string.Empty; // txt/bin/jpg等

    [JsonPropertyName("store")]
    public string Store { get; set; } = "oss"; // 存储类型（oss）

    [JsonPropertyName("expireTime")]
    public long ExpireTime { get; set; } = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(); // 上传链接过期时间
}

/// <summary>
/// 文件上传响应（返回上传链接）
/// </summary>
public class FileUploadResponse : AlinkResponseBase<FileUploadResponseData>
{
}

/// <summary>
/// 文件上传响应数据
/// </summary>
public class FileUploadResponseData
{
    [JsonPropertyName("uploadUrl")]
    public string UploadUrl { get; set; } = string.Empty;

    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("expireTime")]
    public long ExpireTime { get; set; }

    [JsonPropertyName("accessKeyId")]
    public string AccessKeyId { get; set; } = string.Empty;

    [JsonPropertyName("accessKeySecret")]
    public string AccessKeySecret { get; set; } = string.Empty;

    [JsonPropertyName("securityToken")]
    public string SecurityToken { get; set; } = string.Empty;
}

/// <summary>
/// 文件上传完成通知请求
/// Method：thing.file.upload.complete
/// </summary>
public class FileUploadCompleteRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.file.upload.complete";

    [JsonPropertyName("params")]
    public FileUploadCompleteParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/file/upload/complete";
    }
}

/// <summary>
/// 上传完成参数
/// </summary>
public class FileUploadCompleteParams
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; } = string.Empty; // 文件MD5校验值
}


