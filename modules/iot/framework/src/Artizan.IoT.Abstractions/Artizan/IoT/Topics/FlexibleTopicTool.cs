using System;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 适配任意开头的Topic工具类
/// </summary>
public static class FlexibleTopicTool
{
    /// <summary>
    /// 从任意开头的Topic中搜索并解析ProductKey和DeviceName
    /// 约定：Topic中必须包含连续的“/{ProductKey}/{DeviceName}/”片段。
    /// /sys/prod123/dev001/command
    /// /ota/prod456/dev002/update
    /// </summary>
    /// <returns>tuple(ProductKey, DeviceName)</returns>
    /// <exception cref="ArgumentException">格式非法或未找到身份片段时抛出</exception>
    public static (string productKey, string deviceName) ParseTopic(string targetTopic)
    {
        // 1. 基础格式校验（至少3个字符，避免空Topic）
        if (string.IsNullOrWhiteSpace(targetTopic) || targetTopic.Length < 3)
        {
            throw new ArgumentException($"Topic[{targetTopic}]格式非法：长度不足或为空");
        }

        // 2. 分割Topic为 segments（按“/”分割，保留空段用于定位）
        // 示例："/sys/prod123/dev001/command" → ["", "sys", "prod123", "dev001", "command"]
        var segments = targetTopic.Split('/');

        // 3. 搜索连续的“非空段+非空段”（即可能的 ProductKey+DeviceName）
        List<(string Pk, string Dn, int StartIndex)> candidateList = new();
        for (int i = 0; i < segments.Length - 1; i++)
        {
            // 跳过空段，寻找连续两个非空段（ProductKey不能是空，DeviceName也不能是空）
            if (string.IsNullOrWhiteSpace(segments[i]) || string.IsNullOrWhiteSpace(segments[i + 1]))
                continue;

            // 过滤非法字符（排除MQTT保留字符 $ # +）
            var candidatePk = segments[i];
            var candidateDn = segments[i + 1];
            if (candidatePk.Contains("$") || candidatePk.Contains("#") || candidatePk.Contains("+"))
            {
                continue;
            }

            if (candidateDn.Contains("$") || candidateDn.Contains("#") || candidateDn.Contains("+"))
            {
                continue;
            }

            // 加入候选列表（记录起始索引，用于后续去重）
            candidateList.Add((candidatePk, candidateDn, i));
        }

        // 4. 处理候选结果（必须有且仅有一个有效身份片段）
        if (candidateList.Count == 0)
        {
            throw new ArgumentException($"Topic[{targetTopic}]未找到合法的设备身份片段（需包含“/{{productKey}}/{{deviceName}}/”）");
        }

        if (candidateList.Count > 1)
        {
            // 若存在多个候选，判断是否为重复片段（如“/prod123/dev001/prod123/dev001”），重复则取第一个
            var first = candidateList[0];
            bool allDuplicate = candidateList.All(c => c.Pk == first.Pk && c.Dn == first.Dn);
            if (!allDuplicate)
            {
                throw new ArgumentException($"Topic[{targetTopic}]包含多个不同的设备身份片段，存在歧义");
            }
        }

        // 5. 返回唯一有效身份片段
        var validCandidate = candidateList[0];
        return (validCandidate.Pk, validCandidate.Dn);
    }

    /// <summary>
    /// 校验设备与任意开头的Topic是否绑定
    /// /sys/prod123/dev001/command  prod123/dev001 通过， 
    /// /ota/prod456/dev002/update   prod456/dev002 通过，但 prod123/dev001，不通过：DeviceName 不匹配（dev002≠dev001）
    /// ------------------------------------
    /// 使用示例：
    ///     if (!FlexibleTopicTool.IsDeviceBoundToTopic(device.ProductKey, device.DeviceName, targetTopic))
    ///    {
    ///        var errorMsg = $"设备[{device.DeviceName}]尝试向非自身Topic[{targetTopic}]发布消息，已拒绝";
    ///        _logger.Warn(errorMsg);
    ///        throw new UnauthorizedAccessException(errorMsg);
    ///    }
    /// </summary>
    public static bool IsDeviceBoundToTopic(string deviceProductKey, string deviceName, string targetTopic)
    {
        try
        {
            var (parsedPk, parsedDn) = ParseTopic(targetTopic);

            // 严格匹配：设备自身的ProductKey和DeviceName必须与解析结果一致
            return parsedPk.Equals(deviceProductKey, StringComparison.OrdinalIgnoreCase)
                   && parsedDn.Equals(deviceName, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false; // 格式非法或无身份片段，视为不绑定
        }
    }
}