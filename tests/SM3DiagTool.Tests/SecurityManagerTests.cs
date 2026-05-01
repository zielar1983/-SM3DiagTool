using SM3DiagTool.Protocols;
using Xunit;
using static SM3DiagTool.Protocols.SecurityManager;

namespace SM3DiagTool.Tests;

public class SecurityManagerTests
{
    [Fact]
    public void CalculateKey_SimpleXor_ReturnsCorrectKey()
    {
        var seed = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        byte level = 0x01;

        var key = SecurityManager.CalculateKey(seed, AlgorithmType.SimpleXor, level);

        Assert.NotNull(key);
        Assert.Equal(seed.Length, key.Length);
        for (int i = 0; i < seed.Length; i++)
        {
            byte expected = (byte)(seed[i] ^ (byte)(level * 0x5A + 0x37));
            Assert.Equal(expected, key[i]);
        }
    }

    [Fact]
    public void CalculateKey_SimpleXor_DifferentLevels_ProduceDifferentKeys()
    {
        var seed = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

        var key1 = SecurityManager.CalculateKey(seed, AlgorithmType.SimpleXor, 0x01);
        var key3 = SecurityManager.CalculateKey(seed, AlgorithmType.SimpleXor, 0x03);

        Assert.NotEqual(key1, key3);
    }

    [Fact]
    public void CalculateKey_Crc32Based_ReturnsNonEmptyKey()
    {
        var seed = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var key = SecurityManager.CalculateKey(seed, AlgorithmType.Crc32Based, 0x01);

        Assert.NotNull(key);
        Assert.Equal(4, key.Length);
    }

    [Fact]
    public void CalculateKey_Generic_ReturnsCorrectLength()
    {
        var seed = new byte[] { 0x55, 0xAA, 0xFF, 0x00, 0x11 };

        var key = SecurityManager.CalculateKey(seed, AlgorithmType.Generic, 0x01);

        Assert.NotNull(key);
        Assert.Equal(seed.Length, key.Length);
    }

    [Fact]
    public void CalculateKey_VAG_ProducesConsistentResults()
    {
        var seed = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        var key1 = SecurityManager.CalculateKey(seed, AlgorithmType.VAG, 0x01);
        var key2 = SecurityManager.CalculateKey(seed, AlgorithmType.VAG, 0x01);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void CalculateKey_BMW_ProducesConsistentResults()
    {
        var seed = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

        var key1 = SecurityManager.CalculateKey(seed, AlgorithmType.BMW, 0x01);
        var key2 = SecurityManager.CalculateKey(seed, AlgorithmType.BMW, 0x01);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void CalculateKey_Mercedes_ProducesConsistentResults()
    {
        var seed = new byte[] { 0x11, 0x22, 0x33, 0x44 };

        var key1 = SecurityManager.CalculateKey(seed, AlgorithmType.Mercedes, 0x01);
        var key2 = SecurityManager.CalculateKey(seed, AlgorithmType.Mercedes, 0x01);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void CalculateKey_EmptySeed_ReturnsEmptyKey()
    {
        var seed = Array.Empty<byte>();

        var key = SecurityManager.CalculateKey(seed, AlgorithmType.SimpleXor, 0x01);

        Assert.NotNull(key);
        Assert.Empty(key);
    }
}
