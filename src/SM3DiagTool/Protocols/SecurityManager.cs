using Serilog;

namespace SM3DiagTool.Protocols;

public class SecurityManager
{
    private readonly Uds _uds;

    public enum SecurityLevel : byte
    {
        Level1_Seed = 0x01,
        Level1_Key = 0x02,
        Level3_Seed = 0x03,
        Level3_Key = 0x04,
        Level5_Seed = 0x05,
        Level5_Key = 0x06,
        Level7_Seed = 0x07,
        Level7_Key = 0x08,
        Level9_Seed = 0x09,
        Level9_Key = 0x0A,
        Level11_Seed = 0x0B,
        Level11_Key = 0x0C,
    }

    public enum AlgorithmType
    {
        SimpleXor,
        Crc32Based,
        CustomCallback,
        Vag,
        Bmw,
        Mercedes,
        Generic
    }

    public SecurityManager(Uds uds)
    {
        _uds = uds;
    }

    public bool UnlockLevel(byte seedLevel, AlgorithmType algorithm = AlgorithmType.Generic, Func<byte[], byte[]>? customKeyCalculator = null)
    {
        try
        {
            var seedResponse = _uds.SecurityAccess(seedLevel);
            if (seedResponse.Length < 2)
            {
                Log.Warning("Security Access: empty seed response for level 0x{Level:X2}", seedLevel);
                return false;
            }

            var seed = seedResponse.Skip(1).ToArray();

            if (IsSeedAllZeros(seed))
            {
                Log.Information("Security Access: already unlocked (seed is all zeros) for level 0x{Level:X2}", seedLevel);
                return true;
            }

            byte[] key;
            if (customKeyCalculator != null)
                key = customKeyCalculator(seed);
            else
                key = CalculateKey(seed, algorithm, seedLevel);

            _uds.SecurityAccess(seedLevel, key);
            Log.Information("Security Access: unlocked level 0x{Level:X2}", seedLevel);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Security Access failed for level 0x{Level:X2}", seedLevel);
            return false;
        }
    }

    public byte[] RequestSeed(byte seedLevel)
    {
        var response = _uds.SecurityAccess(seedLevel);
        return response.Length > 1 ? response.Skip(1).ToArray() : Array.Empty<byte>();
    }

    public bool SendKey(byte keyLevel, byte[] key)
    {
        try
        {
            _uds.SecurityAccess(keyLevel, key);
            return true;
        }
        catch { return false; }
    }

    public byte[] CalculateKey(byte[] seed, AlgorithmType algorithm, byte level = 0x01)
    {
        return algorithm switch
        {
            AlgorithmType.SimpleXor => CalculateSimpleXor(seed, level),
            AlgorithmType.Crc32Based => CalculateCrc32Based(seed),
            AlgorithmType.Vag => CalculateVagKey(seed, level),
            AlgorithmType.Bmw => CalculateBmwKey(seed),
            AlgorithmType.Mercedes => CalculateMercedesKey(seed),
            AlgorithmType.Generic => CalculateGenericKey(seed, level),
            _ => CalculateGenericKey(seed, level)
        };
    }

    private static byte[] CalculateSimpleXor(byte[] seed, byte level)
    {
        var key = new byte[seed.Length];
        byte xorByte = (byte)(level * 0x5A + 0x37);
        for (int i = 0; i < seed.Length; i++)
            key[i] = (byte)(seed[i] ^ xorByte);
        return key;
    }

    private static byte[] CalculateCrc32Based(byte[] seed)
    {
        var key = new byte[seed.Length];
        uint crc = 0xFFFFFFFF;

        foreach (var b in seed)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }

        crc ^= 0xFFFFFFFF;
        for (int i = 0; i < Math.Min(seed.Length, 4); i++)
            key[i] = (byte)((crc >> (i * 8)) & 0xFF);

        return key;
    }

    private static byte[] CalculateVagKey(byte[] seed, byte level)
    {
        var key = new byte[seed.Length];
        uint constant = level switch
        {
            0x01 => 0x01D60040u,
            0x03 => 0x011F0041u,
            0x05 => 0x01270042u,
            _ => 0x01D60040u
        };

        if (seed.Length >= 4)
        {
            uint seedVal = (uint)((seed[0] << 24) | (seed[1] << 16) | (seed[2] << 8) | seed[3]);

            for (int i = 0; i < 5; i++)
            {
                if ((seedVal & 1) != 0)
                    seedVal = (seedVal >> 1) ^ constant;
                else
                    seedVal >>= 1;
            }

            key[0] = (byte)(seedVal >> 24);
            key[1] = (byte)(seedVal >> 16);
            key[2] = (byte)(seedVal >> 8);
            key[3] = (byte)seedVal;
        }

        return key;
    }

    private static byte[] CalculateBmwKey(byte[] seed)
    {
        var key = new byte[seed.Length];
        if (seed.Length >= 2)
        {
            ushort seedVal = (ushort)((seed[0] << 8) | seed[1]);
            ushort keyVal = (ushort)((seedVal + 0x9365) ^ 0xAC95);
            key[0] = (byte)(keyVal >> 8);
            key[1] = (byte)keyVal;

            for (int i = 2; i < seed.Length; i++)
                key[i] = (byte)(seed[i] ^ key[i - 2]);
        }
        return key;
    }

    private static byte[] CalculateMercedesKey(byte[] seed)
    {
        var key = new byte[seed.Length];
        if (seed.Length >= 4)
        {
            uint seedVal = (uint)((seed[0] << 24) | (seed[1] << 16) | (seed[2] << 8) | seed[3]);
            uint keyVal = seedVal ^ 0x44434241;
            keyVal = ((keyVal << 3) | (keyVal >> 29)) ^ 0xAAAAAAAA;

            key[0] = (byte)(keyVal >> 24);
            key[1] = (byte)(keyVal >> 16);
            key[2] = (byte)(keyVal >> 8);
            key[3] = (byte)keyVal;
        }
        return key;
    }

    private static byte[] CalculateGenericKey(byte[] seed, byte level)
    {
        var key = new byte[seed.Length];
        for (int i = 0; i < seed.Length; i++)
        {
            byte rotated = (byte)((seed[i] << 3) | (seed[i] >> 5));
            key[i] = (byte)(rotated ^ (0xA5 + level + i));
        }
        return key;
    }

    private static bool IsSeedAllZeros(byte[] seed)
    {
        return seed.All(b => b == 0);
    }
}
