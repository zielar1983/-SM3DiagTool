using SM3DiagTool.Models;
using Xunit;

namespace SM3DiagTool.Tests;

public class KnownDtcCodesTests
{
    [Fact]
    public void Descriptions_ContainsAtLeast200Codes()
    {
        Assert.True(KnownDtcCodes.Descriptions.Count >= 200,
            $"Oczekiwano 200+ kodow DTC, znaleziono {KnownDtcCodes.Descriptions.Count}");
    }

    [Fact]
    public void Descriptions_ContainsPowertrainCodes()
    {
        Assert.True(KnownDtcCodes.Descriptions.ContainsKey("P0100"));
        Assert.True(KnownDtcCodes.Descriptions.ContainsKey("P0171"));
        Assert.True(KnownDtcCodes.Descriptions.ContainsKey("P0300"));
        Assert.True(KnownDtcCodes.Descriptions.ContainsKey("P0420"));
    }

    [Fact]
    public void Descriptions_ContainsBodyCodes()
    {
        var hasBodyCode = KnownDtcCodes.Descriptions.Keys.Any(k => k.StartsWith("B"));
        Assert.True(hasBodyCode, "Brak kodow Body (B)");
    }

    [Fact]
    public void Descriptions_ContainsChassisCodes()
    {
        var hasChassis = KnownDtcCodes.Descriptions.Keys.Any(k => k.StartsWith("C"));
        Assert.True(hasChassis, "Brak kodow Chassis (C)");
    }

    [Fact]
    public void Descriptions_ContainsNetworkCodes()
    {
        var hasNetwork = KnownDtcCodes.Descriptions.Keys.Any(k => k.StartsWith("U"));
        Assert.True(hasNetwork, "Brak kodow Network (U)");
    }

    [Fact]
    public void Descriptions_AllCodesHaveDescriptions()
    {
        foreach (var kvp in KnownDtcCodes.Descriptions)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value),
                $"Kod {kvp.Key} ma pusty opis");
        }
    }

    [Fact]
    public void Descriptions_CodesFollowFormat()
    {
        foreach (var code in KnownDtcCodes.Descriptions.Keys)
        {
            Assert.Matches(@"^[PCBU]\d{4}$", code);
        }
    }
}
