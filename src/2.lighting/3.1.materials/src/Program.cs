using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
    private float _deltaTime = 0.0f; // tempo entre o quadro atual e o quadro anterior
    private float _lastFrame = 0.0f; 

    // iluminação
    private Vector3 _lightPos = new Vector3(1.2f, 1.0f, 2.0f);

    private Shader _lightingShader;
    private Shader _lightCubeShader;

    private uint _cubeVAO;
    private uint _VBO;

    private uint _lightCubeVAO;
    
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
        _lightingShader = new Shader("src/materials.vs", "src/materials.fs");
        _lightCubeShader = new Shader("src/light_cube.vs", "src/light_cube.fs");
    }

    protected override void OnLoad()
    {
        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] vertices =
        {
            // posições            // normais
            -0.5f, -0.5f, -0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,   -1.0f,  0.0f,  0.0f,
            
             0.5f, -0.5f,  0.5f,    1.0f,  0.0f,  0.0f,
             0.5f, -0.5f, -0.5f,    1.0f,  0.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    1.0f,  0.0f,  0.0f,
             0.5f, -0.5f,  0.5f,    1.0f,  0.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    1.0f,  0.0f,  0.0f,
             0.5f,  0.5f,  0.5f,    1.0f,  0.0f,  0.0f,
            
            -0.5f, -0.5f, -0.5f,    0.0f, -1.0f,  0.0f,
             0.5f, -0.5f, -0.5f,    0.0f, -1.0f,  0.0f,
             0.5f, -0.5f,  0.5f,    0.0f, -1.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,    0.0f, -1.0f,  0.0f,
             0.5f, -0.5f,  0.5f,    0.0f, -1.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,    0.0f, -1.0f,  0.0f,
            
            -0.5f,  0.5f,  0.5f,    0.0f,  1.0f,  0.0f,
             0.5f,  0.5f,  0.5f,    0.0f,  1.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,    0.0f,  1.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,    0.0f,  1.0f,  0.0f,
            
             0.5f, -0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            -0.5f, -0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            -0.5f,  0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
             0.5f, -0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            -0.5f,  0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
             0.5f,  0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            
            -0.5f, -0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
             0.5f, -0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
             0.5f,  0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
            -0.5f, -0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
             0.5f,  0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
            -0.5f,  0.5f,  0.5f,    0.0f,  0.0f,  1.0f
        };

        // primeiro, configure o VAO (e o VBO) do cubo
        GL.GenVertexArrays(1, out _cubeVAO);
        GL.GenBuffers(1, out _VBO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(_cubeVAO);

        // atributo de posição
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // atributo normal
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // segundo, configure o VAO da luz (o VBO permanece o mesmo; os vértices são os mesmos para o objeto de luz, que também é um cubo 3D)
        GL.GenVertexArrays(1, out _lightCubeVAO);
        GL.BindVertexArray(_lightCubeVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _VBO);

        // observe que atualizamos o stride do atributo de posição da lâmpada para refletir os dados atualizados do buffer
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
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

        // certifique-se de ativar o shader ao definir uniforms ou desenhar objetos
        _lightingShader.Use();
        _lightingShader.SetVec3("light.position", _lightPos);
        _lightingShader.SetVec3("viewPos", _camera.Position);

        // propriedades da luz
        Vector3 lightColor;
        lightColor.X = MathF.Sin((float)GLFW.GetTime() * 2.0f);
        lightColor.Y = MathF.Sin((float)GLFW.GetTime() * 0.7f);
        lightColor.Z = MathF.Sin((float)GLFW.GetTime() * 1.3f);

        Vector3 diffuseColor = lightColor   * new Vector3(0.5f); // reduzir a influência
        Vector3 ambientColor = diffuseColor * new Vector3(0.2f); // baixa influência

        _lightingShader.SetVec3("light.ambient", ambientColor);
        _lightingShader.SetVec3("light.diffuse", diffuseColor);
        _lightingShader.SetVec3("light.specular", 1.0f, 1.0f, 1.0f);

        // propriedades do material
        _lightingShader.SetVec3("material.ambient", 1.0f, 0.5f, 0.31f);
        _lightingShader.SetVec3("material.diffuse", 1.0f, 0.5f, 0.31f);
        _lightingShader.SetVec3("material.specular", 0.5f, 0.5f, 0.5f); // a iluminação especular não tem efeito total no material deste objeto
        _lightingShader.SetFloat("material.shininess", 32.0f);

        // transformações de visualização/projeção
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(_camera.Zoom),
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT,
            depthNear: 0.1f,
            depthFar:  100.0f
        );
        Matrix4 view = _camera.GetViewMatrix();

        _lightCubeShader.SetMat4("projection", projection);
        _lightCubeShader.SetMat4("view", view);

        // transformação do mundo
        Matrix4 model = Matrix4.Identity;
        _lightCubeShader.SetMat4("model", model);

        // renderiza o cubo
        GL.BindVertexArray(_cubeVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // também desenhe o objeto da lâmpada
        _lightCubeShader.Use();
        _lightCubeShader.SetMat4("projection", projection);
        _lightCubeShader.SetMat4("view", view);

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.2f)); // um cubo menor
        model *= Matrix4.CreateTranslation(_lightPos);
        _lightCubeShader.SetMat4("model", model);

        GL.BindVertexArray(_lightCubeVAO);
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
        GL.DeleteVertexArrays(1, ref _lightCubeVAO);
        GL.DeleteBuffers(1, ref _VBO);
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
