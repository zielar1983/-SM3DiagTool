using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Logging;

public class SessionLogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _lock = new();

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    public event Action<LogEntry>? OnNewEntry;

    public void LogInfo(string message)
    {
        AddEntry(LogLevel.Info, message);
        Log.Information(message);
    }

    public void LogWarning(string message)
    {
        AddEntry(LogLevel.Warning, message);
        Log.Warning(message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        AddEntry(LogLevel.Error, ex != null ? $"{message}: {ex.Message}" : message);
        Log.Error(ex, message);
    }

    public void LogData(string direction, byte[] data, string? label = null)
    {
        var hex = BitConverter.ToString(data).Replace("-", " ");
        var msg = label != null ? $"[{label}] {direction}: {hex}" : $"{direction}: {hex}";
        AddEntry(LogLevel.Data, msg);
    }

    private void AddEntry(LogLevel level, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message
        };

        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > 10000)
                _entries.RemoveRange(0, 1000);
        }

        OnNewEntry?.Invoke(entry);
    }

    public void SaveToFile(string filePath)
    {
        lock (_lock)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("Timestamp,Level,Message");
            foreach (var entry in _entries)
            {
                writer.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{entry.Level},{entry.Message}");
            }
        }
    }

    public void Clear()
    {
        lock (_lock) _entries.Clear();
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;

    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
    public string LevelText => Level.ToString().ToUpper();
}

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Data
}
