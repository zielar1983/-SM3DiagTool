namespace SM3DiagTool.Models;

public class PidData
{
    public byte Pid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Value { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public string FormattedValue => $"{Value:F1} {Unit}";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public Func<byte[], double>? Parser { get; set; }

    public override string ToString() => $"{Name}: {FormattedValue}";
}

public static class ObdPids
{
    public static readonly Dictionary<byte, PidData> StandardPids = new()
    {
        [0x04] = new PidData { Pid = 0x04, Name = "Obciazenie silnika", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x05] = new PidData { Pid = 0x05, Name = "Temp. plynu chlodniczego", Unit = "\u00b0C", MinValue = -40, MaxValue = 215,
            Parser = data => data[0] - 40.0 },
        [0x06] = new PidData { Pid = 0x06, Name = "Korekcja krotkoterm. B1", Unit = "%", MinValue = -100, MaxValue = 99.2,
            Parser = data => (data[0] - 128) * 100.0 / 128 },
        [0x07] = new PidData { Pid = 0x07, Name = "Korekcja dlugoterm. B1", Unit = "%", MinValue = -100, MaxValue = 99.2,
            Parser = data => (data[0] - 128) * 100.0 / 128 },
        [0x0B] = new PidData { Pid = 0x0B, Name = "Cisnienie w kolektorze", Unit = "kPa", MinValue = 0, MaxValue = 255,
            Parser = data => data[0] },
        [0x0C] = new PidData { Pid = 0x0C, Name = "Obroty silnika", Unit = "RPM", MinValue = 0, MaxValue = 16383.75,
            Parser = data => (data[0] * 256 + data[1]) / 4.0 },
        [0x0D] = new PidData { Pid = 0x0D, Name = "Predkosc pojazdu", Unit = "km/h", MinValue = 0, MaxValue = 255,
            Parser = data => data[0] },
        [0x0E] = new PidData { Pid = 0x0E, Name = "Wyprzedzenie zaplonu", Unit = "\u00b0", MinValue = -64, MaxValue = 63.5,
            Parser = data => data[0] / 2.0 - 64 },
        [0x0F] = new PidData { Pid = 0x0F, Name = "Temp. powietrza dolotowego", Unit = "\u00b0C", MinValue = -40, MaxValue = 215,
            Parser = data => data[0] - 40.0 },
        [0x10] = new PidData { Pid = 0x10, Name = "Przeplyw powietrza MAF", Unit = "g/s", MinValue = 0, MaxValue = 655.35,
            Parser = data => (data[0] * 256 + data[1]) / 100.0 },
        [0x11] = new PidData { Pid = 0x11, Name = "Pozycja przepustnicy", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x1C] = new PidData { Pid = 0x1C, Name = "Standard OBD", Unit = "", MinValue = 0, MaxValue = 255,
            Parser = data => data[0] },
        [0x1F] = new PidData { Pid = 0x1F, Name = "Czas pracy silnika", Unit = "s", MinValue = 0, MaxValue = 65535,
            Parser = data => data[0] * 256 + data[1] },
        [0x21] = new PidData { Pid = 0x21, Name = "Dystans z MIL", Unit = "km", MinValue = 0, MaxValue = 65535,
            Parser = data => data[0] * 256 + data[1] },
        [0x2F] = new PidData { Pid = 0x2F, Name = "Poziom paliwa", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x33] = new PidData { Pid = 0x33, Name = "Cisnienie barometryczne", Unit = "kPa", MinValue = 0, MaxValue = 255,
            Parser = data => data[0] },
        [0x42] = new PidData { Pid = 0x42, Name = "Napiecie modulu", Unit = "V", MinValue = 0, MaxValue = 65.535,
            Parser = data => (data[0] * 256 + data[1]) / 1000.0 },
        [0x46] = new PidData { Pid = 0x46, Name = "Temp. otoczenia", Unit = "\u00b0C", MinValue = -40, MaxValue = 215,
            Parser = data => data[0] - 40.0 },
        [0x5C] = new PidData { Pid = 0x5C, Name = "Temp. oleju", Unit = "\u00b0C", MinValue = -40, MaxValue = 210,
            Parser = data => data[0] - 40.0 },
        [0x03] = new PidData { Pid = 0x03, Name = "Stan ukladu paliwowego", Unit = "", MinValue = 0, MaxValue = 255,
            Parser = data => data[0] },
        [0x0A] = new PidData { Pid = 0x0A, Name = "Cisnienie paliwa", Unit = "kPa", MinValue = 0, MaxValue = 765,
            Parser = data => data[0] * 3 },
        [0x14] = new PidData { Pid = 0x14, Name = "Sonda lambda B1S1 napiecie", Unit = "V", MinValue = 0, MaxValue = 1.275,
            Parser = data => data[0] / 200.0 },
        [0x15] = new PidData { Pid = 0x15, Name = "Sonda lambda B1S2 napiecie", Unit = "V", MinValue = 0, MaxValue = 1.275,
            Parser = data => data[0] / 200.0 },
        [0x22] = new PidData { Pid = 0x22, Name = "Cisnienie szyny paliwowej (wzg.)", Unit = "kPa", MinValue = 0, MaxValue = 5177.265,
            Parser = data => (data[0] * 256 + data[1]) * 0.079 },
        [0x23] = new PidData { Pid = 0x23, Name = "Cisnienie szyny paliwowej (bezwzg.)", Unit = "kPa", MinValue = 0, MaxValue = 655350,
            Parser = data => (data[0] * 256 + data[1]) * 10 },
        [0x24] = new PidData { Pid = 0x24, Name = "Lambda B1S1 equiv. ratio", Unit = "", MinValue = 0, MaxValue = 2,
            Parser = data => (data[0] * 256 + data[1]) / 32768.0 },
        [0x2C] = new PidData { Pid = 0x2C, Name = "Sterowane EGR", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x2D] = new PidData { Pid = 0x2D, Name = "Blad EGR", Unit = "%", MinValue = -100, MaxValue = 99.2,
            Parser = data => (data[0] - 128) * 100.0 / 128 },
        [0x2E] = new PidData { Pid = 0x2E, Name = "Sterowanie odparowaniem", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x30] = new PidData { Pid = 0x30, Name = "Liczba rozgrzewek od skasow. DTC", Unit = "", MinValue = 0, MaxValue = 255,
            Parser = data => data[0] },
        [0x31] = new PidData { Pid = 0x31, Name = "Dystans od skasowania DTC", Unit = "km", MinValue = 0, MaxValue = 65535,
            Parser = data => data[0] * 256 + data[1] },
        [0x34] = new PidData { Pid = 0x34, Name = "Sonda lambda B1S1 prąd", Unit = "mA", MinValue = -128, MaxValue = 128,
            Parser = data => ((data[0] * 256 + data[1]) / 32768.0 - 1) * 128 },
        [0x43] = new PidData { Pid = 0x43, Name = "Bezwzgledne obciazenie", Unit = "%", MinValue = 0, MaxValue = 25700,
            Parser = data => (data[0] * 256 + data[1]) * 100.0 / 255 },
        [0x44] = new PidData { Pid = 0x44, Name = "Stosunek paliwowy (lambda)", Unit = "", MinValue = 0, MaxValue = 2,
            Parser = data => (data[0] * 256 + data[1]) / 32768.0 },
        [0x45] = new PidData { Pid = 0x45, Name = "Wzgledna pozycja przepust.", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x47] = new PidData { Pid = 0x47, Name = "Pozycja przepustnicy B", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x49] = new PidData { Pid = 0x49, Name = "Pozycja pedalu gazu D", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x4A] = new PidData { Pid = 0x4A, Name = "Pozycja pedalu gazu E", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x4C] = new PidData { Pid = 0x4C, Name = "Wymagany moment obrotowy", Unit = "%", MinValue = -125, MaxValue = 130,
            Parser = data => data[0] - 125.0 },
        [0x4D] = new PidData { Pid = 0x4D, Name = "Czas pracy z MIL", Unit = "min", MinValue = 0, MaxValue = 65535,
            Parser = data => data[0] * 256 + data[1] },
        [0x51] = new PidData { Pid = 0x51, Name = "Typ paliwa", Unit = "", MinValue = 0, MaxValue = 255,
            Parser = data => data[0] },
        [0x5A] = new PidData { Pid = 0x5A, Name = "Wzgledna pozycja pedalu gazu", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x5B] = new PidData { Pid = 0x5B, Name = "Zywotnosc akumulatora hybryd.", Unit = "%", MinValue = 0, MaxValue = 100,
            Parser = data => data[0] * 100.0 / 255 },
        [0x5E] = new PidData { Pid = 0x5E, Name = "Zuzycie paliwa", Unit = "L/h", MinValue = 0, MaxValue = 3276.75,
            Parser = data => (data[0] * 256 + data[1]) / 20.0 },
    };
}
