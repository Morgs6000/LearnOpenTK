using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace LearnOpenTK.src;

public class Program : GameWindow
{
    // configurações
    private const int SCR_WIDTH = 800;
    private const int SCR_HEIGHT = 600;

    private Shader _shader;

    private uint _instanceVBO;

    private uint _quadVAO, _quadVBO;
    
    private static void Main(string[] args)
    {
        // criação da janela glfw
        // --------------------------------------------------
        GameWindowSettings gws = GameWindowSettings.Default;
        NativeWindowSettings nws = NativeWindowSettings.Default;

        nws.ClientSize = new Vector2i(SCR_WIDTH, SCR_HEIGHT);
        nws.Title = "Learn OpenTK";
        nws.StartVisible = false;

        using (Program program = new Program(gws, nws))
        {
            if (OperatingSystem.IsWindows())
            {
                program.CenterWindow();
            }
            program.IsVisible = true;

            try
            {
                program.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Falha ao criar a janela OpenTK" + "\n" +
                    e
                );
            }
        }
    }

    public Program(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        // configurar estado global do OpenGL
        // --------------------------------------------------
        GL.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader("src/instancing.vs", "src/instancing.fs");
    }

    protected override void OnLoad()
    {
        // gerar uma lista de 100 localizações de quad/vetores de translação
        // --------------------------------------------------
        Vector2[] translations = new Vector2[100];

        int index = 0;
        float offset = 0.1f;

        for (int y = -10; y < 10; y += 2)
        {
            for (int x = -10; x < 10; x += 2)
            {
                Vector2 translation;
                translation.X = (float)x / 10.0f + offset;
                translation.Y = (float)y / 10.0f + offset;

                translations[index++] = translation;
            }
        }

        // armazena dados da instância em um buffer de array
        // --------------------------------------------------
        GL.GenBuffers(1, out _instanceVBO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
        unsafe
        {
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(Vector2) * 100, ref translations[0], BufferUsageHint.StaticDraw);
        }
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] quadVertices =
        {
            // posições       // cores
            -0.05f, -0.05f,   1.0f, 0.0f, 0.0f,
             0.05f, -0.05f,   0.0f, 1.0f, 0.0f,
             0.05f,  0.05f,   0.0f, 0.0f, 1.0f,
            -0.05f, -0.05f,   1.0f, 0.0f, 0.0f,
             0.05f,  0.05f,   0.0f, 0.0f, 1.0f,
            -0.05f,  0.05f,   1.0f, 1.0f, 0.0f
        };

        GL.GenVertexArrays(1, out _quadVAO);
        GL.GenBuffers(1, out _quadVBO);

        GL.BindVertexArray(_quadVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));

        // define também os dados da instância
        GL.EnableVertexAttribArray(2);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO); // este atributo vem de um buffer de vértices diferente
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.VertexAttribDivisor(2, 1); // informe ao OpenGL que este é um atributo de vértice instanciado.
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        FramebufferSizeCallback(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        
    }

    // loop de renderização
    // --------------------------------------------------
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // render
        // --------------------------------------------------
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // desenha 100 quads instanciados
        _shader.Use();

        GL.BindVertexArray(_quadVAO);
        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, 100); // 100 triângulos de 6 vértices cada
        GL.BindVertexArray(0);

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        GL.DeleteVertexArrays(1, ref _quadVAO);
        GL.DeleteBuffers(1, ref _quadVBO);
    }

    // glfw: sempre que o tamanho da janela é alterado (pelo SO ou por redimensionamento do usuário), esta função de callback é executada
    // --------------------------------------------------
    private void FramebufferSizeCallback(int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        GL.Viewport(0, 0, width, height);
    }
}
