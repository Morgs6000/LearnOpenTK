/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK.src;

public class SpriteRenderer : IDisposable
{
    // Construtor (inicializa shaders/formas)
    public SpriteRenderer(Shader shader)
    {
        _shader = shader;

        InitRenderData();
    }

    // Destrutor
    public void Dispose()
    {
        GL.DeleteVertexArrays(1, ref _quadVAO);
    }

    // Renderiza um quadrilátero definido texturizado com o sprite fornecido
    public void DrawSprite(Texture2D texture, Vector2 position, Vector2? size = null, float? rotate = null, Vector3? color = null)
    {
        Vector2 _size = size ?? new Vector2(10.0f, 10.0f);
        float _rotate = rotate ?? 0.0f;
        Vector3 _color = color ?? new Vector3(1.0f);

        // preparar transformações
        _shader.Use();
        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(_size.X, _size.Y, 1.0f)); // última escala

        model *= Matrix4.CreateTranslation(new Vector3(-0.5f * _size.X, -0.5f * _size.Y, 0.0f)); // mover a origem de volta
        model *= Matrix4.CreateFromAxisAngle(new Vector3(0.0f, 0.0f, 1.0f), MathHelper.DegreesToRadians(_rotate)); // depois rotacione
        model *= Matrix4.CreateTranslation(new Vector3(0.5f * _size.X, 0.5f * _size.Y, 0.0f)); // move a origem da rotação para o centro do quadrilátero

        model *= Matrix4.CreateTranslation(new Vector3(position.X, position.Y, 0.0f)); // primeira translação (as transformações ocorrem nesta ordem: escala primeiro, depois rotação e, por fim, a translação final; ordem inversa)

        _shader.SetMatrix4("model", model);

        // renderizar quadrilátero texturizado
        _shader.SetVector3f("spriteColor", _color);

        GL.ActiveTexture(TextureUnit.Texture0);
        texture.Bind();

        GL.BindVertexArray(_quadVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
    }

    // Estado de renderização
    private Shader _shader;
    private uint _quadVAO;

    // Inicializa e configura o buffer e os atributos de vértice do quad
    private void InitRenderData()
    {
        // configurar VAO/VBO
        uint VBO;

        float[] vertices =
        {
            // pos        // tex
            0.0f, 0.0f,   0.0f, 0.0f,
            1.0f, 0.0f,   1.0f, 0.0f,
            1.0f, 1.0f,   1.0f, 1.0f,
            0.0f, 0.0f,   0.0f, 0.0f,
            1.0f, 1.0f,   1.0f, 1.0f,
            0.0f, 1.0f,   0.0f, 1.0f
        };

        GL.GenVertexArrays(1, out _quadVAO);
        GL.GenBuffers(1, out VBO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(_quadVAO);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }
}
