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
    private bool _bloom = true;
    private float _exposure = 1.0f;

    // câmera
    private Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 5.0f));
    private float _lastX = (float)SCR_WIDTH / 2.0f;
    private float _lastY = (float)SCR_HEIGHT / 2.0f;
    private bool _firstMouse = true;

    // timing
    private float _deltaTime = 0.0f;
    private float _lastFrame = 0.0f;

    private Shader _shader;
    private Shader _shaderLight;
    private Shader _shaderBlur;
    private Shader _shaderBloomFinal;

    private uint _woodTexture;
    private uint _containerTexture;

    private uint _hdrFBO;
    private uint[] _colorBuffers = new uint[2];

    private uint _rboDepth;
    private DrawBuffersEnum[] _attachments = new DrawBuffersEnum[2];

    private uint[] _pingpongFBO = new uint[2];
    private uint[] _pingpongColorbuffers = new uint[2];

    private List<Vector3> _lightPositions = [];
    private List<Vector3> _lightColors = [];
    
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
        _shader = new Shader("src/bloom.vs", "src/bloom.fs");
        _shaderLight = new Shader("src/bloom.vs", "src/light_box.fs");
        _shaderBlur = new Shader("src/blur.vs", "src/blur.fs");
        _shaderBloomFinal = new Shader("src/bloom_final.vs", "src/bloom_final.fs");
    }

    protected override void OnLoad()
    {
        // carregar texturas
        // --------------------------------------------------
        _woodTexture = LoadTexture("res/textures/wood.png", true); // observe que estamos carregando a textura como uma textura sRGB
        _containerTexture = LoadTexture("res/textures/container2.png", true); // observe que estamos carregando a textura como uma textura sRGB

        // configurar buffers de quadro (ponto flutuante)
        // --------------------------------------------------
        GL.GenFramebuffers(1, out _hdrFBO);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _hdrFBO);

        // cria 2 buffers de cor de ponto flutuante (um para renderização normal, outro para valores de limiar de brilho)
        GL.GenTextures(2, _colorBuffers);

        for (int i = 0; i < 2; i++)
        {
            GL.BindTexture(TextureTarget.Texture2D, _colorBuffers[i]);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); // Limitamos à borda, pois, caso contrário, o filtro de desfoque amostraria valores de textura repetidos!
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // anexar textura ao framebuffer
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, _colorBuffers[i], 0);
        }

        // criar e anexar buffer de profundidade (renderbuffer)
        GL.GenRenderbuffers(1, out _rboDepth);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rboDepth);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, SCR_WIDTH, SCR_HEIGHT);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rboDepth);

        // informar ao OpenGL quais anexos de cor (deste framebuffer) usaremos para a renderização
        _attachments = new DrawBuffersEnum[2]{ (DrawBuffersEnum)FramebufferAttachment.ColorAttachment0, (DrawBuffersEnum)FramebufferAttachment.ColorAttachment1 };

        GL.DrawBuffers(2, _attachments);

        // finalmente, verifica se o framebuffer está completo
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer not complete!");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // framebuffer ping-pong para desfoque
        GL.GenFramebuffers(2, _pingpongFBO);
        GL.GenTextures(2, _pingpongColorbuffers);

        for (int i = 0; i < 2; i++)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _pingpongFBO[i]);
            GL.BindTexture(TextureTarget.Texture2D, _pingpongColorbuffers[i]);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); // Limitamos à borda, pois, caso contrário, o filtro de desfoque amostraria valores de textura repetidos!
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _pingpongColorbuffers[i], 0);

            // verifique também se os framebuffers estão completos (não é necessário buffer de profundidade)
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer not complete!");
            }
        }

        // informações de iluminação
        // --------------------------------------------------

        // posições
        _lightPositions.Add(new Vector3( 0.0f, 0.5f,  1.5f));
        _lightPositions.Add(new Vector3(-4.0f, 0.5f, -3.0f));
        _lightPositions.Add(new Vector3( 3.0f, 0.5f,  1.0f));
        _lightPositions.Add(new Vector3(-0.8f, 2.4f, -1.0f));

        // cores
        _lightColors.Add(new Vector3(5.0f,   5.0f,  5.0f));
        _lightColors.Add(new Vector3(10.0f,  0.0f,  0.0f));
        _lightColors.Add(new Vector3(0.0f,   0.0f,  15.0f));
        _lightColors.Add(new Vector3(0.0f,   5.0f,  0.0f));

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("diffuseTexture", 0);

        _shaderBlur.Use();
        _shaderBlur.SetInt("image", 0);

        _shaderBloomFinal.Use();
        _shaderBloomFinal.SetInt("scene", 0);
        _shaderBloomFinal.SetInt("bloomBlur", 1);
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
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 1. renderizar a cena em um framebuffer de ponto flutuante
        // --------------------------------------------------
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _hdrFBO);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(_camera.Zoom), 
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            depthNear: 0.1f, 
            depthFar:  100.0f
        );
        Matrix4 view = _camera.GetViewMatrix();

        _shader.Use();
        _shader.SetMat4("projection", projection);
        _shader.SetMat4("view", view);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _woodTexture);

        // definir uniforms de iluminação
        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            _shader.SetVec3($"lights[{i}].Position", _lightPositions[i]);
            _shader.SetVec3($"lights[{i}].Color", _lightColors[i]);
        }
        _shader.SetVec3("viewPos", _camera.Position);

        // cria um cubo grande que serve como chão
        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(12.5f, 0.5f, 12.5f));
        model *= Matrix4.CreateTranslation(new Vector3(0.0f, -1.0f, 0.0f));
        _shader.SetMat4("model", model);
        RenderCube();
        
        // em seguida, crie vários cubos para compor o cenário
        GL.BindTexture(TextureTarget.Texture2D, _containerTexture);

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.5f));
        model *= Matrix4.CreateTranslation(new Vector3(0.0f, 1.5f, 0.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.5f));
        model *= Matrix4.CreateTranslation(new Vector3(2.0f, 0.0f, 1.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4.Identity;
        model *= Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.0f, 1.0f), MathHelper.DegreesToRadians(60.0f));
        model *= Matrix4.CreateTranslation(new Vector3(-1.0f, -1.0f, 2.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(1.25f));
        model *= Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.0f, 1.0f), MathHelper.DegreesToRadians(23.0f));
        model *= Matrix4.CreateTranslation(new Vector3(0.0f, 2.7f, 4.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4.Identity;
        model *= Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.0f, 1.0f), MathHelper.DegreesToRadians(124.0f));
        model *= Matrix4.CreateTranslation(new Vector3(-2.0f, 1.0f, -3.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.5f));
        model *= Matrix4.CreateTranslation(new Vector3(-3.0f, 0.0f, 0.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        // finalmente mostra todas as fontes de luz como cubos brilhantes
        _shaderLight.Use();
        _shaderLight.SetMat4("projection", projection);
        _shaderLight.SetMat4("view", view);

        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            model = Matrix4.Identity;
            model *= Matrix4.CreateScale(new Vector3(0.25f));
            model *= Matrix4.CreateTranslation(_lightPositions[i]);
            _shaderLight.SetMat4("model", model);

            _shaderLight.SetVec3("lightColor", _lightColors[i]);

            RenderCube();
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 2. aplicar desfoque gaussiano de duas passagens aos fragmentos brilhantes
        // --------------------------------------------------
        bool horizontal = true, first_iteration = true;
        uint amount = 10;

        _shaderBlur.Use();

        for (int i = 0; i < amount; i++)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _pingpongFBO[horizontal ? 1 : 0]);

            _shaderBlur.SetInt("horizontal", horizontal ? 1 : 0);

            GL.BindTexture(TextureTarget.Texture2D, first_iteration ? _colorBuffers[1] : _pingpongColorbuffers[horizontal ? 0 : 1]); // vincula a textura do outro framebuffer (ou da cena, se for a primeira iteração)

            RenderQuad();

            horizontal = !horizontal;

            if (first_iteration)
            {
                first_iteration = false;
            }
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 3. agora, renderize o buffer de cores de ponto flutuante em um quad 2D e aplique o mapeamento de tons (tonemapping) das cores HDR para o intervalo de cores (limitado) do framebuffer padrão
        // --------------------------------------------------
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shaderBloomFinal.Use();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _colorBuffers[0]);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, _pingpongColorbuffers[horizontal ? 0 : 1]);

        _shaderBloomFinal.SetInt("bloom", _bloom ? 1 : 0);
        _shaderBloomFinal.SetFloat("exposure", _exposure);

        RenderQuad();

        Console.WriteLine((_bloom ? "on" : "off") + "| exposure: " + _exposure);

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        
    }

    // renderCube() renderiza um cubo 3D de 1x1 em NDC.
    // --------------------------------------------------
    private uint _cubeVAO = 0;
    private uint _cubeVBO = 0;

    private void RenderCube()
    {
        // inicializar (se necessário)
        if (_cubeVAO == 0)
        {
            float[] vertices =
            {
                // posições            // normais             // coordenadas de textura

                // face esquerda
                -1.0f, -1.0f, -1.0f,   -1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                -1.0f, -1.0f,  1.0f,   -1.0f,  0.0f,  0.0f,   1.0f, 0.0f,
                -1.0f,  1.0f,  1.0f,   -1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                -1.0f, -1.0f, -1.0f,   -1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                -1.0f,  1.0f,  1.0f,   -1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                -1.0f,  1.0f, -1.0f,   -1.0f,  0.0f,  0.0f,   0.0f, 1.0f,
                
                // face direita
                 1.0f, -1.0f,  1.0f,    1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                 1.0f, -1.0f, -1.0f,    1.0f,  0.0f,  0.0f,   1.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                 1.0f, -1.0f,  1.0f,    1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                 1.0f,  1.0f,  1.0f,    1.0f,  0.0f,  0.0f,   0.0f, 1.0f,
                
                // face inferior
                -1.0f, -1.0f, -1.0f,    0.0f, -1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f, -1.0f, -1.0f,    0.0f, -1.0f,  0.0f,   1.0f, 0.0f,
                 1.0f, -1.0f,  1.0f,    0.0f, -1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f, -1.0f, -1.0f,    0.0f, -1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f, -1.0f,  1.0f,    0.0f, -1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f, -1.0f,  1.0f,    0.0f, -1.0f,  0.0f,   0.0f, 1.0f,
                
                // face superior
                -1.0f,  1.0f,  1.0f,    0.0f,  1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f,  1.0f,    0.0f,  1.0f,  0.0f,   1.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    0.0f,  1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f,  1.0f,  1.0f,    0.0f,  1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    0.0f,  1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f,  1.0f, -1.0f,    0.0f,  1.0f,  0.0f,   0.0f, 1.0f,
                
                // face posterior
                 1.0f, -1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   0.0f, 0.0f,
                -1.0f, -1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   1.0f, 0.0f,
                -1.0f,  1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   1.0f, 1.0f,
                 1.0f, -1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   0.0f, 0.0f,
                -1.0f,  1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   1.0f, 1.0f,
                 1.0f,  1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   0.0f, 1.0f,
                
                // face frontal
                -1.0f, -1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   0.0f, 0.0f,
                 1.0f, -1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   1.0f, 0.0f,
                 1.0f,  1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   1.0f, 1.0f,
                -1.0f, -1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   0.0f, 0.0f,
                 1.0f,  1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   1.0f, 1.0f,
                -1.0f,  1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   0.0f, 1.0f
            };

            GL.GenVertexArrays(1, out _cubeVAO);
            GL.GenBuffers(1, out _cubeVBO);

            // preencher buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // vincular atributos de vértice
            GL.BindVertexArray(_cubeVAO);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        // renderizar cubo
        GL.BindVertexArray(_cubeVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        GL.BindVertexArray(0);
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
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
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

        if (KeyboardState.IsKeyPressed(Keys.Space))
        {
            _bloom = !_bloom;
        }

        if (KeyboardState.IsKeyDown(Keys.Q))
        {
            if (_exposure > 0.0f)
            {
                _exposure -= 0.0001f;
            }
            else
            {
                _exposure = 0.0f;
            }
        }
        else if (KeyboardState.IsKeyDown(Keys.E))
        {
            _exposure += 0.0001f;
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
    private uint LoadTexture(string path, bool gammaCorrection)
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
                pixelInternalFormat = gammaCorrection ? PixelInternalFormat.Srgb : PixelInternalFormat.Rgb;
                pixelFormat = PixelFormat.Rgb;
            }
            else if (image.Comp == ColorComponents.RedGreenBlueAlpha)
            {
                pixelInternalFormat = gammaCorrection ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba;
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
