/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace LearnOpenTK.src;

public class Program : GameWindow
{
    // A largura da tela
    private const int SCREEN_WIDTH = 800;

    // A altura da tela
    private const int SCREEN_HEIGHT = 600;

    private Game Breakout = new Game(SCREEN_WIDTH, SCREEN_HEIGHT);

    // variáveis ​​de deltaTime
    // --------------------------------------------------
    private float _deltaTime = 0.0f;
    private float _lastFrame = 0.0f;

    private static void Main(string[] args)
    {
        GameWindowSettings gws = GameWindowSettings.Default;
        NativeWindowSettings nws = NativeWindowSettings.Default;

        nws.ClientSize = new Vector2i(SCREEN_WIDTH, SCREEN_HEIGHT);
        nws.Title = "Breakout";
        nws.StartVisible = false;

        using (Program program = new Program(gws, nws))
        {
            if (OperatingSystem.IsWindows())
            {
                program.CenterWindow();
            }
            program.IsVisible = true;

            program.Run();
        }
    }

    public Program(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        // Configuração do OpenGL
        // --------------------------------------------------
        GL.Viewport(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    protected override void OnLoad()
    {
        // inicializar o jogo
        // --------------------------------------------------
        Breakout.Init();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        FramebufferSizeCallback(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        // calcular o delta de tempo
        // --------------------------------------------------
        float currentFrame = (float)GLFW.GetTime();
        _deltaTime = currentFrame - _lastFrame;
        _lastFrame = currentFrame;

        // gerenciar a entrada do usuário
        // --------------------------------------------------
        Breakout.ProcessInput(_deltaTime);

        // atualizar estado do jogo
        // --------------------------------------------------
        Breakout.Update(_deltaTime);

        KeyCallback();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // render
        // --------------------------------------------------
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        Breakout.Render();

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        // exclui todos os recursos carregados usando o gerenciador de recursos
        // --------------------------------------------------
        ResourceManager.Clear();

        Breakout.Dispose();
    }

    private void KeyCallback()
    {
        // quando o usuário pressiona a tecla Esc, definimos a propriedade WindowShouldClose como true, fechando o aplicativo
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            Close();
        }
    }

    private void FramebufferSizeCallback(int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        GL.Viewport(0, 0, width, height);
    }
}
