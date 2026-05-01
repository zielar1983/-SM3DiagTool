using SM3DiagTool.J2534;
using Serilog;

namespace SM3DiagTool.Protocols;

public class FlashManager
{
    private readonly Uds _uds;
    private int _blockSequenceCounter;

    public event Action<int, int>? ProgressChanged;
    public event Action<string>? StatusChanged;

    public FlashManager(Uds uds)
    {
        _uds = uds;
    }

    public bool RequestDownload(uint memoryAddress, uint memorySize, byte compressionMethod = 0x00, byte encryptionMethod = 0x00)
    {
        StatusChanged?.Invoke($"Zadanie pobrania: adres=0x{memoryAddress:X8}, rozmiar=0x{memorySize:X8}");

        byte dataFormatId = (byte)((compressionMethod << 4) | encryptionMethod);

        byte addressLength = GetByteLength(memoryAddress);
        byte sizeLength = GetByteLength(memorySize);
        byte addressAndLengthFormatId = (byte)((sizeLength << 4) | addressLength);

        var data = new List<byte> { dataFormatId, addressAndLengthFormatId };
        data.AddRange(GetBytes(memoryAddress, addressLength));
        data.AddRange(GetBytes(memorySize, sizeLength));

        try
        {
            var response = _uds.SendRequest(Uds.ServiceId.RequestDownload, data.ToArray());
            if (response.Length > 0 && response[0] == 0x74)
            {
                _blockSequenceCounter = 1;
                Log.Information("Download request accepted: address=0x{Address:X8}, size=0x{Size:X8}", memoryAddress, memorySize);
                return true;
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Blad zadania pobrania: {ex.Message}");
            Log.Error(ex, "Download request failed");
        }

        return false;
    }

    public bool RequestUpload(uint memoryAddress, uint memorySize, byte compressionMethod = 0x00, byte encryptionMethod = 0x00)
    {
        StatusChanged?.Invoke($"Zadanie wyslania: adres=0x{memoryAddress:X8}, rozmiar=0x{memorySize:X8}");

        byte dataFormatId = (byte)((compressionMethod << 4) | encryptionMethod);
        byte addressLength = GetByteLength(memoryAddress);
        byte sizeLength = GetByteLength(memorySize);
        byte addressAndLengthFormatId = (byte)((sizeLength << 4) | addressLength);

        var data = new List<byte> { dataFormatId, addressAndLengthFormatId };
        data.AddRange(GetBytes(memoryAddress, addressLength));
        data.AddRange(GetBytes(memorySize, sizeLength));

        try
        {
            var response = _uds.SendRequest(Uds.ServiceId.RequestUpload, data.ToArray());
            if (response.Length > 0 && response[0] == 0x75)
            {
                _blockSequenceCounter = 1;
                Log.Information("Upload request accepted: address=0x{Address:X8}, size=0x{Size:X8}", memoryAddress, memorySize);
                return true;
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Blad zadania wyslania: {ex.Message}");
            Log.Error(ex, "Upload request failed");
        }

        return false;
    }

    public byte[] TransferDataBlock(byte[]? blockData = null)
    {
        var data = new List<byte> { (byte)_blockSequenceCounter };
        if (blockData != null) data.AddRange(blockData);

        var response = _uds.SendRequest(Uds.ServiceId.TransferData, data.ToArray());
        _blockSequenceCounter = (_blockSequenceCounter + 1) & 0xFF;

        return response.Length > 1 ? response.Skip(1).ToArray() : Array.Empty<byte>();
    }

    public bool RequestTransferExit()
    {
        try
        {
            _uds.SendRequest(Uds.ServiceId.RequestTransferExit);
            Log.Information("Transfer exit completed");
            StatusChanged?.Invoke("Transfer zakonczony");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Transfer exit failed");
            StatusChanged?.Invoke($"Blad zakonczenia transferu: {ex.Message}");
            return false;
        }
    }

    public byte[] ReadMemory(uint address, uint size, int blockSize = 256)
    {
        StatusChanged?.Invoke($"Odczyt pamieci: 0x{address:X8}, rozmiar: {size} bajtow");

        if (!RequestUpload(address, size))
            return Array.Empty<byte>();

        var result = new List<byte>();
        uint remaining = size;
        int totalBlocks = (int)Math.Ceiling((double)size / blockSize);
        int currentBlock = 0;

        while (remaining > 0)
        {
            var blockData = TransferDataBlock();
            if (blockData.Length == 0) break;

            result.AddRange(blockData);
            remaining -= (uint)Math.Min(blockData.Length, remaining);
            currentBlock++;
            ProgressChanged?.Invoke(currentBlock, totalBlocks);
        }

        RequestTransferExit();
        Log.Information("Memory read complete: {Size} bytes from 0x{Address:X8}", result.Count, address);
        return result.ToArray();
    }

    public bool WriteMemory(uint address, byte[] data, int blockSize = 256)
    {
        StatusChanged?.Invoke($"Zapis pamieci: 0x{address:X8}, rozmiar: {data.Length} bajtow");

        if (!RequestDownload(address, (uint)data.Length))
            return false;

        int totalBlocks = (int)Math.Ceiling((double)data.Length / blockSize);

        for (int i = 0; i < data.Length; i += blockSize)
        {
            int chunkSize = Math.Min(blockSize, data.Length - i);
            var chunk = new byte[chunkSize];
            Array.Copy(data, i, chunk, 0, chunkSize);

            TransferDataBlock(chunk);
            ProgressChanged?.Invoke(i / blockSize + 1, totalBlocks);
        }

        var success = RequestTransferExit();
        if (success)
            Log.Information("Memory write complete: {Size} bytes to 0x{Address:X8}", data.Length, address);

        return success;
    }

    public byte[] ReadMemoryByAddress(uint address, ushort size)
    {
        byte addressLength = GetByteLength(address);
        byte sizeLength = GetByteLength(size);
        byte addressAndLengthFormatId = (byte)((sizeLength << 4) | addressLength);

        var data = new List<byte> { addressAndLengthFormatId };
        data.AddRange(GetBytes(address, addressLength));
        data.AddRange(GetBytes(size, sizeLength));

        var response = _uds.SendRequest(Uds.ServiceId.ReadMemoryByAddress, data.ToArray());
        return response.Length > 0 ? response.Skip(1).ToArray() : Array.Empty<byte>();
    }

    public bool WriteMemoryByAddress(uint address, byte[] writeData)
    {
        byte addressLength = GetByteLength(address);
        byte sizeLength = GetByteLength((uint)writeData.Length);
        byte addressAndLengthFormatId = (byte)((sizeLength << 4) | addressLength);

        var data = new List<byte> { addressAndLengthFormatId };
        data.AddRange(GetBytes(address, addressLength));
        data.AddRange(GetBytes((uint)writeData.Length, sizeLength));
        data.AddRange(writeData);

        try
        {
            _uds.SendRequest(0x3D, data.ToArray()); // WriteMemoryByAddress = 0x3D
            return true;
        }
        catch { return false; }
    }

    public bool EraseMemory(uint address, uint size)
    {
        StatusChanged?.Invoke($"Kasowanie pamieci: 0x{address:X8}, rozmiar: {size}");

        var routineData = new List<byte>();
        routineData.AddRange(GetBytes(address, 4));
        routineData.AddRange(GetBytes(size, 4));

        try
        {
            _uds.RoutineControl(0x01, 0xFF00, routineData.ToArray()); // Start routine: Erase Memory
            Log.Information("Memory erased: 0x{Address:X8}, size: {Size}", address, size);
            StatusChanged?.Invoke("Pamiec skasowana");
            return true;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Blad kasowania: {ex.Message}");
            Log.Error(ex, "Memory erase failed");
            return false;
        }
    }

    public bool CheckProgrammingDependencies()
    {
        try
        {
            _uds.RoutineControl(0x01, 0xFF01); // Check Programming Dependencies
            return true;
        }
        catch { return false; }
    }

    private static byte GetByteLength(uint value)
    {
        if (value <= 0xFF) return 1;
        if (value <= 0xFFFF) return 2;
        if (value <= 0xFFFFFF) return 3;
        return 4;
    }

    private static byte[] GetBytes(uint value, byte length)
    {
        var bytes = new byte[length];
        for (int i = length - 1; i >= 0; i--)
        {
            bytes[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
        return bytes;
    }
}
