using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace LearnOpenTK.src;

public class Program : GameWindow
{
    // configurações
    private const int SCR_WIDTH = 800;
    private const int SCR_HEIGHT = 600;

    public static ErrorCode CheckError([CallerFilePath]string file = "", [CallerLineNumber]int line = 0)
    {
        ErrorCode errorCode;

        while ((errorCode = GL.GetError()) != ErrorCode.NoError)
        {
            string error = string.Empty;

            switch (errorCode)
            {
                case ErrorCode.InvalidEnum:
                    error = "INVALID_ENUM";
                    break;
                case ErrorCode.InvalidValue:
                    error = "INVALID_VALUE";
                    break;
                // case ErrorCode.StackOverflow:
                //     error = "STACK_OVERFLOW";
                // //     break;
                // case ErrorCode.StackUnderflow:
                //     error = "STACK_UNDERFLOW";
                //     break;
                case ErrorCode.OutOfMemory:
                    error = "OUT_OF_MEMORY";
                    break;
                case ErrorCode.InvalidFramebufferOperation:
                    error = "INVALID_FRAMEBUFFER_OPERATION";
                    break;
            }

            Console.WriteLine(error + " | " + file + " (" + line + ")");
        }

        return errorCode;
    }

    public static void DebugOutput(
        DebugSource source, 
        DebugType type, 
        int id, 
        DebugSeverity 
        severity, 
        int length, 
        IntPtr message, 
        IntPtr userParam
    )
    {
        if (id == 131169 || id == 131185 || id == 131218 || id == 131204)
        {
            return; // ignore estes códigos de erro não significativos
        }

        Console.WriteLine("---------------");
        Console.WriteLine("Debug message (" + id + "): " + message);

        switch (source)
        {
            case DebugSource.DebugSourceApi:
                Console.Write("Source: API");
                break;
            case DebugSource.DebugSourceWindowSystem:
                Console.Write("Source: Window System");
                break;
            case DebugSource.DebugSourceShaderCompiler:
                Console.Write("Source: Shader Compiler");
                break;
            case DebugSource.DebugSourceThirdParty:
                Console.Write("Source: Third Party");
                break;
            case DebugSource.DebugSourceApplication:
                Console.Write("Source: Application");
                break;
            case DebugSource.DebugSourceOther:
                Console.Write("Source: Other");
                break;
        }
        Console.WriteLine();

        switch (type)
        {
            case DebugType.DebugTypeError:
                Console.Write("Type: Erro");
                break;
            case DebugType.DebugTypeDeprecatedBehavior:
                Console.Write("Type: Deprecated Behaviour");
                break;
            case DebugType.DebugTypeUndefinedBehavior:
                Console.Write("Type: Undefined Behaviour");
                break;
            case DebugType.DebugTypePortability:
                Console.Write("Type: Portability");
                break;
            case DebugType.DebugTypePerformance:
                Console.Write("Type: Performance");
                break;
            case DebugType.DebugTypeMarker:
                Console.Write("Type: Marker");
                break;
            case DebugType.DebugTypePushGroup:
                Console.Write("Type: Push Group");
                break;
            case DebugType.DebugTypePopGroup:
                Console.Write("Type: Pop Group");
                break;
            case DebugType.DebugTypeOther:
                Console.Write("Type: Other");
                break;
        }
        Console.WriteLine();

        switch (severity)
        {
            case DebugSeverity.DebugSeverityHigh:
                Console.Write("Severity: high");
                break;
            case DebugSeverity.DebugSeverityMedium:
                Console.Write("Severity: medium");
                break;
            case DebugSeverity.DebugSeverityLow:
                Console.Write("Severity: low");
                break;
            case DebugSeverity.DebugSeverityNotification:
                Console.Write("Severity: notification");
                break;
        }
        Console.WriteLine();

        Console.WriteLine();
    }

    private Shader _shader;

    private uint _cubeVAO, _cubeVBO;

    private uint _texture;
    
