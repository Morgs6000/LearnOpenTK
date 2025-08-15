using OpenTK.Windowing.Desktop;

namespace ConsoleApp1.src;

public class Program {
    public static void Main() {
        Console.WriteLine("Hello World!");

        GameWindowSettings gws = GameWindowSettings.Default;

        NativeWindowSettings nws = NativeWindowSettings.Default;
        nws.ClientSize = (800, 600);
        nws.Title = "Breakout";

        new Game(gws, nws).Run();
    }
}
