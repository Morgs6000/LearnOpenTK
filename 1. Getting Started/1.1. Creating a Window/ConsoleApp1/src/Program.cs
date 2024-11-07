using OpenTK.Windowing.Desktop;

namespace ConsoleApp1.src;

public class Program {
    private static void Main(string[] args) {
        GameWindowSettings gws = GameWindowSettings.Default;

        NativeWindowSettings nws = NativeWindowSettings.Default;
        nws.ClientSize = (800, 600);
        nws.Title = "LearnOpenTK = Creating a Window";

        // Para criar uma nova janela, crie uma classe que estenda GameWindow e chame Run() nela.
        new Window(gws, nws).Run();

        // E é isso! É tudo o que é preciso para criar uma janela com OpenTK.
    }
}
