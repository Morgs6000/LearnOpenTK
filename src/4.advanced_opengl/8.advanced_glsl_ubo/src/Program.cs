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

    // câmera
    private Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 3.0f));
    private float _lastX = SCR_WIDTH / 2.0f;
    private float _lastY = SCR_HEIGHT / 2.0f;
    private bool _firstMouse = true;

    // tempo
    private float _deltaTime = 0.0f;
    private float _lastFrame = 0.0f;

    private Shader _shaderRed;
    private Shader _shaderGreen;
    private Shader _shaderBlue;
    private Shader _shaderYellow;

    private uint _cubeVAO, _cubeVBO;

    private uint _uboMatrices;
    
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
        // instruir o GLFW a capturar o mouse
        CursorState = CursorState.Grabbed;

        // configurar estado global do OpenGL
        // --------------------------------------------------
        GL.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shaderRed = new Shader("src/advanced_glsl.vs", "src/red.fs");
        _shaderGreen = new Shader("src/advanced_glsl.vs", "src/green.fs");
        _shaderBlue = new Shader("src/advanced_glsl.vs", "src/blue.fs");
        _shaderYellow = new Shader("src/advanced_glsl.vs", "src/yellow.fs");
    }

    protected override void OnLoad()
    {
        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] cubeVertices =
        {
            // posições
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f,
            
             0.5f, -0.5f,  0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
             0.5f,  0.5f,  0.5f,
            
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
            
            -0.5f,  0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
            
             0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
            
            -0.5f, -0.5f,  0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f
        };

        // cube VAO
        GL.GenVertexArrays(1, out _cubeVAO);
        GL.GenBuffers(1, out _cubeVBO);

        GL.BindVertexArray(_cubeVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, cubeVertices.Length * sizeof(float), cubeVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        // configurar um objeto de buffer uniforme
        // --------------------------------------------------

        // primeiro. Obtemos os índices de bloco relevantes
        int uniformBlockIndexRed = GL.GetUniformBlockIndex(_shaderRed.ID, "Matrices");
        int uniformBlockIndexGreen = GL.GetUniformBlockIndex(_shaderGreen.ID, "Matrices");
        int uniformBlockIndexBlue = GL.GetUniformBlockIndex(_shaderBlue.ID, "Matrices");
        int uniformBlockIndexYellow = GL.GetUniformBlockIndex(_shaderYellow.ID, "Matrices");

        // então, vinculamos o bloco de uniformes de cada shader a este ponto de vinculação de uniformes
        GL.UniformBlockBinding(_shaderRed.ID, uniformBlockIndexRed, 0);
        GL.UniformBlockBinding(_shaderGreen.ID, uniformBlockIndexGreen, 0);
        GL.UniformBlockBinding(_shaderBlue.ID, uniformBlockIndexBlue, 0);
        GL.UniformBlockBinding(_shaderYellow.ID, uniformBlockIndexYellow, 0);

        // Agora, de fato, crie o buffer
        GL.GenBuffers(1, out _uboMatrices);
        GL.BindBuffer(BufferTarget.UniformBuffer, _uboMatrices);
        unsafe
        {
            GL.BufferData(BufferTarget.UniformBuffer, 2 * sizeof(Matrix4), IntPtr.Zero, BufferUsageHint.StaticDraw);
        }
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);

        // define o intervalo do buffer que se conecta a um ponto de vinculação de uniform
        unsafe
        {
            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, 0, _uboMatrices, 0, 2 * sizeof(Matrix4));
        }

        // armazena a matriz de projeção (agora fazemos isso apenas uma vez) (nota: não usamos mais o zoom alterando o FoV)
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(45.0f), 
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            depthNear: 0.1f, 
            depthFar:  100.0f
        );
        GL.BindBuffer(BufferTarget.UniformBuffer, _uboMatrices);
        unsafe
        {
            GL.BufferSubData(BufferTarget.UniformBuffer, 0, sizeof(Matrix4), ref projection);
        }
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        FramebufferSizeCallback(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        // lógica de tempo por quadro
        // --------------------------------------------------
        float currentFrame = (float)GLFW.GetTime();
        _deltaTime = currentFrame - _lastFrame;
        _lastFrame = currentFrame;

        // input
        // --------------------------------------------------
        ProcessInput();

        MouseCallback();
        ScrollCallback();
    }

    // loop de renderização
    // --------------------------------------------------
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // render
        // --------------------------------------------------
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // define as matrizes de visualização e projeção no bloco uniform — só precisamos fazer isso uma vez por iteração do loop.
        Matrix4 view = _camera.GetViewMatrix();

        GL.BindBuffer(BufferTarget.UniformBuffer, _uboMatrices);
        unsafe
        {
            GL.BufferSubData(BufferTarget.UniformBuffer, sizeof(Matrix4), sizeof(Matrix4), ref view);
        }
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);

        // desenhar 4 cubos

        // RED
        GL.BindVertexArray(_cubeVAO);
        _shaderRed.Use();
        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateTranslation(new Vector3(-0.75f, 0.75f, 0.0f)); // mover para o canto superior esquerdo
        _shaderRed.SetMat4("model", model);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // GREEN
        GL.BindVertexArray(_cubeVAO);
        _shaderGreen.Use();
        model = Matrix4.Identity;
        model *= Matrix4.CreateTranslation(new Vector3(0.75f, 0.75f, 0.0f)); // mover para o canto superior direito
        _shaderGreen.SetMat4("model", model);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // YELLOW
        GL.BindVertexArray(_cubeVAO);
        _shaderYellow.Use();
        model = Matrix4.Identity;
        model *= Matrix4.CreateTranslation(new Vector3(-0.75f, -0.75f, 0.0f)); // mover para baixo à esquerda 
        _shaderYellow.SetMat4("model", model);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // BLUE
        GL.BindVertexArray(_cubeVAO);
        _shaderBlue.Use();
        model = Matrix4.Identity;
        model *= Matrix4.CreateTranslation(new Vector3(0.75f, -0.75f, 0.0f)); // mover para baixo e para a direita
        _shaderBlue.SetMat4("model", model);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        GL.DeleteVertexArrays(1, ref _cubeVAO);
        GL.DeleteBuffers(1, ref _cubeVBO);
    }

    // processar toda a entrada: consultar a GLFW para saber se teclas relevantes foram pressionadas ou liberadas neste quadro e reagir de acordo
    // --------------------------------------------------
    private void ProcessInput()
    {
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            Close();
        }

        if (KeyboardState.IsKeyDown(Keys.W))
        {
            _camera.ProcessKeyboard(Camera_Movement.FORWARD, _deltaTime);
        }
        if (KeyboardState.IsKeyDown(Keys.S))
        {
            _camera.ProcessKeyboard(Camera_Movement.BACKWARD, _deltaTime);
        }
        if (KeyboardState.IsKeyDown(Keys.A))
        {
            _camera.ProcessKeyboard(Camera_Movement.LEFT, _deltaTime);
        }
        if (KeyboardState.IsKeyDown(Keys.D))
        {
            _camera.ProcessKeyboard(Camera_Movement.RIGHT, _deltaTime);
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

    // glfw: sempre que o mouse se move, este callback é chamado
    // --------------------------------------------------
    private void MouseCallback()
    {
        float xpos = MouseState.Position.X;
        float ypos = MouseState.Position.Y;

        if (_firstMouse)
        {
            _lastX = xpos;
            _lastY = ypos;

            _firstMouse = false;
        }

        float xoffset = xpos - _lastX;
        float yoffset = _lastY - ypos; // invertido, já que as coordenadas y vão de baixo para cima

        _lastX = xpos;
        _lastY = ypos;

        _camera.ProcessMouseMovement(xoffset, yoffset);
    }

    // glfw: sempre que a roda de rolagem do mouse é girada, este callback é chamado
    // --------------------------------------------------
    private void ScrollCallback()
    {
        _camera.ProcessMouseScroll(MouseState.ScrollDelta.Y);
    }
}
