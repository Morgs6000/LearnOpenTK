using OpenTK.Windowing.Desktop;

namespace ConsoleApp1.src;

public class Program {
    private static void Main(string[] args) {
        Console.WriteLine("Hello, World!");

        GameWindowSettings gws = GameWindowSettings.Default;

        NativeWindowSettings nws = NativeWindowSettings.Default;
        nws.ClientSize = (800, 600);
        nws.Title = "LearnOpenTK";

        new Window(gws, nws).Run();
    }
}
