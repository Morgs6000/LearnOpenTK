using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

public class Window : GameWindow {
    private int width;
    private int height;

    private Shader lightingShader;
    private Shader lightCubeShader;

    private Texture diffuseMap;
    private Texture specularMap;

    private float[] vertices = {
        // positions          // normals           // texture coords
        -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f,  1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f,  1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f,  1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f,  0.0f,

        -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f,  1.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f,  1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f,  0.0f,

        -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  1.0f,  1.0f,
        -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
        -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
        -0.5f, -0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  0.0f,  0.0f,
        -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  1.0f,  0.0f,

         0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  1.0f,  1.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
         0.5f, -0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  0.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  1.0f,  0.0f,

        -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  0.0f,  1.0f,
         0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  1.0f,  1.0f,
         0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  1.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  0.0f,  1.0f,

        -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f,  1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  1.0f,  1.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  0.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f,  1.0f
    };

    private Vector3[] cubePositions = {
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

    private int cubeVAO;
    private int lightCubeVAO;
    private int vertexBufferObject;

    private float speed = 1.5f;

    private Vector3 position = new Vector3(0.0f, 0.0f, 3.0f);
    private Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);
    private Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);

    private float fov = 45.0f;

    private bool firstMouse = true;
    private Vector2 lastPos;

    private float pitch;        // Rotx
    private float yaw = -90.0f; // Roty

    private float sensitivity = 0.1f;

    // iluminação
    private static Vector3 lightPos = new Vector3(1.2f, 1.0f, 2.0f);

    public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();

        width = ClientSize.X;
        height = ClientSize.Y;
    }

    protected override void OnLoad() {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

        lightingShader = new Shader("../../../src/shaders/vertex_materials.glsl", "../../../src/shaders/fragment_materials.glsl");
        lightCubeShader = new Shader("../../../src/shaders/vertex_light_cube.glsl", "../../../src/shaders/fragment_light_cube.glsl");

        diffuseMap = new Texture("../../../src/textures/container2.png");
        specularMap = new Texture("../../../src/textures/container2_specular.png");

        // primeiro, configure o VAO (e VBO) do cubo
        cubeVAO = GL.GenVertexArray();
        GL.BindVertexArray(cubeVAO);
        
        vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);

        // atributo de posição
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // atributo normal
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // atributo de textura
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        // segundo, configure o VAO da luz (o VBO permanece o mesmo; os vértices são os mesmos para o objeto de luz que também é um cubo 3D)
        lightCubeVAO = GL.GenVertexArray();
        GL.BindVertexArray(lightCubeVAO);

        // só precisamos vincular ao VBO (para vinculá-lo ao glVertexAttribPointer), não há necessidade de preenchê-lo; os dados do VBO já contêm tudo o que precisamos (já está vinculado, mas fazemos novamente para fins educacionais)
        //vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        //GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.Enable(EnableCap.DepthTest);

        CursorState = CursorState.Grabbed;

        lightingShader.Use();
        lightingShader.SetInt("material.diffuse", 0);
        lightingShader.SetInt("material.specular", 1);
    }

    protected override void OnUpdateFrame(FrameEventArgs args) {
        base.OnUpdateFrame(args);

        if(!IsFocused) {
            return;
        }

        KeyboardState input = KeyboardState;

        if(input.IsKeyDown(Keys.Escape)) {
            Close();
        }

        if(input.IsKeyDown(Keys.W)) {
            position += front * speed * (float)args.Time; // Forward 
        }
        if(input.IsKeyDown(Keys.S)) {
            position -= front * speed * (float)args.Time; // Backwards
        }
        if(input.IsKeyDown(Keys.A)) {
            position -= Vector3.Normalize(Vector3.Cross(front, up)) * speed * (float)args.Time; // Left
        }
        if(input.IsKeyDown(Keys.D)) {
            position += Vector3.Normalize(Vector3.Cross(front, up)) * speed * (float)args.Time; // Right
        }

        if(input.IsKeyDown(Keys.Space)) {
            position += up * speed * (float)args.Time; // Up
        }
        if(input.IsKeyDown(Keys.LeftShift)) {
            position -= up * speed * (float)args.Time; // Down
        }

        if(firstMouse) {
            lastPos = new Vector2(MouseState.X, MouseState.Y);
            firstMouse = false;
        }
        else {
            float deltaX = MouseState.X - lastPos.X;
            float deltaY = MouseState.Y - lastPos.Y;
            lastPos = new Vector2(MouseState.X, MouseState.Y);

            yaw += deltaX * sensitivity;
            pitch -= deltaY * sensitivity;

            if(pitch > 89.0f) {
                pitch = 89.0f;
            }
            if(pitch < -89.0f) {
                pitch = -89.0f;
            }

            front.X = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        lightingShader.Use();
        lightingShader.SetVector3("light.position", lightPos);
        lightingShader.SetVector3("viewPos", position);

        // propriedades da luz
        lightingShader.SetVector3("light.ambient", 0.2f, 0.2f, 0.2f);
        lightingShader.SetVector3("light.diffuse", 0.5f, 0.5f, 0.5f);
        lightingShader.SetVector3("light.specular", 1.0f, 1.0f, 1.0f);
        lightingShader.SetFloat("light.constant", 1.0f);
        lightingShader.SetFloat("light.linear", 0.09f);
        lightingShader.SetFloat("light.quadratic", 0.032f);

        // propriedades dos materiais
        lightingShader.SetFloat("material.shininess", 32.0f);

        // vincula mapa difuso
        diffuseMap.Use(TextureUnit.Texture0);
        // vincula mapa especular
        specularMap.Use(TextureUnit.Texture1);

        // transformação mundial
        Matrix4 model = Matrix4.Identity;
        lightingShader.SetMatrix4("model", model);

        // renderiza o cubo
        //GL.BindVertexArray(cubeVAO);
        //GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // renderiza contêineres
        GL.BindVertexArray(cubeVAO);
        for(int i = 0; i < 10; i++) {
            // calcula a matriz do modelo para cada objeto e passa para o shader antes de desenhar
            model = Matrix4.Identity;
            model *= Matrix4.CreateTranslation(cubePositions[i]);
            float angle = 20.0f * i;
            model *= Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.3f, 0.5f), MathHelper.DegreesToRadians(angle));
            lightingShader.SetMatrix4("model", model);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }

        // transformação de visualização
        Matrix4 view = Matrix4.LookAt(position, position + front, up);
        lightingShader.SetMatrix4("view", view);

        // transformação de projeção
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), (float)width / (float)height, 0.1f, 100.0f);
        lightingShader.SetMatrix4("projection", projection);

        // também desenha o objeto lâmpada
        lightCubeShader.Use();

        GL.BindVertexArray(lightCubeVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        model = Matrix4.Identity;
        model *= Matrix4.CreateScale(new Vector3(0.2f)); // um cubo menor
        model *= Matrix4.CreateTranslation(lightPos);
        lightCubeShader.SetMatrix4("model", model);

        lightCubeShader.SetMatrix4("view", view);

        lightCubeShader.SetMatrix4("projection", projection);

        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e) {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        width = e.Width;
        height = e.Height;
    }

    protected override void OnMouseMove(MouseMoveEventArgs e) {
        base.OnMouseMove(e);

        if(IsFocused) {
            //MousePosition = new Vector2(e.X + width / 2.0f, e.Y + height / 2.0f);
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e) {
        base.OnMouseWheel(e);

        fov -= e.OffsetY;

        if(fov > 45.0f) {
            fov = 45.0f;
        }
        if(fov < 1.0f) {
            fov = 1.0f;
        }
    }
}
