using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace LearnOpenTK.src;

public class Program : GameWindow
{
    // Propriedades
    private static int _screenWidth = 800, _screenHeight = 600;

    // Câmera
    private Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 3.0f));
    private float _lastX = 400, _lastY = 300;
    private bool _firstMouse = true;

    private float _deltaTime = 0.0f;
    private float _lastFrame = 0.0f;

    private Shader _shader;

    private uint _cubeVAO, _cubeVBO;
    private uint _planeVAO, _planeVBO;

    private uint _cubeTexture;
    private uint _floorTexture;
    
    // A função principal (MAIN); é a partir daqui que iniciamos nossa aplicação e executamos o loop do jogo.
    private static void Main(string[] args)
    {
        GameWindowSettings gws = GameWindowSettings.Default;
        NativeWindowSettings nws = NativeWindowSettings.Default;

        nws.ClientSize = new Vector2i(_screenWidth, _screenHeight);
        nws.Title = "Learn OpenTK";
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
        // Opções
        CursorState = CursorState.Grabbed;

        // Define as dimensões da viewport
        GL.Viewport(0, 0, _screenWidth, _screenHeight);

        // Configurar algumas opções do OpenGL
        GL.Enable(EnableCap.DepthTest);
        // GL.DepthFunc(DepthFunction.Always); // Define para sempre passar no teste de profundidade (mesmo efeito de glDisable(GL_DEPTH_TEST))

        // Configurar e compilar nossos shaders
        _shader = new Shader("src/csm.vs", "src/csm.fs");
    }

    protected override void OnLoad()
    {
        // Define os dados do objeto (buffers, atributos de vértice)
        float[] cubeVertices =
        {
            // Posições            // Coordenadas de textura
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
            // Posições            // Coordenadas de textura (Observe que definimos esses valores como maiores que 1, o que, juntamente com GL_REPEAT como modo de repetição de textura, fará com que a textura do chão se repita)
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f, -5.0f,   2.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f,  5.0f,   0.0f, 2.0f
        };

        // Setup cube VAO
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

        // Setup plane VAO
        GL.GenVertexArrays(1, out _planeVAO);
        GL.GenBuffers(1, out _planeVBO);

        GL.BindVertexArray(_planeVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _planeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, planeVertices.Length * sizeof(float), planeVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        GL.BindVertexArray(0);

        // Carregar texturas
        _cubeTexture = LoadTexture("res/textures/marble.jpg");
        _floorTexture = LoadTexture("res/textures/metal.png");
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        // Define o tempo do quadro
        float currentFrame = (float)GLFW.GetTime();
        _deltaTime = currentFrame - _lastFrame;
        _lastFrame = currentFrame;

        // Verificar e chamar eventos
        DoMovement();

        KeyCallback();
        MouseCallback();
        ScrollCallback();
    }

    // loop de renderização
    // --------------------------------------------------
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // Limpa o buffer de cor
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Desenhar objetos
        _shader.Use();

        Matrix4 model;
        Matrix4 view = _camera.GetViewMatrix();
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(_camera.Zoom), 
            aspect:    (float)_screenWidth / (float)_screenHeight, 
            depthNear: 0.1f, 
            depthFar:  100.0f
        );

        _shader.SetMat4("view", view);
        _shader.SetMat4("projection", projection);

        // Cubos
        GL.BindVertexArray(_cubeVAO);
        GL.BindTexture(TextureTarget.Texture2D, _cubeTexture); // Omitimos a parte do glActiveTexture, pois TEXTURE0 já é a unidade de textura ativa padrão. (o sampler usado no fragment shader também está definido como 0 por padrão)

        model = Matrix4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f));
        _shader.SetMat4("model", model);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        model = Matrix4.Identity;
        model *= Matrix4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f));
        _shader.SetMat4("model", model);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // Chão
        GL.BindVertexArray(_planeVAO);
        GL.BindTexture(TextureTarget.Texture2D, _floorTexture);

        model = Matrix4.Identity;
        _shader.SetMat4("model", model);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);

        // Trocar os buffers
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        
    }

    // Esta função carrega uma textura a partir de um arquivo. Nota: funções de carregamento de textura como estas
    // geralmente são gerenciadas por um 'Gerenciador de Recursos' que administra todos os recursos (como texturas, modelos e áudio).
    // Para fins de aprendizado, vamos defini-la apenas como uma função utilitária.
    private uint LoadTexture(string path)
    {
        //Gerar ID de textura e carregar dados de textura
        uint textureID;
        GL.GenTextures(1, out textureID);

        int width, height;
        byte[] data;

        using (FileStream stream = File.OpenRead(path))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        GL.BindTexture(TextureTarget.Texture2D, textureID);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, data);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.BindTexture(TextureTarget.Texture2D, 0);

        return textureID;
    }

    // Move/altera as posições da câmera com base na entrada do usuário
    private void DoMovement()
    {
        // Controles da câmera
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

    // Chamado sempre que uma tecla é pressionada ou liberada via GLFW
    private void KeyCallback()
    {
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            Close();
        }
    }

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
        float yoffset = _lastY - ypos;

        _lastX = xpos;
        _lastY = ypos;

        _camera.ProcessMouseMovement(xoffset, yoffset);
    }

    private void ScrollCallback()
    {
        _camera.ProcessMouseScroll(MouseState.ScrollDelta.Y);
    }
}
