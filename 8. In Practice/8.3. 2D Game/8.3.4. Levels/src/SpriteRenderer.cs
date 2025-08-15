using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Drawing;

namespace ConsoleApp1.src;

public class SpriteRenderer {
    private Shader shader;

    private float[] vertices = {
        // pos      // tex
        0.0f, 1.0f, 0.0f, 1.0f,
        1.0f, 0.0f, 1.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 0.0f,

        0.0f, 1.0f, 0.0f, 1.0f,
        1.0f, 1.0f, 1.0f, 1.0f,
        1.0f, 0.0f, 1.0f, 0.0f
    };

    private int vertexArrayObject;
    private int vertexBufferObject;

    public SpriteRenderer(Shader shader) {
        this.shader = shader;

        InitRenderData();
    }

    private void InitRenderData() {
        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);

        vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void DrawSprite(Texture texture, Vector2 position, Vector2 size, float rotate, Vector3 color) {
        shader.Use();

        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(size.X, size.Y, 1.0f));
        model *= Matrix4.CreateTranslation(new Vector3(-0.5f * size.X, -0.5f * size.Y, 0.0f));
        model *= Matrix4.CreateFromAxisAngle(new Vector3(0.0f, 0.0f, 1.0f), MathHelper.DegreesToRadians(rotate));
        model *= Matrix4.CreateTranslation(new Vector3(0.5f * size.X, 0.5f * size.Y, 0.0f));
        model *= Matrix4.CreateTranslation(new Vector3(position.X, position.Y, 0.0f));
        shader.SetMatrix4("model", model);

        shader.SetVector3("spriteColor", color);

        texture.Use(TextureUnit.Texture0);

        GL.BindVertexArray(vertexArrayObject);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
    }
}
