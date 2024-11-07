using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

// Esteja avisado, há MUITA coisa aqui. Pode parecer complicado, mas vá devagar e você ficará bem.
// O obstáculo inicial do OpenGL é bem grande, mas depois que você superá-lo, as coisas começarão a fazer mais sentido.
public class Window : GameWindow {
    // Crie os vértices para nosso triângulo. Eles são listados em coordenadas de dispositivo normalizadas (NDC)
    // Em NDC, (0, 0) é o centro da tela.
    // Coordenadas X negativas movem-se para a esquerda, X positivas movem-se para a direita.
    // Coordenadas Y negativas movem-se para baixo, Y positivas movem-se para cima.
    // OpenGL suporta apenas renderização em 3D, então para criar um triângulo plano, a coordenada Z será mantida como 0.
    float[] vertices = {
        -0.5f, -0.5f, 0.0f, //Bottom-left vertex
         0.5f, -0.5f, 0.0f, //Bottom-right vertex
         0.0f,  0.5f, 0.0f  //Top vertex
    };

    // Estes são os identificadores para objetos OpenGL. Um identificador é um inteiro que representa onde o objeto reside na placa gráfica. Considere-os como um ponteiro; não podemos fazer nada com eles diretamente, mas podemos enviá-los para funções OpenGL que precisam deles.

    // O que esses objetos são será explicado em OnLoad.
    int VertexBufferObject;

    int VertexArrayObject;

    // Esta classe é um wrapper em torno de um shader, o que nos ajuda a gerenciá-lo.
    // O que são shaders e para que são usados ​​será explicado mais adiante neste tutorial.
    private Shader shader;

    public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    // Agora, começamos a inicializar o OpenGL.
    protected override void OnLoad() {
        base.OnLoad();

        // Esta será a cor do fundo depois que o limparmos, em cores normalizadas.
        // Cores normalizadas são mapeadas em um intervalo de 0,0 a 1,0, com 0,0 representando preto e 1,0 representando o maior valor possível para aquele canal.
        // Este é um verde profundo.
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        // Precisamos enviar nossos vértices para a placa de vídeo para que o OpenGL possa usá-los.
        // Para fazer isso, precisamos criar o que é chamado de Vertex Buffer Object (VBO).
        // Eles permitem que você carregue um monte de dados para um buffer e envie o buffer para a placa de vídeo.
        // Isso efetivamente envia todos os vértices ao mesmo tempo.

        // Primeiro, precisamos criar um buffer. Esta função retorna um handle para ele, mas, por enquanto, ele está vazio.
        VertexBufferObject = GL.GenBuffer();

        // Agora, vincule o buffer. O OpenGL usa um estado global, então, depois de chamar isso,
        // todas as chamadas futuras que modificarem o VBO serão aplicadas a esse buffer até que outro buffer seja vinculado.
        // O primeiro argumento é um enum, especificando que tipo de buffer estamos vinculando. Um VBO é um ArrayBuffer.
        // Existem vários tipos de buffers, mas, por enquanto, apenas o VBO é necessário.
        // O segundo argumento é o identificador para nosso buffer.
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

        // Por fim, carregue os vértices no buffer.
        // Argumentos:
        // Para qual buffer os dados devem ser enviados.
        // Quantos dados estão sendo enviados, em bytes. Geralmente, você pode definir isso como o comprimento do seu array, multiplicado por sizeof(array type).
        // Os próprios vértices.
        // Como o buffer será usado, para que o OpenGL possa gravar os dados no espaço de memória adequado na GPU.
        // Existem três BufferUsageHints diferentes para desenho:
        // StaticDraw: Este buffer raramente, ou nunca, será atualizado após ser carregado inicialmente.
        // DynamicDraw: Este buffer mudará frequentemente após ser carregado inicialmente.
        // StreamDraw: Este buffer mudará em cada quadro.
        // Escrever no espaço de memória adequado é importante! Geralmente, você só vai querer StaticDraw,
        // mas certifique-se de usar o correto para seu caso de uso.
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // Uma coisa notável sobre o buffer no qual acabamos de carregar dados é que ele não tem nenhuma estrutura. É apenas um monte de floats (que são, na verdade, apenas bytes).
        // O driver opengl não sabe como esses dados devem ser interpretados ou como devem ser divididos em vértices. Para fazer isso, o opengl introduz a ideia de um Vertex Array Object (VAO) que tem a função de rastrear quais partes ou quais buffers correspondem a quais dados. Neste exemplo, queremos configurar nosso VAO para que ele diga ao opengl que queremos interpretar 12 bytes como 3 floats e dividir o buffer em vértices usando isso.
        // Para fazer isso, geramos e vinculamos um VAO (que parece enganosamente semelhante a criar e vincular um VBO, mas eles são diferentes!).
        VertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(VertexArrayObject);

        // Agora, precisamos configurar como o vertex shader interpretará os dados VBO; você pode enviar quase qualquer tipo de dado C (e alguns não C também) para ele.
        // Embora isso os torne incrivelmente flexíveis, significa que temos que especificar como esses dados serão mapeados para as variáveis ​​de entrada do shader.

        // Para fazer isso, usamos a função GL.VertexAttribPointer
        // Esta função tem dois trabalhos, informar ao OpenGL sobre o formato dos dados, mas também associar o buffer de array atual ao VAO.
        // Isso significa que após esta chamada, configuramos este atributo para obter dados do buffer de array atual e interpretá-los da maneira que especificamos.
        // Argumentos:
        // Localização da variável de entrada no shader. A linha layout(location = 0) no vertex shader a define explicitamente como 0.
        // Quantos elementos serão enviados para a variável. Neste caso, 3 floats para cada vértice.
        // O tipo de dado do conjunto de elementos, neste caso float.
        // Se os dados devem ou não ser convertidos para coordenadas de dispositivo normalizadas. Neste caso, falso, porque isso já foi feito.
        // O passo; é quantos bytes estão entre o último elemento de um vértice e o primeiro elemento do próximo. 3 * sizeof(float) neste caso.
        // O deslocamento; é quantos bytes ele deve pular para encontrar o primeiro elemento do primeiro vértice. 0 até agora.
        // Passo e Deslocamento são apenas meio que ignorados por enquanto, mas quando entrarmos nas coordenadas de textura, eles serão mostrados em mais detalhes.
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        // Habilita a variável 0 no shader.
        GL.EnableVertexAttribArray(0);

        // Temos os vértices prontos, mas como exatamente isso deve ser convertido em pixels para a imagem final?
        // O OpenGL moderno torna esse pipeline muito livre, nos dando muita liberdade sobre como os vértices são transformados em pixels.
        // A desvantagem é que, na verdade, precisamos de mais dois programas para isso! Eles são chamados de "shaders".
        // Shaders são pequenos programas que vivem na GPU. O OpenGL os usa para lidar com o pipeline de vértice para pixel.
        // Confira a classe Shader em Common para ver como criamos nossos shaders, bem como uma explicação mais aprofundada de como os shaders funcionam.
        // shaderVertex.glsl e shaderFragment.glsl contêm o código real do shader.

        //Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
        /*
        
        Current Directory: D:\GitHub\Meus Repositórios\LearnOpenTK\1. Getting Started\1.2. Hello Triangle\ConsoleApp1\bin\Debug\net8.0

        */

        //shader = new Shader("src/shaders/shaderVertex.glsl", "src/shaders/shaderFragment.glsl");
        shader = new Shader("../../../src/shaders/shaderVertex.glsl", "../../../src/shaders/shaderFragment.glsl");

        // Agora, habilite o shader.
        // Assim como o VBO, isso é global, então cada função que usa um shader modificará este até que um novo seja vinculado.
        shader.Use();

        // A configuração está completa! Agora vamos para a função OnRenderFrame para finalmente desenhar o triângulo.
    }

