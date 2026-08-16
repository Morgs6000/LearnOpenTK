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
    private Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 55.0f));
    private float _lastX = (float)SCR_WIDTH / 2.0f;
    private float _lastY = (float)SCR_HEIGHT / 2.0f;
    private bool _firstMouse = true;

    // timing
    private float _deltaTime = 0.0f;
    private float _lastFrame = 0.0f;

    private Shader _shader;

    private Model _rock;
    private Model _planet;

    private uint _amount;
    private Matrix4[] _modelMatrices;
    
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
        _shader = new Shader("src/instancing.vs", "src/instancing.fs");
        
        // carregar modelos
        // --------------------------------------------------
        _rock = new Model("res/objects/rock/rock.obj");
        _planet = new Model("res/objects/planet/planet.obj");
    }

    protected override void OnLoad()
    {
        // gerar uma lista grande de matrizes de transformação de modelo semialeatórias
        // --------------------------------------------------
        _amount = 1000;
        _modelMatrices = new Matrix4[_amount];

        Random rand = new Random((int)GLFW.GetTime()); // inicializar a semente de números aleatórios

        float radius = 50.0f;
        float offset = 2.5f;

        for (int i = 0; i < _amount; i++)
        {
            Matrix4 model = Matrix4.Identity;

            // 1. rotação: adicionar rotação aleatória em torno de um vetor de eixo de rotação escolhido de forma (semi)aleatória
            float rotAngle = (float)(rand.Next() % 360);
            model *= Matrix4.CreateFromAxisAngle(new Vector3(0.4f, 0.6f, 0.8f), rotAngle);

            // 2. scale: Escala entre 0,05 e 0,25f
            float scale = (float)((rand.Next() % 20) / 100.0f + 0.05f);
            model *= Matrix4.CreateScale(new Vector3(scale));

            // 3. tradução: deslocar ao longo de um círculo com 'raio' no intervalo [-offset, offset]
            float angle = (float)i / (float)_amount * 360.0f;
            float displacement = (rand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float x = MathF.Sin(angle) * radius + displacement;
            displacement = (rand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float y = displacement * 0.4f; // mantenha a altura do campo de asteroides menor em relação à largura nos eixos x e z
            displacement = (rand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float z = MathF.Cos(angle) * radius + displacement;
            model *= Matrix4.CreateTranslation(new Vector3(x, y, z));

            // 4. agora adicione à lista de matrizes
            _modelMatrices[i] = model;
        }
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

        // configurar matrizes de transformação
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(45.0f), 
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            depthNear: 0.1f, 
            depthFar:  1000.0f
        );
        Matrix4 view = _camera.GetViewMatrix();

        _shader.Use();
        _shader.SetMat4("projection", projection);
        _shader.SetMat4("view", view);

        // desenhar planeta
        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(4.0f, 4.0f, 4.0f));
        model *= Matrix4.CreateTranslation(new Vector3(0.0f, -3.0f, 0.0f));
        _shader.SetMat4("model", model);
        _planet.Draw(_shader);

        // desenhar meteoritos
        for (int i = 0; i < _amount; i++)
        {
            _shader.SetMat4("model", _modelMatrices[i]);
            _rock.Draw(_shader);
        }

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        
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
