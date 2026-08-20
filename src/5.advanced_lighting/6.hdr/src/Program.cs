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
    private bool _hdr = true;
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
    private Shader _hdrShader;

    private uint _woodTexture;

    private uint _hdrFBO;
    private uint _colorBuffer;
    private uint _rbgDepth;

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
        _shader = new Shader("src/lighting.vs", "src/lighting.fs");
        _hdrShader = new Shader("src/hdr.vs", "src/hdr.fs");
    }

    protected override void OnLoad()
    {
        // carregar texturas
        // --------------------------------------------------
        _woodTexture = LoadTexture("res/textures/wood.png", true); // observe que estamos carregando a textura como uma textura sRGB

        // configurar framebuffer de ponto flutuante
        // --------------------------------------------------
        GL.GenFramebuffers(1, out _hdrFBO);

        // criar buffer de cor de ponto flutuante
        GL.GenTextures(1, out _colorBuffer);
        GL.BindTexture(TextureTarget.Texture2D, _colorBuffer);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // criar buffer de profundidade (renderbuffer)
        GL.GenRenderbuffers(1, out _rbgDepth);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbgDepth);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, SCR_WIDTH, SCR_HEIGHT);

        // anexar buffers
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _hdrFBO);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorBuffer, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rbgDepth);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer not complete!");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);        

        // informações de iluminação
        // --------------------------------------------------
        
        // posições
        _lightPositions.Add(new Vector3( 0.0f,  0.0f, 49.5f)); // luz de fundo
        _lightPositions.Add(new Vector3(-1.4f, -1.9f, 9.0f));
        _lightPositions.Add(new Vector3( 0.0f, -1.8f, 4.0f));
        _lightPositions.Add(new Vector3( 0.8f, -1.7f, 6.0f));

        // cores
        _lightColors.Add(new Vector3(200.0f, 200.0f, 200.0f));
        _lightColors.Add(new Vector3(0.1f, 0.0f, 0.0f));
        _lightColors.Add(new Vector3(0.0f, 0.0f, 0.2f));
        _lightColors.Add(new Vector3(0.0f, 0.1f, 0.0f));

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("diffuseTexture", 0);

        _hdrShader.Use();
        _hdrShader.SetInt("hdrBuffer", 0);
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

            // renderizar túnel
            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(new Vector3(2.5f, 2.5f, 27.5f));
            model *= Matrix4.CreateTranslation(new Vector3(0.0f, 0.0f, 25.0f));
            _shader.SetMat4("model", model);

            _shader.SetInt("inverse_normals", 1);

            RenderCube();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 2. agora, renderize o buffer de cor de ponto flutuante em um quad 2D e aplique tonemapping às cores HDR para o intervalo de cores (limitado) do framebuffer padrão
        // --------------------------------------------------
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _hdrShader.Use();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _colorBuffer);

        _hdrShader.SetInt("hdr", _hdr ? 1 : 0);
        _hdrShader.SetFloat("exposure", _exposure);

        RenderQuad();

        Console.WriteLine((_hdr ? "on" : "off") + "| exposure: " + _exposure);

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
            _hdr = !_hdr;
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
