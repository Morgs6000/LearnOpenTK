using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

public class Program : GameWindow {
    private Shader shader;

    private List<Vector2> vertex = new List<Vector2>();
    private List<int> indices = new List<int>();
    private List<Vector3> colors = new List<Vector3>();

    private int vertices;

    private int vertexArrayObject;
    private int vertexBufferObject;
    private int elementBufferObject;
    private int colorBufferObject;

    private float xDaBola = 0;
    private float yDaBola = 0;
    private int tamanhoDaBola = 20;
    private int velocidadeDaBolaEmX = 300;
    private int velocidadeDaBolaEmY = 300;

    private float yDoJogador1 = 0;
    private float yDoJogador2 = 0;

    public Program(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    protected override void OnLoad() {
        base.OnLoad();

        shader = new Shader("../../../src/shaders/Vertex.glsl", "../../../src/shaders/Fragment.glsl");

        //DesenharRetangulo(0, 0, 20, 20);
        //DesenharRetangulo(-310, 0, 20, 40);
        //DesenharRetangulo(310, 0, 20, 40);

        //LoadTesselator();
    }

    protected override void OnUpdateFrame(FrameEventArgs args) {
        base.OnUpdateFrame(args);

        Clear();

        xDaBola = xDaBola + velocidadeDaBolaEmX * (float)args.Time;
        yDaBola = yDaBola + velocidadeDaBolaEmY * (float)args.Time;

        if(
            xDaBola + tamanhoDaBola / 2 > xDoJogador2() - larguraDosJoagadores() / 2 &&
            yDaBola - tamanhoDaBola / 2 < yDoJogador2 + alturaDosJogadores() / 2 &&
            yDaBola + tamanhoDaBola / 2 > yDoJogador2 - alturaDosJogadores() / 2
        ) {
            velocidadeDaBolaEmX = -velocidadeDaBolaEmX;
        }
        if(
            xDaBola - tamanhoDaBola / 2 < xDoJogador1() + larguraDosJoagadores() / 2 &&
            yDaBola - tamanhoDaBola / 2 < yDoJogador1 + alturaDosJogadores() / 2 &&
            yDaBola + tamanhoDaBola / 2 > yDoJogador1 - alturaDosJogadores() / 2
        ) {
            velocidadeDaBolaEmX = -velocidadeDaBolaEmX;
        }

        if(yDaBola + tamanhoDaBola / 2 > ClientSize.Y / 2) {
            velocidadeDaBolaEmY = -velocidadeDaBolaEmY;
        }
        if(yDaBola - tamanhoDaBola / 2 < -ClientSize.Y / 2) {
            velocidadeDaBolaEmY = -velocidadeDaBolaEmY;
        }

        if(KeyboardState.IsKeyDown(Keys.W)) {
            yDoJogador1 = yDoJogador1 + 500 * (float)args.Time;
        }
        if(KeyboardState.IsKeyDown(Keys.S)) {
            yDoJogador1 = yDoJogador1 - 500 * (float)args.Time;
        }

        if(KeyboardState.IsKeyDown(Keys.Up)) {
            yDoJogador2 = yDoJogador2 + 500 * (float)args.Time;
        }
        if(KeyboardState.IsKeyDown(Keys.Down)) {
            yDoJogador2 = yDoJogador2 - 500 * (float)args.Time;
        }

        if(xDaBola < -ClientSize.X / 2 || xDaBola > ClientSize.X / 2) {
            xDaBola = 0;
            yDaBola = 0;
        }

        DesenharRetangulo(xDaBola, yDaBola, tamanhoDaBola, tamanhoDaBola, 1.0f, 1.0f, 0.0f);
        DesenharRetangulo(xDoJogador1(), yDoJogador1, larguraDosJoagadores(), alturaDosJogadores(), 1.0f, 0.0f, 0.0f);
        DesenharRetangulo(xDoJogador2(), yDoJogador2, larguraDosJoagadores(), alturaDosJogadores(), 0.0f, 0.0f, 1.0f);

        LoadTesselator();
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

        shader.Use();

        Matrix4 projection = Matrix4.CreateOrthographic(ClientSize.X, ClientSize.Y, 0.0f, 1.0f);
        shader.SetMatrix4("projection", projection);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        RenderTesselator();

        SwapBuffers();
    }

    private void LoadTesselator() {
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

        colorBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, colors.Count * Vector3.SizeInBytes, colors.ToArray(), BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
        GL.EnableVertexAttribArray(1);
    }

    private void RenderTesselator() {
        GL.BindVertexArray(vertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
    }

    private void Clear() {
        vertex.Clear();
        indices.Clear();
        colors.Clear();

        vertices = 0;

        GL.DeleteVertexArray(vertexArrayObject);
        GL.DeleteBuffer(vertexBufferObject);
        GL.DeleteBuffer(elementBufferObject);
        GL.DeleteBuffer(colorBufferObject);
    }

    private void DesenharRetangulo(float x, float y, int largura, int altura, float r, float g, float b) {
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

        colors.Add(new Vector3(r, g, b));
        colors.Add(new Vector3(r, g, b));
        colors.Add(new Vector3(r, g, b));
        colors.Add(new Vector3(r, g, b));
    }

    private int xDoJogador1() {
        return -ClientSize.X / 2 + larguraDosJoagadores() / 2;
    }

    private int xDoJogador2() {
        return ClientSize.X / 2 - larguraDosJoagadores() / 2;
    }

    private int larguraDosJoagadores() {
        return tamanhoDaBola;
    }

    private int alturaDosJogadores() {
        return 3 * tamanhoDaBola;
    }

    private static void Main() {
        Console.WriteLine("Hello World!");

        GameWindowSettings gws = GameWindowSettings.Default;

        NativeWindowSettings nws = NativeWindowSettings.Default;
        nws.ClientSize = new Vector2i(640, 480);

        new Program(gws, nws).Run();
    }
}
