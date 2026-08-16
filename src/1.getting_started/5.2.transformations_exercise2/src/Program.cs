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

    private Shader _ourShader;

    private uint _vertexArrayObject;
    private uint _vertexBufferObject;
    private uint _elementBufferObject;

    private uint _texture1, _texture2;
    
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
        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _ourShader = new Shader("src/transform.vs", "src/transform.fs"); // você pode nomear seus arquivos de shader como quiser
    }

    protected override void OnLoad()
    {
        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] vertices =
        {
            // posições            // coordenadas de textura
            -0.5f, -0.5f,  0.0f,   0.0f, 0.0f, // inferior esquerdo
             0.5f, -0.5f,  0.0f,   1.0f, 0.0f, // inferior direito
             0.5f,  0.5f,  0.0f,   1.0f, 1.0f, // superior direito
            -0.5f,  0.5f,  0.0f,   0.0f, 1.0f  // superior esquerdo
        };

        uint[] indices =
        {
            0, 1, 2, // primeiro triângulo
            0, 2, 3  // segundo triângulo
        };

        GL.GenVertexArrays(1, out _vertexArrayObject);
        GL.GenBuffers(1, out _vertexBufferObject);
        GL.GenBuffers(1, out _elementBufferObject);

        GL.BindVertexArray(_vertexArrayObject);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // atributo de posição
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // atributo de coordenada de textura
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // carregar e criar uma textura
        // --------------------------------------------------

        // textura 1
        // --------------------------------------------------
        GL.GenTextures(1, out _texture1);
        GL.BindTexture(TextureTarget.Texture2D, _texture1); // todas as operações GL_TEXTURE_2D subsequentes agora afetam este objeto de textura

        // define os parâmetros de repetição da textura
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat); // define o modo de repetição da textura como GL_REPEAT (método padrão)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // definir parâmetros de filtragem de textura
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // carregar imagem, criar textura e gerar mipmaps
        int width, height;
        byte[] data;

        StbImage.stbi_set_flip_vertically_on_load(1); // instrui a stb_image.h a inverter as texturas carregadas no eixo Y.

        using (FileStream stream = File.OpenRead("res/textures/container.jpg"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura");
        }

        // textura 2
        // --------------------------------------------------
        GL.GenTextures(1, out _texture2);
        GL.BindTexture(TextureTarget.Texture2D, _texture2);

        // define os parâmetros de repetição da textura
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat); // define o modo de repetição da textura como GL_REPEAT (método padrão)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // definir parâmetros de filtragem de textura
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // carregar imagem, criar textura e gerar mipmaps
        using (FileStream stream = File.OpenRead("res/textures/awesomeface.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            // observe que o awesomeface.png possui transparência e, portanto, um canal alfa; certifique-se de informar ao OpenGL que o tipo de dado é GL_RGBA
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura");
        }

        // informar ao OpenGL, para cada sampler, a qual unidade de textura ele pertence (isso só precisa ser feito uma vez)
        // --------------------------------------------------
        _ourShader.Use();
        _ourShader.SetInt("texture1", 0);
        _ourShader.SetInt("texture2", 1);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        FramebufferSizeCallback(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        // input
        // --------------------------------------------------
        ProcessInput();
    }

    // loop de renderização
    // --------------------------------------------------
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // render
        // --------------------------------------------------
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // vincular textura
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture1);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, _texture2);

        Matrix4 transform = Matrix4.Identity; // certifique-se de inicializar a matriz como a matriz identidade primeiro

        // primeiro contêiner
        // --------------------------------------------------
        transform *= Matrix4.CreateFromAxisAngle(new Vector3(0.0f, 0.0f, 1.0f), (float)GLFW.GetTime());
        transform *= Matrix4.CreateTranslation(new Vector3(0.5f, -0.5f, 0.0f));
        int transformLoc = GL.GetUniformLocation(_ourShader.ID, "transform");
        GL.UniformMatrix4(transformLoc, false, ref transform);

        // com a matriz uniforme definida, desenhe o primeiro contêiner
        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

        // segunda transformação
        // --------------------------------------------------
        transform = Matrix4.Identity; // redefini-la para a matriz identidade
        float scaleAmount = MathF.Sin((float)GLFW.GetTime());
        transform *= Matrix4.CreateScale(new Vector3(scaleAmount, scaleAmount, scaleAmount));
        transform *= Matrix4.CreateTranslation(new Vector3(-0.5f, 0.5f, 0.0f));
        GL.UniformMatrix4(transformLoc, false, ref transform);

        // agora, com a matriz uniform sendo substituída por novas transformações, desenhe-o novamente.
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

        // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
        // --------------------------------------------------
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        GL.DeleteVertexArrays(1, ref _vertexArrayObject);
        GL.DeleteBuffers(1, ref _vertexBufferObject);
        GL.DeleteBuffers(1, ref _elementBufferObject);
    }

    // processar toda a entrada: consultar a GLFW para saber se teclas relevantes foram pressionadas ou liberadas neste quadro e reagir de acordo
    // --------------------------------------------------
    private void ProcessInput()
    {
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            Close();
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
}
