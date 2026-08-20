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
    private float _lastX = (float)SCR_WIDTH / 2.0f;
    private float _lastY = (float)SCR_HEIGHT / 2.0f;
    private bool _firstMouse = true;

    // timing
    private float _deltaTime = 0.0f;
    private float _lastFrame = 0.0f;

    // malhas
    private uint _planeVAO;

    private Shader _shader;
    private Shader _simpleDepthShader;
    private Shader _debugDepthQuad;

    private uint _planeVBO;

    private uint _woodTexture;

    private const int SHADOW_WIDTH = 1024, SHADOW_HEIGHT = 1024;
    private uint _depthMapFBO;
    private uint _depthMap;

    // informações de iluminação
    // --------------------------------------------------
    private Vector3 _lightPos = new Vector3(-2.0f, 4.0f, -1.0f);
    
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
        _shader = new Shader("src/shadow_mapping.vs", "src/shadow_mapping.fs");
        _simpleDepthShader = new Shader("src/shadow_mapping_depth.vs", "src/shadow_mapping_depth.fs");
        _debugDepthQuad = new Shader("src/debug_quad.vs", "src/debug_quad_depth.fs");
    }

    protected override void OnLoad()
    {
        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] planeVertices =
        {
            // posições              // normais          // coordenadas de textura
            -25.0f, -0.5f, -25.0f,   0.0f, 1.0f, 0.0f,    0.0f,  0.0f,
             25.0f, -0.5f, -25.0f,   0.0f, 1.0f, 0.0f,   25.0f,  0.0f,
             25.0f, -0.5f,  25.0f,   0.0f, 1.0f, 0.0f,   25.0f, 25.0f,
            -25.0f, -0.5f, -25.0f,   0.0f, 1.0f, 0.0f,    0.0f,  0.0f,
             25.0f, -0.5f,  25.0f,   0.0f, 1.0f, 0.0f,   25.0f, 25.0f,
            -25.0f, -0.5f,  25.0f,   0.0f, 1.0f, 0.0f,    0.0f, 25.0f
        };

        // plane VAO
        GL.GenVertexArrays(1, out _planeVAO);
        GL.GenBuffers(1, out _planeVBO);

        GL.BindVertexArray(_planeVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _planeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, planeVertices.Length * sizeof(float), planeVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

        GL.BindVertexArray(0);

        // carregar texturas
        // --------------------------------------------------
        _woodTexture = LoadTexture("res/textures/wood.png");

        // configurar FBO do mapa de profundidade
        // --------------------------------------------------
        GL.GenFramebuffers(1, out _depthMapFBO);

        // criar textura de profundidade
        GL.GenTextures(1, out _depthMap);
        GL.BindTexture(TextureTarget.Texture2D, _depthMap);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, SHADOW_WIDTH, SHADOW_HEIGHT, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColor);

        // anexa a textura de profundidade como buffer de profundidade do FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFBO);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthMap, 0);

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("diffuseTexture", 0);
        _shader.SetInt("shadowMap", 1);

        _debugDepthQuad.Use();
        _debugDepthQuad.SetInt("depthMap", 0);
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
        // alterar a posição da luz ao longo do tempo
        // _lightPos.X = MathF.Sin((float)GLFW.GetTime()) * 3.0f;
        // _lightPos.Z = MathF.Cos((float)GLFW.GetTime()) * 2.0f;
        // _lightPos.Y = 5.0f + MathF.Cos((float)GLFW.GetTime()) * 1.0f;

        // render
        // --------------------------------------------------
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 1. renderizar a profundidade da cena em uma textura (da perspectiva da luz)
        // --------------------------------------------------
        Matrix4 lightProjection, lightView;
        Matrix4 lightSpaceMatrix;
        float near_plane = 1.0f, far_plane = 7.5f;

        // lightProjection = Matrix4.CreatePerspectiveFieldOfView( // observe que, se você utilizar uma matriz de projeção em perspectiva, terá que alterar a posição da luz, pois a posição atual não é suficiente para refletir toda a cena
        //     fovy:      MathHelper.DegreesToRadians(45.0f), 
        //     aspect:    (float)SHADOW_WIDTH / (float)SHADOW_HEIGHT, 
        //     depthNear: near_plane, 
        //     depthFar:  far_plane
        // );

        lightProjection = Matrix4.CreateOrthographicOffCenter(
            left:      -10.0f, 
            right:      10.0f, 
            bottom:    -10.0f, 
            top:        10.0f, 
            depthNear:  near_plane, 
            depthFar:   far_plane
        );

        lightView = Matrix4.LookAt(
            eye:    _lightPos, 
            target: new Vector3(0.0f), 
            up:     new Vector3(0.0f, 1.0f, 0.0f)
        );

        lightSpaceMatrix = lightView * lightProjection;

        // renderizar a cena do ponto de vista da luz
        _simpleDepthShader.Use();
        _simpleDepthShader.SetMat4("lightSpaceMatrix", lightSpaceMatrix);

        GL.Viewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _woodTexture);
            RenderScene(_simpleDepthShader);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        // redefinir viewport
        GL.Viewport(0, 0, SCR_WIDTH, SCR_HEIGHT);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 2. renderizar a cena normalmente usando o mapa de profundidade/sombra gerado
        // --------------------------------------------------
        _shader.Use();
        
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(_camera.Zoom), 
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            depthNear: 0.1f, 
            depthFar:  100.0f
        );
        Matrix4 view = _camera.GetViewMatrix();

        _shader.SetMat4("projection", projection);
        _shader.SetMat4("view", view);

        // definir uniformes claros
        _shader.SetVec3("viewPos", _camera.Position);
        _shader.SetVec3("lightPos", _lightPos);
        _shader.SetMat4("lightSpaceMatrix", lightSpaceMatrix);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _woodTexture);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, _depthMap);

        RenderScene(_shader);

        // renderiza o mapa de profundidade em um quad para depuração visual
        // --------------------------------------------------
        _debugDepthQuad.Use();
        _debugDepthQuad.SetFloat("near_plane", near_plane);
        _debugDepthQuad.SetFloat("far_plane", far_plane);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _depthMap);
        // RenderQuad();

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        GL.DeleteVertexArrays(1, ref _planeVAO);
        GL.DeleteBuffers(1, ref _planeVBO);
    }

    // renderiza a cena 3D
    // --------------------------------------------------
    private void RenderScene(Shader shader)
    {
        // chão
        Matrix4 model = Matrix4.Identity;
        shader.SetMat4("model", model);
        GL.BindVertexArray(_planeVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        // cubos
        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.5f));
        model *= Matrix4.CreateTranslation(new Vector3(0.0f, 1.5f, 0.0f));
        shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.5f));
        model *= Matrix4.CreateTranslation(new Vector3(2.0f, 0.0f, 1.0f));
        shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.25f));
        model *= Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.0f, 1.0f), MathHelper.DegreesToRadians(60.0f));
        model *= Matrix4.CreateTranslation(new Vector3(-1.0f, 0.0f, 2.0f));
        shader.SetMat4("model", model);
        RenderCube();
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
                 1.0f, -1.0f, 0.0f,   1.0f, 0.0f,
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

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, pixelInternalFormat == PixelInternalFormat.Rgba ? (int)TextureWrapMode.ClampToEdge : (int)TextureWrapMode.Repeat); // para este tutorial: use GL_CLAMP_TO_EDGE para evitar bordas semitransparentes. Devido à interpolação, são amostrados texels da repetição seguinte.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, pixelInternalFormat == PixelInternalFormat.Rgba ? (int)TextureWrapMode.ClampToEdge : (int)TextureWrapMode.Repeat);
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
