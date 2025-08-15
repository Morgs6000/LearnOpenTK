using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace ConsoleApp1.src;

public class Program : GameWindow {
    private Shader shader;

    private List<Vector2> vertex = new List<Vector2>();
    private List<int> indices = new List<int>();

    private int vertices;

    private int vertexArrayObject;
    private int vertexBufferObject;
    private int elementBufferObject;

    public Program(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    protected override void OnLoad() {
        base.OnLoad();

        shader = new Shader("../../../src/shaders/Vertex.glsl", "../../../src/shaders/Fragment.glsl");

        DesenharRetangulo(0, 0, 20, 20);
        DesenharRetangulo(-310, 0, 20, 40);
        DesenharRetangulo(310, 0, 20, 40);

        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);

        vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertex.Count * Vector2.SizeInBytes, vertex.ToArray(), BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
        GL.EnableVertexAttribArray(0);

        elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StreamDraw);
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

        shader.Use();

        Matrix4 projection = Matrix4.CreateOrthographic(ClientSize.X, ClientSize.Y, 0.0f, 1.0f);
        shader.SetMatrix4("projection", projection);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.BindVertexArray(vertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    private void DesenharRetangulo(int x, int y, int largura, int altura) {
        vertex.Add(new Vector2(-0.5f * largura + x, -0.5f * altura + y));
        vertex.Add(new Vector2( 0.5f * largura + x, -0.5f * altura + y));
        vertex.Add(new Vector2( 0.5f * largura + x,  0.5f * altura + y));
        vertex.Add(new Vector2(-0.5f * largura + x,  0.5f * altura + y));

        indices.Add(0 + vertices);
        indices.Add(1 + vertices);
        indices.Add(2 + vertices);

        indices.Add(0 + vertices);
        indices.Add(2 + vertices);
        indices.Add(3 + vertices);

        vertices += 4;
    }

    private static void Main() {
        Console.WriteLine("Hello World!");

        GameWindowSettings gws = GameWindowSettings.Default;

        NativeWindowSettings nws = NativeWindowSettings.Default;
        nws.ClientSize = new Vector2i(640, 480);

        new Program(gws, nws).Run();
    }
}
