using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

public class Window : GameWindow {
    private readonly float[] _vertices = {
        // Positions          // Texture coordinates
         0.5f,  0.5f, 0.0f,   1.0f, 1.0f,  // top right
         0.5f, -0.5f, 0.0f,   1.0f, 0.0f,  // bottom right
        -0.5f, -0.5f, 0.0f,   0.0f, 0.0f,  // bottom left
        -0.5f,  0.5f, 0.0f,   0.0f, 1.0f   // top left
    };

    private readonly uint[] _indices = {  // note that we start from 0!
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    };

    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private int _elementBufferObject;

    private Shader _shader;
    private Texture _texture0;
    private Texture _texture1;

    private Matrix4 _view;
    private Matrix4 _projection;

    public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    protected override void OnLoad() {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        GL.Enable(EnableCap.DepthTest);

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);

        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

        _elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

        /*--------------------------------------------------*/

        //Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
        /*
        
        Current Directory: D:\GitHub\Meus Repositórios\LearnOpenTK\1. Getting Started\1.2. Hello Triangle\ConsoleApp1\bin\Debug\net8.0

        */

        //shader = new Shader("src/shaders/shaderVertex.glsl", "src/shaders/shaderFragment.glsl");
        _shader = new Shader("../../../src/shaders/shaderVertex.glsl", "../../../src/shaders/shaderFragment.glsl");
        _shader.Use();

        /*--------------------------------------------------*/

        var vertexLocation = _shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        int texCoordLocation = _shader.GetAttribLocation("aTexCoord");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        //_texture0 = Texture.LoadFromFile("Resources/container.png");
        _texture0 = Texture.LoadFromFile("../../../src/resources/container.png");
        _texture0.Use(TextureUnit.Texture0);

        //_texture1 = Texture.LoadFromFile("Resources/container.png");
        _texture1 = Texture.LoadFromFile("../../../src/resources/awesomeface.png");
        _texture1.Use(TextureUnit.Texture1);

        _shader.SetInt("texture0", 0);
        _shader.SetInt("texture1", 1);

        _view = Matrix4.CreateTranslation(0.0f, 0.0f, -3.0f);

        _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), Size.X / (float)Size.Y, 0.1f, 100.0f);
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.BindVertexArray(_vertexArrayObject);

        _texture0.Use(TextureUnit.Texture0);
        _texture1.Use(TextureUnit.Texture1);
        _shader.Use();

        var _model = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-55.0f));

        _shader.SetMatrix4("model", _model);
        _shader.SetMatrix4("view", _view);
        _shader.SetMatrix4("projection", _projection);

        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args) {
        base.OnUpdateFrame(args);

        var input = KeyboardState;

        if(input.IsKeyDown(Keys.Escape)) {
            Close();
        }
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e) {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
    }
}
