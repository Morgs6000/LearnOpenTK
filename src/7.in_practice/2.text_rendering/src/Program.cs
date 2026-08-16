using System.Runtime.InteropServices;
using FreeTypeSharp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;

namespace LearnOpenTK.src;

public class Program : GameWindow
{
    // configurações
    private const int SCR_WIDTH = 800;
    private const int SCR_HEIGHT = 600;

    // Armazena todas as informações de estado relevantes para um caractere, conforme carregado usando o FreeType.
    public struct Character
    {
        public uint TextureID;   // Identificador da textura do glifo
        public Vector2i Size;    // Tamanho do glifo
        public Vector2i Bearing; // Deslocamento da linha de base até a esquerda/topo do glifo
        public uint Advance;     // Deslocamento horizontal para avançar para o próximo glifo
    }

    private Dictionary<uint, Character> Characters = [];
    private uint _vertexArrayObject;
    private uint _vertexBufferObject;

    private Shader _shader;

    private uint _texture;
    
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
        // configurar estado global do OpenGL
        // --------------------------------------------------
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader("src/text.vs", "src/text.fs");
    }

    protected override void OnLoad()
    {
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
            left:       0.0f, 
            right:      (float)SCR_WIDTH, 
            bottom:     0.0f, 
            top:        (float)SCR_HEIGHT, 
            depthNear: -1.0f, 
            depthFar:   1.0f
        );

        _shader.Use();
        GL.UniformMatrix4(GL.GetUniformLocation(_shader.ID, "projection"), false, ref projection);

        // FreeType
        // --------------------------------------------------
        unsafe
        {
            FT_LibraryRec_* ft;

            // Todas as funções retornam um valor diferente de 0 sempre que ocorre um erro
            if (FT_Init_FreeType(&ft) != 0)
            {
                Console.WriteLine("ERROR::FREETYPE: Could not init FreeType Library");
                return;
            }

            // encontrar caminho para a fonte
            string font_name = "res/fonts/Antonio-Bold.ttf";

            if (font_name == string.Empty)
            {
                Console.WriteLine("ERROR::FREETYPE: Failed to load font_name");
                return;
            }

            // carregar fonte como face
            FT_FaceRec_* face;

            if (FT_New_Face(ft, (byte*)Marshal.StringToHGlobalAnsi(font_name), 0, &face) != 0)
            {
                Console.WriteLine("ERROR::FREETYPE: Failed to load font");
                return;
            }
            else
            {
                // Defina o tamanho para carregar os glifos como
                FT_Set_Pixel_Sizes(face, 0, 48);

                // desativar a restrição de alinhamento de bytes
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                // carrega os primeiros 128 caracteres do conjunto ASCII
                for (uint c = 0; c < 128; c++)
                {
                    // Carregar glifo do caractere
                    if (FT_Load_Char(face, c, FT_LOAD_RENDER) != 0)
                    {
                        Console.WriteLine("ERROR::FREETYTPE: Failed to load Glyph");
                        continue;
                    }

                    // gerar textura
                    GL.GenTextures(1, out _texture);
                    GL.BindTexture(TextureTarget.Texture2D, _texture);
                    GL.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        PixelInternalFormat.CompressedRed,
                        (int)face->glyph->bitmap.width,
                        (int)face->glyph->bitmap.rows,
                        0,
                        PixelFormat.Red,
                        PixelType.UnsignedByte,
                        (IntPtr)face->glyph->bitmap.buffer
                    );

                    // definir opções de textura
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                    // definir opções de textura
                    Character character = new Character()
                    {
                        TextureID = _texture,
                        Size      = new Vector2i((int)face->glyph->bitmap.width, (int)face->glyph->bitmap.rows),
                        Bearing   = new Vector2i((int)face->glyph->bitmap_left, (int)face->glyph->bitmap_top),
                        Advance   = (uint)face->glyph->advance.x
                    };

                    Characters.Add(c, character);
                }

                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            // destruir o FreeType assim que terminarmos
            FT_Done_Face(face);
            FT_Done_FreeType(ft);
        }

        // configurar VAO/VBO para quads de textura
        // --------------------------------------------------
        GL.GenVertexArrays(1, out _vertexArrayObject);
        GL.GenBuffers(1, out _vertexBufferObject);

        GL.BindVertexArray(_vertexArrayObject);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 6 * 4, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
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

        RenderText(_shader, "This is sample text", 25.0f, 25.0f, 1.0f, new Vector3(0.5f, 0.8f, 0.2f));
        RenderText(_shader, "(C) LearnOpenGL.com", 540.0f, 570.0f, 0.5f, new Vector3(0.3f, 0.7f, 0.9f));

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
    }

    // glfw: sempre que o tamanho da janela é alterado (pelo SO ou por redimensionamento do usuário), esta função de callback é executada
    // --------------------------------------------------
    private void FramebufferSizeCallback(int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        GL.Viewport(0, 0, width, height);
    }

    // renderizar linha de texto
    // --------------------------------------------------
    private void RenderText(Shader shader, string text, float x, float y, float scale, Vector3 color)
    {
        // ativar o estado de renderização correspondente
        shader.Use();
        GL.Uniform3(GL.GetUniformLocation(shader.ID, "textColor"), color.X, color.Y, color.Z);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindVertexArray(_vertexArrayObject);

        // percorrer todos os caracteres
        foreach (char c in text)
        {
            Character ch = Characters[c];

            float xpos = x + ch.Bearing.X * scale;
            float ypos = y - (ch.Size.Y - ch.Bearing.Y) * scale;

            float w = ch.Size.X * scale;
            float h = ch.Size.Y * scale;

            // atualizar o VBO para cada caractere
            float[,] vertices = new float[6, 4]
            {
                  // posições           // coordenadas de textura
                { xpos,     ypos,       0.0f, 1.0f },
                { xpos + w, ypos,       1.0f, 1.0f },
                { xpos + w, ypos + h,   1.0f, 0.0f },
                { xpos,     ypos,       0.0f, 1.0f },
                { xpos + w, ypos + h,   1.0f, 0.0f },
                { xpos,     ypos + h,   0.0f, 0.0f }
            };

            // renderizar textura de glifo sobre quadrilátero
            GL.BindTexture(TextureTarget.Texture2D, ch.TextureID);

            // atualizar o conteúdo da memória VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferSubData(BufferTarget.ArrayBuffer, 0, vertices.Length * sizeof(float), vertices); // certifique-se de usar glBufferSubData e não glBufferData

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // renderizar quadrilátero
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // agora avance os cursores para o próximo glifo (note que o avanço é em unidades de 1/64 de pixel)
            x += (ch.Advance >> 6) * scale; // deslocamento de bits de 6 posições para obter o valor em pixels (2^6 = 64 (divida a quantidade de 1/64 de pixel por 64 para obter a quantidade de pixels))
        }

        GL.BindVertexArray(0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}
