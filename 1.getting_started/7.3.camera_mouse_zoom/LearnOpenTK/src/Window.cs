using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace LearnOpenTK;

public class Window : GameWindow
{
    private Shader shader;

    private uint texture0;
    private uint texture1;

    private float[] vertices =
    {
        // posiitons          // texture coords
        -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,   // bottom left    // 0
        -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,   // bottom right   // 1
        -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,   // bottom left    // 0
        -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,   // top left       // 3
        
         0.5f, -0.5f,  0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f, -0.5f, -0.5f,   1.0f, 0.0f,   // bottom right   // 1
         0.5f,  0.5f, -0.5f,   1.0f, 1.0f,   // top right      // 2
         0.5f, -0.5f,  0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f,  0.5f, -0.5f,   1.0f, 1.0f,   // top right      // 2
         0.5f,  0.5f,  0.5f,   0.0f, 1.0f,   // top left       // 3
        
        -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f, -0.5f, -0.5f,   1.0f, 0.0f,   // bottom right   // 1
         0.5f, -0.5f,  0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f, -0.5f,  0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,   // top left       // 3
        
        -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f,  0.5f,  0.5f,   1.0f, 0.0f,   // bottom right   // 1
         0.5f,  0.5f, -0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f,  0.5f, -0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,   // top left       // 3
        
         0.5f, -0.5f, -0.5f,   0.0f, 0.0f,   // bottom left    // 0
        -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,   // bottom right   // 1
        -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,   // top right      // 2
         0.5f, -0.5f, -0.5f,   0.0f, 0.0f,   // bottom left    // 0
        -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,   // top right      // 2
         0.5f,  0.5f, -0.5f,   0.0f, 1.0f,   // top left       // 3
        
        -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f, -0.5f,  0.5f,   1.0f, 0.0f,   // bottom right   // 1
         0.5f,  0.5f,  0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,   // bottom left    // 0
         0.5f,  0.5f,  0.5f,   1.0f, 1.0f,   // top right      // 2
        -0.5f,  0.5f,  0.5f,   0.0f, 1.0f,   // top left       // 3
    };

    private uint[] indices =
    {
        0, 1, 2, // first triangle
        0, 2, 3  // second triangle
    };

    private uint VAO;
    private uint VBO;
    private uint EBO;

    private Vector3[] cubePositions =
    {
        new Vector3( 0.0f,  0.0f,  0.0f),
        new Vector3( 2.0f,  5.0f, -15.0f),
        new Vector3(-1.5f, -2.2f, -2.5f),
        new Vector3(-3.8f, -2.0f, -12.3f),
        new Vector3( 2.4f, -0.4f, -3.5f),
        new Vector3(-1.7f,  3.0f, -7.5f),
        new Vector3( 1.3f, -2.0f, -2.5f),
        new Vector3( 1.5f,  2.0f, -2.5f),
        new Vector3( 1.5f,  0.2f, -1.5f),
        new Vector3(-1.3f,  1.0f, -1.5f)
    };

    private Vector3 cameraPos   = new Vector3(0.0f, 0.0f, 3.0f);
    private Vector3 cameraFront = new Vector3(0.0f, 0.0f, -1.0f);
    private Vector3 cameraUp = new Vector3(0.0f, 1.0f, 0.0f);

    private float yaw = -90.0f;
    private float pitch;

    private Vector2 last;

    private bool firstMouse = true;

    private float fov = 45.0f;

    private float deltaTime = 0.0f;
    private float lastFrame = 0.0f;

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        shader = new Shader("src/shaders/shader_vertex.glsl", "src/shaders/shader_fragment.glsl");
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        GL.GenTextures(1, out texture0);
        GL.BindTexture(TextureTarget.Texture2D, texture0);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        StbImage.stbi_set_flip_vertically_on_load(1);

        ImageResult image = ImageResult.FromStream(File.OpenRead("src/textures/container.jpg"), ColorComponents.RedGreenBlueAlpha);

