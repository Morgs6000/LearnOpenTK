using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

// O OpenToolkit permite que diversas funções sejam substituídas para estender a funcionalidade; é assim que escreveremos o código.
public class Window : GameWindow {
    // Um ​​construtor simples que nos permite definir propriedades como tamanho da janela, título, FPS, etc. na janela.
    public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    // Esta função é executada em cada quadro de atualização.
    protected override void OnUpdateFrame(FrameEventArgs args) {
        // Verifique se o botão Escape está sendo pressionado no momento.
        if(KeyboardState.IsKeyDown(Keys.Escape)) {
            // Se estiver, feche a janela.
            Close();
        }

        base.OnUpdateFrame(args);
    }
}
