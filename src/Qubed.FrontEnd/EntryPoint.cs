namespace Qubed.FrontEnd;

public static class EntryPoint
{
    public static void Main()
    {
        using var cube = new Display.Qubed();
        
        cube.Run();
    }
}