        if (image.Data != null)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        else
        {
            Console.WriteLine("Failed to load texture");
        }

        GL.GenTextures(1, out texture1);
        GL.BindTexture(TextureTarget.Texture2D, texture1);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        image = ImageResult.FromStream(File.OpenRead("src/textures/awesomeface.png"), ColorComponents.RedGreenBlueAlpha);

        if (image.Data != null)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        else
        {
            Console.WriteLine("Failed to load texture");
        }  

        GL.GenVertexArrays(1, out VAO);
        GL.BindVertexArray(VAO);

        GL.GenBuffers(1, out VBO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.GenBuffers(1, out EBO);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
        // GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        GL.Enable(EnableCap.DepthTest);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        float currentFrame = (float)GLFW.GetTime();
        deltaTime = currentFrame - lastFrame;
        lastFrame = currentFrame;

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        float cameraSpeed = 2.5f * deltaTime;

        if (KeyboardState.IsKeyDown(Keys.W))
        {
            cameraPos += cameraSpeed * cameraFront;
        }
        if (KeyboardState.IsKeyDown(Keys.S))
        {
            cameraPos -= cameraSpeed * cameraFront;
        }
        if (KeyboardState.IsKeyDown(Keys.A))
        {
            cameraPos -= cameraSpeed * Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp));
        }
        if (KeyboardState.IsKeyDown(Keys.D))
        {
            cameraPos += cameraSpeed * Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp));
        }

        if (firstMouse)
        {
            last.X = MouseState.Position.X;
            last.Y = MouseState.Position.Y;
            firstMouse = false;
        }

        float xoffset = MouseState.Position.X - last.X;
        float yoffset = last.Y - MouseState.Position.Y;
        last.X = MouseState.Position.X;
        last.Y = MouseState.Position.Y;

        const float sensitivity = 0.1f;
        xoffset *= sensitivity;
        yoffset *= sensitivity;

        yaw   += xoffset;
        pitch += yoffset;

        if (pitch > 89.0f)
        {
            pitch = 89.0f;
        }
        if (pitch < -89.0f)
        {
            pitch = -89.0f;
        }

        Vector3 direction;
        direction.X = (float)(Math.Cos(MathHelper.DegreesToRadians(pitch)) * Math.Cos(MathHelper.DegreesToRadians(yaw)));
        direction.Y = (float)(Math.Sin(MathHelper.DegreesToRadians(pitch)));
        direction.Z = (float)(Math.Cos(MathHelper.DegreesToRadians(pitch)) * Math.Sin(MathHelper.DegreesToRadians(yaw)));
        cameraFront = Vector3.Normalize(direction);

        CursorState = CursorState.Grabbed;

        fov -= MouseState.ScrollDelta.Y;
        if(fov < 1.0f)
        {
            fov = 1.0f;
        }
        if(fov > 45.0f)
        {
            fov = 45.0f;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        shader.Use();

        shader.SetInt("texture0", 0);
        shader.SetInt("texture1", 1);

        Matrix4 view = Matrix4.Identity;
        view *= Matrix4.LookAt(cameraPos, cameraPos + cameraFront, cameraUp);
        shader.SetMatrix4("view", view);

        Matrix4 projection = Matrix4.Identity;
        projection *= Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), (float)ClientSize.X / (float)ClientSize.Y, 0.1f, 100.0f);
        shader.SetMatrix4("projection", projection);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, texture0);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, texture1);

        GL.BindVertexArray(VAO);

        for (int i = 0; i < 10; i++)
        {
            Matrix4 model = Matrix4.Identity;
            float angle = 20.0f * i;
            model *= Matrix4.CreateFromAxisAngle(new Vector3(1.5f, 0.3f, 0.5f), MathHelper.DegreesToRadians(angle));
            model *= Matrix4.CreateTranslation(cubePositions[i]);
            shader.SetMatrix4("model", model);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }        

        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
    }
}
