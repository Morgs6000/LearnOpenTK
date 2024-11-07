using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

// Então você desenhou o primeiro triângulo. Mas e quanto a desenhar múltiplos?
// Você pode considerar apenas adicionar mais vértices ao array, e isso tecnicamente funcionaria, mas digamos que você esteja desenhando um retângulo.
// Ele só precisa de quatro vértices, mas como o OpenGL funciona em triângulos, você precisaria definir 6.
// Não é grande coisa, mas rapidamente aumenta quando você chega a modelos mais complexos. Por exemplo, um cubo precisa de apenas 8 vértices, mas fazer dessa forma precisaria de 36 vértices!

// O OpenGL fornece uma maneira de reutilizar vértices, o que pode reduzir bastante o uso de memória em objetos complexos.
// Isso é chamado de Objeto Buffer de Elemento. Este tutorial será sobre como configurar um.
public class Window : GameWindow {
    // Modificamos a matriz de vértices para incluir quatro vértices para nosso retângulo.
    float[] vertices = {
         0.5f,  0.5f, 0.0f,  // top right
         0.5f, -0.5f, 0.0f,  // bottom right
        -0.5f, -0.5f, 0.0f,  // bottom left
        -0.5f,  0.5f, 0.0f   // top left
    };

    // Então, criamos um novo array: indices.
    // Este array controla como o EBO usará esses vértices para criar triângulos
    uint[] indices = {  // note that we start from 0!
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    };

    int VertexBufferObject;

    int VertexArrayObject;

    private Shader shader;

    // Adicione um identificador para o EBO
    int ElementBufferObject;

    public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    protected override void OnLoad() {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        VertexBufferObject = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        VertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(VertexArrayObject);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        GL.EnableVertexAttribArray(0);

        // Nós criamos/vinculamos o Element Buffer Object EBO da mesma forma que o VBO, exceto que há uma grande diferença aqui que pode ser REALMENTE confusa.
        // O ponto de vinculação para ElementArrayBuffer não é realmente um ponto de vinculação global como ArrayBuffer é.
        // Em vez disso, é na verdade uma propriedade do VertexArrayObject atualmente vinculado, e vincular um EBO sem VAO é um comportamento indefinido.
        // Isso também significa que se você vincular outro VAO, o ElementArrayBuffer atual mudará com ele.
        // Outra parte furtiva é que você não precisa desvincular o buffer em ElementArrayBuffer, pois desvincular o VAO fará isso, e desvincular o EBO o removerá do VAO em vez de desvinculá-lo como você faria para VBOs ou VAOs.
        ElementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        // Também carregamos dados para o EBO da mesma forma que fizemos com os VBOs.
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        // O EBO agora foi configurado corretamente. Vá para a função Render para ver como desenhamos nosso retângulo agora!

        //Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
        /*
        
        Current Directory: D:\GitHub\Meus Repositórios\LearnOpenTK\1. Getting Started\1.2. Hello Triangle\ConsoleApp1\bin\Debug\net8.0

        */

        //shader = new Shader("src/shaders/shaderVertex.glsl", "src/shaders/shaderFragment.glsl");
        shader = new Shader("../../../src/shaders/shaderVertex.glsl", "../../../src/shaders/shaderFragment.glsl");

        shader.Use();
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        shader.Use();

        // Como ElementArrayObject é uma propriedade do VAO atualmente vinculado, o buffer que você encontrará no ElementArrayBuffer mudará com o VAO atualmente vinculado.
        GL.BindVertexArray(VertexArrayObject);

        // Então substitua sua chamada para DrawTriangles por uma para DrawElements
        // Argumentos:
        // Tipo primitivo para desenhar. Triângulos neste caso.
        // Quantos índices devem ser desenhados. Seis neste caso.
        // Tipo de dados dos índices. Os índices são um unsigned int, então queremos isso aqui também.
        // Deslocamento no EBO. Defina como 0 porque queremos desenhar a coisa toda.
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args) {
        base.OnUpdateFrame(args);

        if(KeyboardState.IsKeyDown(Keys.Escape)) {
            Close();
        }
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e) {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
    }
}