    private static void Main(string[] args)
    {
        // criação da janela glfw
        // --------------------------------------------------
        GameWindowSettings gws = GameWindowSettings.Default;
        NativeWindowSettings nws = NativeWindowSettings.Default;

        nws.ClientSize = new Vector2i(SCR_WIDTH, SCR_HEIGHT);
        nws.Title = "Learn OpenTK";
        nws.StartVisible = false;
        nws.Flags = ContextFlags.Debug; // comente esta linha em uma build de produção!

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
        // instruir o GLFW a capturar o mouse
        CursorState = CursorState.Grabbed;

        // habilita o contexto de depuração do OpenGL se o contexto permitir um contexto de depuração
        int flags;
        GL.GetInteger(GetPName.ContextFlags, out flags);

        if ((flags & (int)ContextFlags.Debug) != 0)
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous); // garante que os erros sejam exibidos de forma síncrona
            GL.DebugMessageCallback(DebugOutput, IntPtr.Zero);
            GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);
        }

        // configurar estado global do OpenGL
        // --------------------------------------------------
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader("src/debugging.vs", "src/debugging.fs");
    }

    protected override void OnLoad()
    {
        // configure 3D cube
        float[] vertices =
        {
            // posições            // coordenadas de textura

            // face esquerda
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            // face direita
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,   0.0f, 1.0f,

            // face inferior
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,

            // face superior
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,

            // face posterior
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,   0.0f, 1.0f,

            // face frontal
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 1.0f
        };

        GL.GenVertexArrays(1, out _cubeVAO);
        GL.GenBuffers(1, out _cubeVBO);

        // preencher buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // vincular atributos de vértice
        GL.BindVertexArray(_cubeVAO);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        // carregar textura de cubo
        GL.GenTextures(1, out _texture);
        GL.BindTexture(TextureTarget.Texture2D, _texture);

        int width, height;
        byte[] data;

        using (FileStream stream = File.OpenRead("res/textures/wood.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            GL.TexImage2D((TextureTarget)FramebufferTarget.Framebuffer, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura");
        }

        // configurar a matriz de projeção
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(45.0f), 
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            depthNear: 0.1f, 
            depthFar:  10.0f
        );
        GL.UniformMatrix4(GL.GetUniformLocation(_shader.ID, "projection"), false, ref projection);
        GL.Uniform1(GL.GetUniformLocation(_shader.ID, "tex"), 0);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        FramebufferSizeCallback(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        // input
        // --------------------------------------------------
        ProcessInput();
    }

    // loop de renderização
    // --------------------------------------------------
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // render
        // --------------------------------------------------
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

        float rotationSpeed = 10.0f;
        float angle = (float)GLFW.GetTime() * rotationSpeed;

        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 1.0f, 1.0f), MathHelper.DegreesToRadians(angle));
        model *= Matrix4.CreateTranslation(new Vector3(0.0f, 0.0f, -2.5f));
        GL.UniformMatrix4(GL.GetUniformLocation(_shader.ID, "model"), false, ref model);

        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.BindVertexArray(_cubeVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        GL.BindVertexArray(0);

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        
    }

    // renderQuad() renderiza um quadrilátero XY de 1x1 em NDC
    // --------------------------------------------------
    private uint _quadVAO = 0;
    private uint _quadVBO;

    private void RenderQuad()
    {
        if (_quadVAO == 0)
        {
            float[] quadVertices =
            {
                // posições           // coordenadas de textura
                -1.0f,  1.0f, 0.0f,   0.0f, 1.0f,
                -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, 0.0f,   1.0f, 1.0f,
                 1.0f, -1.0f, 0.0f,   1.0f, 0.0f
            };

            // setup plane VAO
            GL.GenVertexArrays(1, out _quadVAO);
            GL.GenBuffers(1, out _quadVBO);

            GL.BindVertexArray(_quadVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof (float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof (float), 3 * sizeof(float));
        }

        GL.BindVertexArray(_quadVAO);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.BindVertexArray(0);
    }

    // processar toda a entrada: consultar a GLFW para saber se teclas relevantes foram pressionadas ou liberadas neste quadro e reagir de acordo
    // --------------------------------------------------
    private void ProcessInput()
    {
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            Close();
        }
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
