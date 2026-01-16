using Artizan.IoT.ScriptDataCodec.JavaScript.Pooling;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Artizan.IoT.ScriptDataCodec.JavaScript.Tests;

/// <summary>
/// JavaScriptDataCodec & JavaScriptCodecPoolManager 单元测试
/// 测试覆盖：基础编解码、高并发池化、异常场景、实例复用
/// </summary>
public class JavaScriptCodecTests : IDisposable
{
    private const string TestScript = @"
        function decode(rawData) {
            return { Temp: (rawData[0] << 8 | rawData[1]) / 10, Humi: (rawData[2] << 8 | rawData[3]) / 10 };
        }
        function encode(protocolData) {
            var data = JSON.parse(protocolData);
            return [0xAA, data.ledOn ? 0x01 : 0x00, 0x55];
        }
        function emptyMethod() {
            return null;
        }
    ";

    private readonly string _testProductKey = "test_product_001";
    private readonly string _testProductPoolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey("test_product_001");
    private readonly JavaScriptCodecPoolManager _poolManager;

    public JavaScriptCodecTests()
    {
        // 获取池管理器实例
        _poolManager = JavaScriptCodecPoolManager.Instance;
        // 清理测试池（避免脏数据）
        _poolManager.RemovePool(_testProductPoolKey);
    }

    #region JavaScriptDataCodec 基础测试
    [Fact]
    public async Task DecodeAsync_ValidRawData_ReturnsSuccess()
    {
        // Arrange
        using var codec = new JavaScriptDataCodec(TestScript, new CodecLogger(NullLogger.Instance));
        var context = new ScriptExecutionContext
        {
            RawData = new byte[] { 0x01, 0x2C, 0x02, 0x58 }, // 30.0℃, 60.0%
            MethodName = "decode",
            ProductKey = _testProductKey,
            DeviceName = "test_device_001"
        };

        // Act
        var acquireResult = codec.TryAcquire();
        var decodeResult = await codec.DecodeAsync(context);

        // Assert
        acquireResult.ShouldBeTrue();
        decodeResult.Success.ShouldBeTrue();
        decodeResult.OutputProtocolData.ShouldNotBeNull();
        decodeResult.OutputProtocolData.ShouldContain("Temp");
        decodeResult.OutputProtocolData.ShouldContain("30");
        decodeResult.OutputProtocolData.ShouldContain("Humi");
        decodeResult.OutputProtocolData.ShouldContain("60");
    }

    [Fact]
    public async Task EncodeAsync_ValidProtocolData_ReturnsSuccess()
    {
        // Arrange
        using var codec = new JavaScriptDataCodec(TestScript, new CodecLogger(NullLogger.Instance));
        var context = new ScriptExecutionContext
        {
            ProtocolData = "{\"ledOn\": true}",
            MethodName = "encode",
            ProductKey = _testProductKey,
            DeviceName = "test_device_001"
        };

        // Act
        var acquireResult = codec.TryAcquire();
        var encodeResult = await codec.EncodeAsync(context);

        // Assert
        acquireResult.ShouldBeTrue();
        encodeResult.Success.ShouldBeTrue();
        encodeResult.OutputRawData.ShouldNotBeNull();
        encodeResult.OutputRawData.ShouldBe(new byte[] { 0xAA, 0x01, 0x55 });
    }

    [Fact]
    public async Task InvokeAsync_UnoccupiedInstance_ReturnsFailure()
    {
        // Arrange
        using var codec = new JavaScriptDataCodec(TestScript, new CodecLogger(NullLogger.Instance));
        var context = new ScriptExecutionContext
        {
            RawData = new byte[] { 0x01, 0x2C },
            MethodName = "decode"
        };

        // Act：未占用实例直接执行解码
        var decodeResult = await codec.DecodeAsync(context);

        // Assert
        decodeResult.Success.ShouldBeFalse();
        decodeResult.ErrorMessage.ShouldContain("未被正确占用");
    }

