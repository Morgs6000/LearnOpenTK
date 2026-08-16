/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Runtime.InteropServices;
using FreeTypeSharp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;

namespace LearnOpenTK.src;

// Armazena todas as informações de estado relevantes para um caractere, conforme carregado usando o FreeType
public struct Character
{
    public uint TextureID;  // Identificador da textura do glifo
    public Vector2 Size;    // tamanho do glifo
    public Vector2 Bearing; // deslocamento da linha de base até a esquerda/topo do glifo
    public uint Advance;    // deslocamento horizontal para avançar para o próximo glifo
}

// Uma classe de renderização para exibir texto utilizando uma fonte carregada
// com a biblioteca FreeType. Uma única fonte é carregada e processada em uma
// lista de itens de caractere para posterior renderização.
public class TextRenderer
{
    // armazena uma lista de caracteres pré-compilados
    public Dictionary<uint, Character> Characters = [];

    // shader usado para renderização de texto
    public Shader TextShader;

    // construtor
    public TextRenderer(uint width, uint height)
    {
        // carregar e configurar o shader
        TextShader = ResourceManager.LoadShader("src/text_2d.vs", "src/text_2d.fs", null, "text");
        TextShader.SetMatrix4("projection", Matrix4.CreateOrthographicOffCenter(
            left:       0.0f, 
            right:      (float)width, 
            bottom:     (float)height, 
            top:        0.0f, 
            depthNear: -1.0f, 
            depthFar:   1.0f
        ), true);
        TextShader.SetInteger("text", 0);

        // configurar VAO/VBO para quads de textura
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

    // pré-compila uma lista de caracteres da fonte fornecida
    public void Load(string font, uint fontSize)
    {
        // primeiro, limpe os personagens carregados anteriormente
        Characters.Clear();

        unsafe
        {
            // então, inicialize e carregue a biblioteca FreeType
            FT_LibraryRec_* ft;

            if (FT_Init_FreeType(&ft) != 0) // todas as funções retornam um valor diferente de 0 sempre que ocorre um erro
            {
                Console.WriteLine("ERROR::FREETYPE: Could not init FreeType Library");
            }

            // carregar fonte como face
            FT_FaceRec_* face;

            if (FT_New_Face(ft, (byte*)Marshal.StringToHGlobalAnsi(font), 0, &face) != 0)
            {
                Console.WriteLine("ERROR::FREETYPE: Failed to load font");
            }

            // Defina o tamanho para carregar os glifos como
            FT_Set_Pixel_Sizes(face, 0, fontSize);

            // desativar a restrição de alinhamento de bytes
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            // então, para os primeiros 128 caracteres ASCII, pré-carregue/compile seus caracteres e armazene-os
            for (uint c = 0; c < 128; c++)
            {
                // carregar glifo de caractere
                if (FT_Load_Char(face, c, FT_LOAD_RENDER) != 0)
                {
                    Console.WriteLine("ERROR::FREETYTPE: Failed to load Glyph");
                    continue;
                }

                // gerar textura
                uint texture;

                GL.GenTextures(1, out texture);
                GL.BindTexture(TextureTarget.Texture2D, texture);

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

                // agora armazene o caractere para uso posterior
                Character character = new Character()
                {
                    TextureID = texture,
                    Size = new Vector2(face->glyph->bitmap.width, face->glyph->bitmap.rows),
                    Bearing = new Vector2(face->glyph->bitmap_left, face->glyph->bitmap_top),
                    Advance = (uint)face->glyph->advance.x
                };

                Characters.Add(c, character);
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);

            // destruir o FreeType assim que terminarmos
            FT_Done_Face(face);
            FT_Done_FreeType(ft);
        }
    }

    // renderiza uma cadeia de texto usando a lista de caracteres pré-compilada
    public void RenderText(string text, float x, float y, float scale, Vector3? color = null)
    {
        Vector3 _color = color ?? new Vector3(1.0f);

        // ativar o estado de renderização correspondente
        TextShader.Use();
        TextShader.SetVector3f("textColor", _color);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindVertexArray(_vertexArrayObject);

        // percorrer todos os caracteres
        foreach (char c in text)
        {
            Character ch = Characters[c];

            float xpos = x + ch.Bearing.X * scale;
            float ypos = y + (Characters['H'].Bearing.Y - ch.Bearing.Y) * scale;

            float w = ch.Size.X * scale;
            float h = ch.Size.Y * scale;

            // atualizar o VBO para cada caractere
            float[,] vertices = new float[6, 4]
            {
                { xpos,     ypos,     0.0f, 0.0f },
                { xpos + w, ypos,     1.0f, 0.0f },
                { xpos + w, ypos + h, 1.0f, 1.0f },
                { xpos,     ypos,     0.0f, 0.0f },
                { xpos + w, ypos + h, 1.0f, 1.0f },
                { xpos,     ypos + h, 0.0f, 1.0f }
            };

            // renderizar textura de glifo sobre quadrilátero
            GL.BindTexture(TextureTarget.Texture2D, ch.TextureID);

            // atualizar o conteúdo da memória VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferSubData(BufferTarget.ArrayBuffer, 0, vertices.Length * sizeof(float), vertices); // certifique-se de usar glBufferSubData e não glBufferData

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // renderizar quadrilátero
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // agora avance os cursores para o próximo glifo
            x += (ch.Advance >> 6) * scale; // deslocamento de bits de 6 posições para obter o valor em pixels (1/64 vezes 2^6 = 64)
        }

        GL.BindVertexArray(0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    // estado de renderização
    private uint _vertexArrayObject;
    private uint _vertexBufferObject;
}
