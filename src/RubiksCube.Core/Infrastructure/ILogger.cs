namespace Qubed.Core.Infrastructure;

public interface ILogger
{
    public void WriteLine(string line = "");

    public void Write(string line);
}