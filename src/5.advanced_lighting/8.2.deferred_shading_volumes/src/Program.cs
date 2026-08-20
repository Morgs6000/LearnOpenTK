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
    private Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 5.0f));
    private float _lastX = (float)SCR_WIDTH / 2.0f;
    private float _lastY = (float)SCR_HEIGHT / 2.0f;
    private bool _firstMouse = true;

    // timing
    private float _deltaTime = 0.0f;
    private float _lastFrame = 0.0f;

    private Shader _shaderGeometryPass;
    private Shader _shaderLightingPass;
    private Shader _shaderLightBox;

    private Model _backpack;

    private List<Vector3> _objectPositions = [];

    private uint _gBuffer;
    private uint _gPosition, _gNormal, _gAlbedoSpec;

    private DrawBuffersEnum[] _attachments = new DrawBuffersEnum[3];
    private uint _rboDepth;

    private const uint NR_LIGHTS = 32;
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

        // instrua a stb_image.h a inverter as texturas carregadas no eixo Y (antes de carregar o modelo).
        StbImage.stbi_set_flip_vertically_on_load(1);

        // configurar estado global do OpenGL
        // --------------------------------------------------
        GL.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shaderGeometryPass = new Shader("src/g_buffer.vs", "src/g_buffer.fs");
        _shaderLightingPass = new Shader("src/deferred_shading.vs", "src/deferred_shading.fs");
        _shaderLightBox = new Shader("src/deferred_light_box.vs", "src/deferred_light_box.fs");

        // carregar modelos
        // --------------------------------------------------
        _backpack = new Model("res/objects/backpack/backpack.obj");
    }

    protected override void OnLoad()
    {
        _objectPositions.Add(new Vector3(-3.0f,  -0.5f, -3.0f));
        _objectPositions.Add(new Vector3( 0.0f,  -0.5f, -3.0f));
        _objectPositions.Add(new Vector3( 3.0f,  -0.5f, -3.0f));
        _objectPositions.Add(new Vector3(-3.0f,  -0.5f,  0.0f));
        _objectPositions.Add(new Vector3( 0.0f,  -0.5f,  0.0f));
        _objectPositions.Add(new Vector3( 3.0f,  -0.5f,  0.0f));
        _objectPositions.Add(new Vector3(-3.0f,  -0.5f,  3.0f));
        _objectPositions.Add(new Vector3( 0.0f,  -0.5f,  3.0f));
        _objectPositions.Add(new Vector3( 3.0f,  -0.5f,  3.0f));

        // configurar o framebuffer do g-buffer
        // --------------------------------------------------
        GL.GenFramebuffers(1, out _gBuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);

        // buffer de cor de posição
        GL.GenTextures(1, out _gPosition);
        GL.BindTexture(TextureTarget.Texture2D, _gPosition);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gPosition, 0);

        // buffer de cor normal
        GL.GenTextures(1, out _gNormal);
        GL.BindTexture(TextureTarget.Texture2D, _gNormal);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gNormal, 0);

        // buffer de cor + cor especular
        GL.GenTextures(1, out _gAlbedoSpec);
        GL.BindTexture(TextureTarget.Texture2D, _gAlbedoSpec);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gAlbedoSpec, 0);

        // informar ao OpenGL quais anexos de cor (deste framebuffer) usaremos para a renderização
        _attachments = new DrawBuffersEnum[3] { (DrawBuffersEnum)FramebufferAttachment.ColorAttachment0, (DrawBuffersEnum)FramebufferAttachment.ColorAttachment1, (DrawBuffersEnum)FramebufferAttachment.ColorAttachment2 };

        GL.DrawBuffers(3, _attachments);

        // criar e anexar buffer de profundidade (renderbuffer)
        GL.GenRenderbuffers(1, out _rboDepth);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rboDepth);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, SCR_WIDTH, SCR_HEIGHT);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rboDepth);

        // finalmente, verifica se o framebuffer está completo
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer not complete!");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // informações de iluminação
        // --------------------------------------------------
        Random srand = new Random(13);

        for (int i = 0; i < NR_LIGHTS; i++)
        {
            // calcular deslocamentos levemente aleatórios
            float xPos = (float)(((srand.Next() % 100) / 100.0f) * 6.0f - 3.0f);
            float yPos = (float)(((srand.Next() % 100) / 100.0f) * 6.0f - 4.0f);
            float zPos = (float)(((srand.Next() % 100) / 100.0f) * 6.0f - 3.0f);

            _lightPositions.Add(new Vector3(xPos, yPos, zPos));

            float rColor = (float)(((srand.Next() % 100) / 200.0f) + 0.5f); // entre 0,5 e 1,0
            float gColor = (float)(((srand.Next() % 100) / 200.0f) + 0.5f); // entre 0,5 e 1,0
            float bColor = (float)(((srand.Next() % 100) / 200.0f) + 0.5f); // entre 0,5 e 1,0

            _lightColors.Add(new Vector3(rColor, gColor, bColor));
        }

        // configuração do shader
        // --------------------------------------------------
        _shaderLightingPass.Use();
        _shaderLightingPass.SetInt("gPosition", 0);
        _shaderLightingPass.SetInt("gNormal", 1);
        _shaderLightingPass.SetInt("gAlbedoSpec", 2);
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
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                fovy:      MathHelper.DegreesToRadians(_camera.Zoom), 
                aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT, 
                depthNear: 0.1f, 
                depthFar:  100.0f
            );
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 model = Matrix4.Identity;

            _shaderGeometryPass.Use();
            _shaderGeometryPass.SetMat4("projection", projection);
            _shaderGeometryPass.SetMat4("view", view);

            for (int i = 0; i < _objectPositions.Count(); i++)
            {
                model = Matrix4.Identity;
                model *= Matrix4.CreateScale(new Vector3(0.5f));
                model *= Matrix4.CreateTranslation(_objectPositions[i]);
                _shaderGeometryPass.SetMat4("model", model);

                _backpack.Draw(_shaderGeometryPass);
            }
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 2. passo de iluminação: calcular a iluminação iterando pixel a pixel sobre um quadrilátero que preenche a tela, utilizando o conteúdo do G-buffer.
        // --------------------------------------------------
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shaderLightingPass.Use();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _gPosition);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, _gNormal);

        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2D, _gAlbedoSpec);

        // enviar uniformes leves e adequados
        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            _shaderLightingPass.SetVec3($"lights[{i}].Position", _lightPositions[i]);
            _shaderLightingPass.SetVec3($"lights[{i}].Color", _lightColors[i]);

            // atualizar parâmetros de atenuação e calcular o raio
            const float constant = 1.0f; // observe que não enviamos isso para o shader; assumimos que é sempre 1.0 (no nosso caso)
            const float linear = 0.7f;
            const float quadratic = 1.8f;

            _shaderLightingPass.SetFloat($"lights[{i}].Linear", linear);
            _shaderLightingPass.SetFloat($"lights[{i}].Quadratic", quadratic);

            // então, calcule o raio do volume de luz/esfera
            float maxBrightness = MathF.Max(MathF.Max(_lightColors[i].X, _lightColors[i].Y), _lightColors[i].Z);
            float radius = (-linear + MathF.Sqrt(linear * linear - 4 * quadratic * (constant - (256.0f / 5.0f) * maxBrightness))) / (2.0f * quadratic);

            _shaderLightingPass.SetFloat($"lights[{i}].Radius", radius);
        }

        _shaderLightingPass.SetVec3("viewPos", _camera.Position);

        // finalmente renderiza o quadrilátero
        RenderQuad();

        // 2.5. copiar o conteúdo do buffer de profundidade da geometria para o buffer de profundidade do framebuffer padrão
        // --------------------------------------------------
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _gBuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0); // gravar no framebuffer padrão

        // Realiza o blit para o framebuffer padrão. Observe que isso pode ou não funcionar, pois os formatos internos do FBO e do framebuffer padrão precisam ser compatíveis. 
        // Os formatos internos são definidos pela implementação. Isso funciona em todos os meus sistemas, mas, se não funcionar no seu, provavelmente você terá que gravar no
        // buffer de profundidade em outro estágio do shader (ou, de alguma forma, fazer com que o formato interno do framebuffer padrão corresponda ao formato interno do FBO).
        GL.BlitFramebuffer(0, 0, SCR_WIDTH, SCR_HEIGHT, 0, 0, SCR_WIDTH, SCR_HEIGHT, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 3. renderizar luzes sobre a cena
        // --------------------------------------------------
        _shaderLightBox.Use();
        _shaderLightBox.SetMat4("projection", projection);
        _shaderLightBox.SetMat4("view", view);

        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            model = Matrix4.Identity;
            model *= Matrix4.CreateScale(new Vector3(0.125f));
            model *= Matrix4.CreateTranslation(_lightPositions[i]);
            _shaderLightBox.SetMat4("model", model);
            
            _shaderLightBox.SetVec3("lightColor", _lightColors[i]);

            RenderCube();
        }

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
