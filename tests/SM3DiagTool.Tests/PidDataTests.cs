using SM3DiagTool.Models;
using Xunit;

namespace SM3DiagTool.Tests;

public class PidDataTests
{
    [Fact]
    public void StandardPids_ContainsAtLeast40Pids()
    {
        Assert.True(ObdPids.StandardPids.Count >= 40,
            $"Oczekiwano 40+ PID-ow, znaleziono {ObdPids.StandardPids.Count}");
    }

    [Fact]
    public void StandardPids_ContainsRpm()
    {
        Assert.True(ObdPids.StandardPids.ContainsKey(0x0C));
        var rpm = ObdPids.StandardPids[0x0C];
        Assert.Equal("RPM", rpm.Unit);
        Assert.NotNull(rpm.Parser);
    }

    [Fact]
    public void StandardPids_RpmParser_CalculatesCorrectly()
    {
        var pid = ObdPids.StandardPids[0x0C];
        var result = pid.Parser!(new byte[] { 0x0C, 0x80 }); // 3200 / 4 = 800 RPM
        Assert.Equal(800, result);
    }

    [Fact]
    public void StandardPids_SpeedParser_CalculatesCorrectly()
    {
        var pid = ObdPids.StandardPids[0x0D];
        var result = pid.Parser!(new byte[] { 120 }); // 120 km/h
        Assert.Equal(120, result);
    }

    [Fact]
    public void StandardPids_CoolantTempParser_CalculatesCorrectly()
    {
        var pid = ObdPids.StandardPids[0x05];
        var result = pid.Parser!(new byte[] { 130 }); // 130 - 40 = 90°C
        Assert.Equal(90, result);
    }

    [Fact]
    public void StandardPids_ThrottleParser_CalculatesCorrectly()
    {
        var pid = ObdPids.StandardPids[0x11];
        var result = pid.Parser!(new byte[] { 255 }); // 255 * 100 / 255 = 100%
        Assert.Equal(100, result);
    }

    [Fact]
    public void StandardPids_AllHaveParsers()
    {
        foreach (var kvp in ObdPids.StandardPids)
        {
            Assert.NotNull(kvp.Value.Parser);
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value.Name),
                $"PID 0x{kvp.Key:X2} ma pusta nazwe");
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value.Unit) && kvp.Value.Unit != "",
                $"PID 0x{kvp.Key:X2} nie ma jednostki (null)");
        }
    }

    [Fact]
    public void StandardPids_ContainsFuelPressure()
    {
        Assert.True(ObdPids.StandardPids.ContainsKey(0x0A));
    }

    [Fact]
    public void StandardPids_ContainsLambdaSensors()
    {
        Assert.True(ObdPids.StandardPids.ContainsKey(0x14));
        Assert.True(ObdPids.StandardPids.ContainsKey(0x15));
    }

    [Fact]
    public void StandardPids_ContainsFuelConsumption()
    {
        Assert.True(ObdPids.StandardPids.ContainsKey(0x5E));
        Assert.Equal("L/h", ObdPids.StandardPids[0x5E].Unit);
    }
}
