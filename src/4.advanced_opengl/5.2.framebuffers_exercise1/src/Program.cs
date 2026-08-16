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
    private Shader _screenShader;

    private uint _cubeVAO, _cubeVBO;
    private uint _planeVAO, _planeVBO;
    private uint _quadVAO, _quadVBO;

    private uint _cubeTexture;
    private uint _floorTexture;

    private uint _framebuffer;
    private uint _textureColorbuffer;
    private uint _rbo;
    
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
        _shader = new Shader("src/framebuffers.vs", "src/framebuffers.fs");
        _screenShader = new Shader("src/framebuffers_screen.vs", "src/framebuffers_screen.fs");
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

        float[] quadVertices = // atributos de vértice para um quadrilátero que preenche toda a tela em Coordenadas de Dispositivo Normalizadas.
        {
            // posições     // coordenadas de textura
            -0.3f,  0.7f,   0.0f, 0.0f,
             0.3f,  0.7f,   1.0f, 0.0f,
             0.3f,  1.0f,   1.0f, 1.0f,
            -0.3f,  0.7f,   0.0f, 0.0f,
             0.3f,  1.0f,   1.0f, 1.0f,
            -0.3f,  1.0f,   0.0f, 1.0f
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

        // screen quad VAO
        GL.GenVertexArrays(1, out _quadVAO);
        GL.GenBuffers(1, out _quadVBO);

        GL.BindVertexArray(_quadVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

        // carregar texturas
        // --------------------------------------------------
        _cubeTexture = LoadTexture("res/textures/container.jpg");
        _floorTexture = LoadTexture("res/textures/metal.png");

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("texture1", 0);

        _screenShader.Use();
        _screenShader.SetInt("screenTexture", 0);

        // configuração do framebuffer
        // --------------------------------------------------
        GL.GenFramebuffers(1, out _framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

        // criar uma textura de anexo de cor
        GL.GenTextures(1, out _textureColorbuffer);
        GL.BindTexture(TextureTarget.Texture2D, _textureColorbuffer);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _textureColorbuffer, 0);

        // cria um objeto renderbuffer para anexos de profundidade e stencil (não faremos amostragem deles)
        GL.GenRenderbuffers(1, out _rbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, SCR_WIDTH, SCR_HEIGHT); // use um único objeto renderbuffer tanto para o buffer de profundidade quanto para o buffer de stencil.
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo); // agora, de fato, anexe-o

        // agora que realmente criamos o framebuffer e adicionamos todos os anexos, queremos verificar se ele está de fato completo
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("ERROR::FRAMEBUFFER:: Framebuffer is not complete!");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // desenhar como estrutura de arame
        // GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
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
        // primeira passada de renderização: textura de espelho. 
        // vincular ao framebuffer e desenhar na textura de cor como faríamos
        // normalmente, mas com a câmera de visualização invertida. 
        // vincular ao framebuffer e desenhar a cena na textura de cor como faríamos normalmente.
        // --------------------------------------------------
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        GL.Enable(EnableCap.DepthTest); // habilita o teste de profundidade (desabilitado para renderizar o quad no espaço da tela)

        // certifique-se de limpar o conteúdo do framebuffer
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

        Matrix4 model = Matrix4.Identity;

        _camera.Yaw += 180.0f; // rotaciona a guinada da câmera em 180 graus
        _camera.ProcessMouseMovement(0, 0, false); // chame isto para garantir que os vetores da câmera sejam atualizados; note que desativamos as restrições de pitch para este caso específico (caso contrário, não conseguimos inverter os valores de pitch da câmera)

        Matrix4 view = _camera.GetViewMatrix();
        _camera.Yaw -= 180.0f; // redefina-o para sua orientação original
        _camera.ProcessMouseMovement(0, 0, true);

        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(_camera.Zoom),
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT,
            depthNear: 0.1f,
            depthFar:  100.0f
        );

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

        GL.BindVertexArray(0);

        // segunda passada de renderização: desenhar normalmente
        // --------------------------------------------------
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        model = Matrix4.Identity;
        view = _camera.GetViewMatrix();

        _shader.SetMat4("view", view);

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

        GL.BindVertexArray(0);

        // agora desenhe o quadrilátero do espelho com a textura da tela
        // --------------------------------------------------
        GL.Disable(EnableCap.DepthTest); // desabilita o teste de profundidade para que o quad no espaço da tela não seja descartado pelo teste de profundidade.

        _screenShader.Use();

        GL.BindVertexArray(_quadVAO);

        GL.BindTexture(TextureTarget.Texture2D, _textureColorbuffer);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

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
        GL.DeleteVertexArrays(1, ref _quadVAO);
        GL.DeleteBuffers(1, ref _cubeVBO);
        GL.DeleteBuffers(1, ref _planeVBO);
        GL.DeleteBuffers(1, ref _quadVBO);
        GL.DeleteRenderbuffers(1, ref _rbo);
        GL.DeleteFramebuffers(1, ref _framebuffer);
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