    // Agora que a inicialização foi concluída, vamos criar nosso loop de renderização.
    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        // Isso limpa a imagem, usando o que você definiu como GL.ClearColor anteriormente.
        // OpenGL fornece vários tipos diferentes de dados que podem ser renderizados.
        // Você pode limpar vários buffers usando vários sinalizadores de bits.
        // No entanto, modificamos apenas a cor, então ColorBufferBit é tudo o que precisamos limpar.
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // Para desenhar um objeto em OpenGL, é tipicamente tão simples quanto vincular seu shader, definir uniformes de shader (não feito aqui, será mostrado em um tutorial futuro) vincular o VAO, e então chamar uma função OpenGL para renderizar.

        // Vincular o shader
        shader.Use();

        // Vincular o VAO
        GL.BindVertexArray(VertexArrayObject);

        // E então chame nossa função de desenho.
        // Para este tutorial, usaremos GL.DrawArrays, que é uma função de renderização muito simples.
        // Argumentos:
        // Tipo primitivo; Que tipo de primitivo geométrico os vértices representam.
        // O OpenGL costumava suportar muitos tipos primitivos diferentes, mas quase todos ainda são suportados é alguma variante de um triângulo. Como queremos apenas um único triângulo, usamos Triangles.
        // Índice inicial; este é apenas o início dos dados que você deseja desenhar. 0 aqui.
        // Quantos vértices você deseja desenhar. 3 para um triângulo.
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // As janelas OpenTK são o que é conhecido como "double-buffered". Em essência, a janela gerencia dois buffers.
        // Um ​​é renderizado enquanto o outro é exibido atualmente pela janela.
        // Isso evita tela rasgada, um artefato visual que pode acontecer se o buffer for modificado enquanto estiver sendo exibido.
        // Após desenhar, chame esta função para trocar os buffers. Se não fizer isso, não exibirá o que você renderizou.
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

        // Quando a janela é redimensionada, temos que chamar GL.Viewport para redimensionar a viewport do OpenGL para corresponder ao novo tamanho.
        // Se não fizermos isso, o NDC não estará mais correto.
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    // Agora, para limpeza.
    // Você geralmente não deve fazer limpeza de recursos opengl ao sair de um aplicativo, pois isso é feito pelo driver e pelo sistema operacional quando o aplicativo sai.
    //
    // Há motivos para excluir recursos opengl, mas sair do aplicativo não é um deles.
    // Isso é fornecido aqui como uma referência sobre como a limpeza de recursos é feita em opengl, mas não deve ser feita ao sair do aplicativo.
    //
    // Os lugares onde a limpeza é apropriada seriam: para excluir texturas que não são
    // mais usadas por qualquer motivo (por exemplo, uma nova cena é carregada que não usa uma textura).
    // Isso liberaria memória RAM de vídeo (VRAM) que pode ser usada para novas texturas.
    //
    // Os próximos capítulos não terão esse código.
    protected override void OnUnload() {
        base.OnUnload();

        // Desvincule todos os recursos vinculando os alvos a 0/nulo.
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        // Exclua todos os recursos.
        GL.DeleteBuffer(VertexBufferObject);
    }

    // ... //

    
}
