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

    private Shader _shader;

    private uint _cubeVAO, _cubeVBO;
    private uint _planeVAO, _planeVBO;
    private uint _transparentVAO, _transparentVBO;

    private uint _cubeTexture;
    private uint _floorTexture;
    private uint _transparentTexture;

    // locais de vegetação transparente
    // --------------------------------------------------
    private List<Vector3> _windows = new List<Vector3>()
    {
        new Vector3(-1.5f,  0.0f, -0.48f),
        new Vector3( 1.5f,  0.0f,  0.51f),
        new Vector3( 0.0f,  0.0f,  0.7f),
        new Vector3(-0.3f,  0.0f, -2.3f),
        new Vector3( 0.5f,  0.0f, -0.6f)
    };
    
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

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);
        GL.FrontFace(FrontFaceDirection.Ccw);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader("src/blending.vs", "src/blending.fs");
    }

    protected override void OnLoad()
    {
        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] cubeVertices =
        {
            // posições            // coordenadas de textura
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,   0.0f, 1.0f,
            
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,
            
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 1.0f
        };

        float[] planeVertices =
        {
            // posições            // coordenadas de textura
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f, -5.0f,   2.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f,  5.0f,   0.0f, 2.0f
        };

        float[] transparentVertices =
        {
            // posições            // coordenadas de textura (coordenadas Y invertidas porque a textura está de cabeça para baixo)
             0.0f, -0.5f,  0.0f,   0.0f, 1.0f,
             1.0f, -0.5f,  0.0f,   1.0f, 1.0f,
             1.0f,  0.5f,  0.0f,   1.0f, 0.0f,
             0.0f, -0.5f,  0.0f,   0.0f, 1.0f,
             1.0f,  0.5f,  0.0f,   1.0f, 0.0f,
             0.0f,  0.5f,  0.0f,   0.0f, 0.0f
        };

        // cube VAO
        GL.GenVertexArrays(1, out _cubeVAO);
        GL.GenBuffers(1, out _cubeVBO);

        GL.BindVertexArray(_cubeVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, cubeVertices.Length * sizeof(float), cubeVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        GL.BindVertexArray(0);

        // plane VAO
        GL.GenVertexArrays(1, out _planeVAO);
        GL.GenBuffers(1, out _planeVBO);

        GL.BindVertexArray(_planeVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _planeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, planeVertices.Length * sizeof(float), planeVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        // transparent VAO
        GL.GenVertexArrays(1, out _transparentVAO);
        GL.GenBuffers(1, out _transparentVBO);

        GL.BindVertexArray(_transparentVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _transparentVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, transparentVertices.Length * sizeof(float), transparentVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        GL.BindVertexArray(0);

        // carregar texturas
        // --------------------------------------------------
        _cubeTexture = LoadTexture("res/textures/marble.jpg");
        _floorTexture = LoadTexture("res/textures/metal.png");
        _transparentTexture = LoadTexture("res/textures/window.png");

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("texture1", 0);
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
        // ordena as janelas transparentes antes da renderização
        // --------------------------------------------------
        SortedDictionary<float, Vector3> sorted = [];

        for (int i = 0; i < _windows.Count(); i++)
        {
            float distance = Vector3.Distance(_camera.Position, _windows[i]);
            sorted[distance] = _windows[i];
        }

        // render
        // --------------------------------------------------
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // desenhar objetos
        _shader.Use();

        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(_camera.Zoom),
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT,
            depthNear: 0.1f,
            depthFar:  100.0f
        );
        Matrix4 view = _camera.GetViewMatrix();
        Matrix4 model = Matrix4.Identity;

        _shader.SetMat4("view", view);
        _shader.SetMat4("projection", projection);

        // cubos
        GL.BindVertexArray(_cubeVAO);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _cubeTexture);

        model *= Matrix4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f));
        _shader.SetMat4("model", model);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        model = Matrix4.Identity;
        model *= Matrix4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f));
        _shader.SetMat4("model", model);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // floor
        GL.BindVertexArray(_planeVAO);

        GL.BindTexture(TextureTarget.Texture2D, _floorTexture);

        _shader.SetMat4("model", Matrix4.Identity);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        // windows (do mais distante para o mais próximo)
        GL.BindVertexArray(_transparentVAO);

        GL.BindTexture(TextureTarget.Texture2D, _transparentTexture);

        foreach (var it in sorted.Reverse())
        {
            model = Matrix4.Identity;
            model *= Matrix4.CreateTranslation(it.Value);
            _shader.SetMat4("model", model);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        GL.DeleteVertexArrays(1, ref _cubeVAO);
        GL.DeleteVertexArrays(1, ref _planeVAO);
        GL.DeleteBuffers(1, ref _cubeVBO);
        GL.DeleteBuffers(1, ref _planeVBO);
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

    // função utilitária para carregar uma textura 2D a partir de um arquivo
    // --------------------------------------------------
    private uint LoadTexture(string path)
    {
        uint textureID;
        GL.GenTextures(1, out textureID);

        int width, height;
        byte[] data;

        ImageResult image;

        using (FileStream stream = File.OpenRead(path))
        {
            image = ImageResult.FromStream(stream, ColorComponents.Default);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgb;
            PixelFormat pixelFormat = PixelFormat.Rgb;

            if (image.Comp == ColorComponents.Grey)
            {
                pixelInternalFormat = PixelInternalFormat.CompressedRed;
                pixelFormat = PixelFormat.Red;
            }
            else if (image.Comp == ColorComponents.RedGreenBlue)
            {
                pixelInternalFormat = PixelInternalFormat.Rgb;
                pixelFormat = PixelFormat.Rgb;
            }
            else if (image.Comp == ColorComponents.RedGreenBlueAlpha)
            {
                pixelInternalFormat = PixelInternalFormat.Rgba;
                pixelFormat = PixelFormat.Rgba;
            }

            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, pixelInternalFormat, width, height, 0, pixelFormat, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura no caminho: " + path);
        }

        return textureID;
    }
}
