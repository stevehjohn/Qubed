namespace RubiksCube.Core.Infrastructure;

public class ConsoleLogger : ILogger
{
    public void WriteLine(string line = "")
    {
        Console.WriteLine(line);
    }

    public void Write(string line)
    {
        Console.Write(line);
    }
}