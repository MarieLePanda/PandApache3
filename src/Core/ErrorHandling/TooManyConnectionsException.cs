namespace PandApache3.src.Core.ErrorHandling
{
    public class TooManyConnectionsException : Exception
    {
        public TooManyConnectionsException() : base("Too many connections") { }
        public TooManyConnectionsException(string message) : base(message) { }
        public TooManyConnectionsException(string message, Exception innerException) : base(message, innerException) { }
    }
}
