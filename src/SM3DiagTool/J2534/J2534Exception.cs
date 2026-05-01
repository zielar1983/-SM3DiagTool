namespace SM3DiagTool.J2534;

public class J2534Exception : Exception
{
    public J2534Error ErrorCode { get; }

    public J2534Exception(J2534Error errorCode)
        : base($"J2534 Error: {errorCode}")
    {
        ErrorCode = errorCode;
    }

    public J2534Exception(J2534Error errorCode, string message)
        : base($"J2534 Error [{errorCode}]: {message}")
    {
        ErrorCode = errorCode;
    }

    public J2534Exception(string message)
        : base(message)
    {
        ErrorCode = J2534Error.ERR_FAILED;
    }

    public J2534Exception(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = J2534Error.ERR_FAILED;
    }
}
