using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Logging;

public class CanLogger : IDisposable
{
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private bool _disposed;
    private long _messageCount;

    public string? FilePath { get; private set; }
    public bool IsLogging => _writer != null;
    public long MessageCount => _messageCount;

    public void StartLogging(string filePath, LogFormat format = LogFormat.CSV)
    {
        StopLogging();

        FilePath = filePath;
        _writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        _messageCount = 0;

        switch (format)
        {
            case LogFormat.CSV:
                _writer.WriteLine("Timestamp,Direction,ID,DLC,Data,ASCII");
                break;
            case LogFormat.ASC:
                _writer.WriteLine($"date {DateTime.Now:ddd MMM dd hh:mm:ss.fff tt yyyy}");
                _writer.WriteLine("base hex  timestamps absolute");
                _writer.WriteLine("internal events logged");
                _writer.WriteLine("Begin Triggerblock");
                break;
        }

        _writer.Flush();
        Log.Information("CAN logging started: {FilePath}", filePath);
    }

    public void LogMessage(CanMessage message, LogFormat format = LogFormat.CSV)
    {
        if (_writer == null) return;

        lock (_lock)
        {
            try
            {
                string line = format switch
                {
                    LogFormat.CSV => FormatCsv(message),
                    LogFormat.ASC => FormatAsc(message),
                    _ => FormatCsv(message)
                };

                _writer.WriteLine(line);
                _messageCount++;

                if (_messageCount % 100 == 0)
                    _writer.Flush();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error writing CAN log");
            }
        }
    }

    public void LogMessages(IEnumerable<CanMessage> messages, LogFormat format = LogFormat.CSV)
    {
        foreach (var msg in messages)
            LogMessage(msg, format);
    }

    public void StopLogging()
    {
        lock (_lock)
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
                Log.Information("CAN logging stopped. {Count} messages logged to {FilePath}",
                    _messageCount, FilePath);
            }
        }
    }

    private static string FormatCsv(CanMessage msg)
    {
        return $"{msg.TimestampFormatted},{msg.Direction},{msg.IdHex},{msg.DataLength},{msg.DataHex},{msg.DataAscii}";
    }

    private static string FormatAsc(CanMessage msg)
    {
        var timestamp = msg.Timestamp.TimeOfDay.TotalSeconds;
        var dataStr = string.Join(" ", msg.Data.Select(b => $"{b:X2}"));
        return $"  {timestamp:F6} 1  {msg.Id:X}x       {msg.Direction}   d {msg.DataLength} {dataStr}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopLogging();
        _disposed = true;
    }
}

public enum LogFormat
{
    CSV,
    ASC
}
