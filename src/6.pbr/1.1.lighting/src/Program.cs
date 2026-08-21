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

    private Shader _shader;

    // luzes
    // --------------------------------------------------
    private Vector3[] _lightPositions =
    {
        new Vector3(-10.0f,  10.0f, 10.0f),
        new Vector3( 10.0f,  10.0f, 10.0f),
        new Vector3(-10.0f, -10.0f, 10.0f),
        new Vector3( 10.0f, -10.0f, 10.0f)
    };

    private Vector3[] _lightColors =
    {
        new Vector3(300.0f, 300.0f, 300.0f),
        new Vector3(300.0f, 300.0f, 300.0f),
        new Vector3(300.0f, 300.0f, 300.0f),
        new Vector3(300.0f, 300.0f, 300.0f)
    };

    private int _nrRows = 7;
    private int _nrColumns = 7;
    private float _spacing = 2.5f;
    
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
        _shader = new Shader("src/pbr.vs", "src/pbr.fs");
    }

    protected override void OnLoad()
    {
        // inicializar uniformes de shader estáticos antes da renderização
        // --------------------------------------------------
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            fovy:      MathHelper.DegreesToRadians(_camera.Zoom), 
            aspect:    (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            depthNear: 0.1f, 
            depthFar:  100.0f
        );

        _shader.Use();
        _shader.SetMat4("projection", projection);
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

        _shader.Use();

        Matrix4 view = _camera.GetViewMatrix();
        _shader.SetMat4("view", view);

        _shader.SetVec3("camPos", _camera.Position);

        // renderiza um número de esferas igual a linhas × colunas, com valores variáveis ​​de metalicidade/rugosidade escalonados pelas linhas e colunas, respectivamente
        Matrix4 model = Matrix4.Identity;

        for (int row = 0; row < _nrRows; row++)
        {
            _shader.SetFloat("metallic", (float)row / (float)_nrRows);

            for (int col = 0; col < _nrColumns; col++)
            {
                // Limitamos a rugosidade ao intervalo de 0,05 a 1,0, pois superfícies perfeitamente lisas (rugosidade de 0,0) tendem a parecer um pouco estranhas
                // sob iluminação direta.
                _shader.SetFloat("roughness", Math.Clamp((float)col / (float)_nrColumns, 0.05f, 1.0f));

                model = Matrix4.Identity;
                model *= Matrix4.CreateTranslation(new Vector3(
                    (col - (_nrColumns / 2)) * _spacing,
                    (row - (_nrRows / 2)) * _spacing,
                    0.0f
                ));
                _shader.SetMat4("model", model);

                _shader.SetMat3("normalMatrix", Matrix3.Transpose(Matrix3.Invert(new Matrix3(model))));  

                RenderSphere();              
            }
        }

        // renderiza a fonte de luz (simplesmente renderiza novamente a esfera nas posições da luz)
        // isso parece um pouco estranho, já que usamos o mesmo shader, mas torna as posições evidentes e
        // mantém o código conciso.
        for (int i = 0; i < _lightPositions.Length; i++)
        {
            Vector3 newPos = _lightPositions[i];
            newPos *= _lightPositions[i] + new Vector3(MathF.Sin((float)GLFW.GetTime() * 5.0f) * 5.0f, 0.0f, 0.0f);
            
            _shader.SetVec3($"lightPositions[{i}]", newPos);
            _shader.SetVec3($"lightColors[{i}]", _lightColors[i]);

            model = Matrix4.Identity;
            model *= Matrix4.CreateScale(new Vector3(0.5f));
            model *= Matrix4.CreateTranslation(newPos);
            _shader.SetMat4("model", model);

            _shader.SetMat3("normalMatrix", Matrix3.Transpose(Matrix3.Invert(new Matrix3(model))));

            RenderSphere();
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

    // renderiza (e constrói na primeira invocação) uma esfera
    // --------------------------------------------------
    private uint _sphereVAO = 0;
    private uint _indexCount;

    private void RenderSphere()
    {
        if (_sphereVAO == 0)
        {
            GL.GenVertexArrays(1, out _sphereVAO);

            uint vbo, ebo;
            GL.GenBuffers(1, out vbo);
            GL.GenBuffers(1, out ebo);

            List<Vector3> positions = [];
            List<Vector2> uv = [];
            List<Vector3> normals = [];
            List<uint> indices = [];

            const uint X_SEGMENTS = 64;
            const uint Y_SEGMENTS = 64;
            const float PI = MathF.PI;

            for (int x = 0; x < X_SEGMENTS; x++)
            {
                for (int y = 0; y < Y_SEGMENTS; y++)
                {
                    float xSegments = (float)x / (float)X_SEGMENTS;
                    float ySegments = (float)y / (float)Y_SEGMENTS;

                    float xPos = MathF.Cos(xSegments * 2.0f * PI) * MathF.Sin(ySegments * PI);
                    float yPos = MathF.Cos(ySegments * PI);
                    float zPos = MathF.Sin(xSegments * 2.0f * PI) * MathF.Sin(ySegments * PI);

                    positions.Add(new Vector3(xPos, yPos, zPos));
                    uv.Add(new Vector2(xSegments, ySegments));
                    normals.Add(new Vector3(xPos, yPos, zPos));
                }
            }

            bool oddRow = false;

            for (int y = 0; y < Y_SEGMENTS; y++)
            {
                if (!oddRow) // linhas pares: y == 0, y == 2; e assim por diante
                {
                    for (int x = 0; x < X_SEGMENTS; x++)
                    {
                        indices.Add((uint)(y       * (X_SEGMENTS + 1) + x));
                        indices.Add((uint)((y + 1) * (X_SEGMENTS + 1) + x));
                    }
                }
                else
                {
                    for (int x = (int)X_SEGMENTS; x >= 0; x--)
                    {
                        indices.Add((uint)((y + 1) * (X_SEGMENTS + 1) + x));
                        indices.Add((uint)(y       * (X_SEGMENTS + 1) + x));
                    }
                }

                oddRow = !oddRow;
            }

            _indexCount = (uint)indices.Count();

            List<float> data = [];

            for (int i = 0; i < positions.Count(); i++)
            {
                data.Add(positions[i].X);
                data.Add(positions[i].Y);
                data.Add(positions[i].Z);

                if (normals.Count() > 0)
                {
                    data.Add(normals[i].X);
                    data.Add(normals[i].Y);
                    data.Add(normals[i].Z);
                }
                if (uv.Count() > 0)
                {
                    data.Add(uv[i].X);
                    data.Add(uv[i].Y);
                }
            }

            GL.BindVertexArray(_sphereVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count() * sizeof(float), (int)data[0], BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count() * sizeof(uint), (int)indices[0], BufferUsageHint.StaticDraw);

            int stride = (3 + 2 + 3) * sizeof(float);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        }

        GL.BindVertexArray(_sphereVAO);
        GL.DrawElements(PrimitiveType.TriangleStrip, (int)_indexCount, DrawElementsType.UnsignedInt, 0);
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