    [Fact]
    public async Task InvokeAsync_DisposedInstance_ReturnsFailure()
    {
        // Arrange
        var codec = new JavaScriptDataCodec(TestScript, new CodecLogger(NullLogger.Instance));
        codec.TryAcquire();
        codec.Dispose();

        var context = new ScriptExecutionContext
        {
            RawData = new byte[] { 0x01, 0x2C },
            MethodName = "decode"
        };

        // Act
        var decodeResult = await codec.DecodeAsync(context);

        // Assert
        decodeResult.Success.ShouldBeFalse();
        decodeResult.ErrorMessage.ShouldContain("已释放");
    }
    #endregion

    #region JavaScriptCodecPoolManager 池化测试
    [Fact]
    public void GetPool_SameProductKey_ReturnsSamePool()
    {
        // Arrange
        // Act
        var pool1 = _poolManager.GetPool(_testProductPoolKey, TestScript);
        var pool2 = _poolManager.GetPool(_testProductPoolKey, TestScript);

        // Assert
        pool1.ShouldBeSameAs(pool2);
    }

    [Fact]
    public void GetPool_DifferentProductKey_ReturnsDifferentPool()
    {
        // Arrange
        var productKey2 = "test_product_002";
        var poolKey2 = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey2);

        // Act
        var pool1 = _poolManager.GetPool(_testProductPoolKey, TestScript);
        var pool2 = _poolManager.GetPool(poolKey2, TestScript);

        // Assert
        pool1.ShouldNotBeSameAs(pool2);

        // Cleanup
        _poolManager.RemovePool(poolKey2);
    }

    [Fact]
    public async Task PooledCodec_ReuseInstance_Success()
    {
        // Arrange
        var pool = _poolManager.GetPool(_testProductPoolKey, TestScript, maxPoolSize: 2);
        var context = new ScriptExecutionContext
        {
            RawData = new byte[] { 0x01, 0x2C, 0x02, 0x58 },
            MethodName = "decode",
            ProductKey = _testProductKey
        };

        // Act：第一次获取实例
        var codec1 = pool.Get();
        var acquire1 = codec1.TryAcquire(); // 占用实例
        var result1 = await codec1.DecodeAsync(context);
        codec1.Release(); // 释放实例
        pool.Return(codec1); // 归还到池

        // 第二次获取实例（复用codec1）
        var codec2 = pool.Get();
        var acquire2 = codec2.TryAcquire(); // 占用实例
        var result2 = await codec2.DecodeAsync(context);
        codec2.Release(); // 释放实例
        pool.Return(codec2); // 归还到池

        // Assert
        acquire1.ShouldBeTrue(); // 第一次占用成功
        acquire2.ShouldBeTrue(); // 第二次占用成功
        result1.Success.ShouldBeTrue();
        result2.Success.ShouldBeTrue();
        result1.OutputProtocolData.ShouldBe(result2.OutputProtocolData); // 结果一致
        codec1.ShouldBeSameAs(codec2); // 验证实例复用
    }

    [Fact]
    public async Task HighConcurrency_PooledCodec_NoErrors()
    {
        // Arrange
        var pool = _poolManager.GetPool(_testProductPoolKey, TestScript, maxPoolSize: 5);
        var taskCount = 100000; // 模拟高并发：10万个并发任务
        var tasks = new List<Task<ScriptExecutionResult>>();

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var codec = pool.Get();
                try
                {
                    if (codec.TryAcquire())
                    {
                        var ctx = new ScriptExecutionContext
                        {
                            RawData = new byte[] { 0x01, (byte)(index % 255), 0x02, (byte)(index % 255) },
                            MethodName = "decode",
                            ProductKey = _testProductKey,
                            DeviceName = $"sensor_{index}"
                        };
                        return await codec.DecodeAsync(ctx);
                    }
                    return ScriptExecutionResult.Fail("Failed to acquire codec");
                }
                finally
                {
                    codec.Release();
                    pool.Return(codec);
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Count(r => r.Success).ShouldBe(taskCount); // 所有任务执行成功
        results.ShouldAllBe(r => r.Success);
    }
    #endregion

    public void Dispose()
    {
        // 清理测试池
        _poolManager.RemovePool(_testProductPoolKey);
        GC.SuppressFinalize(this);
    }
}
