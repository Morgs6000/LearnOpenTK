using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK;

public class Program
{
    private static GameWindowSettings gameWindowSettings => GWS();
    private static NativeWindowSettings nativeWindowSettings => NWS();

    private static void Main(string[] args)
    {
        // Console.WriteLine("Hello, World!");

        using (Window window = new Window(gameWindowSettings, nativeWindowSettings))
        {
            window.CenterWindow();
            window.IsVisible = true;
            window.Run();
        }
    }

    private static GameWindowSettings GWS()
    {
        GameWindowSettings gws = GameWindowSettings.Default;

        return gws;
    }

    private static NativeWindowSettings NWS()
    {
        NativeWindowSettings nws = NativeWindowSettings.Default;

        nws.ClientSize = new Vector2i(800, 600);
        nws.Title = "LearnOpenTK";
        nws.StartVisible = false;

        return nws;
    }
}
