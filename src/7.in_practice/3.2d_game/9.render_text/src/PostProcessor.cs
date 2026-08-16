/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using OpenTK.Graphics.OpenGL4;

namespace LearnOpenTK.src;

// A classe PostProcessor gerencia todos os efeitos de pós-processamento para o jogo Breakout.
// Ela renderiza o jogo em um quadrilátero texturizado, permitindo ativar efeitos
// específicos por meio das variáveis ​​booleanas Confuse, Chaos ou Shake.
// Para que a classe funcione, é necessário chamar BeginRender() antes de renderizar
// o jogo e EndRender() após a renderização.
public class PostProcessor
{
    // estado
    public Shader PostProcessingShader;
    public Texture2D Texture;
    public int Width, Height;

    // opções
    public bool Confuse, Chaos, Shake;

    // construtor
    public PostProcessor(Shader shader, int width, int height)
    {
        PostProcessingShader = shader;
        Texture = new Texture2D();

        Width = width;
        Height = height;

        Confuse = false;
        Chaos = false;
        Shake = false;

        // inicializar objeto renderbuffer/framebuffer
        GL.GenFramebuffers(1, out _multisampledFramebufferObject);
        GL.GenFramebuffers(1, out _framebufferObject);
        GL.GenRenderbuffers(1, out _renderbufferObject);

        // inicializa o armazenamento do renderbuffer com um buffer de cor multisampled (não é necessário um buffer de profundidade/stencil)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _multisampledFramebufferObject);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderbufferObject);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, RenderbufferStorage.Rgb10, width, height); // alocar armazenamento para o objeto de buffer de renderização
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _renderbufferObject); // anexa o objeto de buffer de renderização MS ao framebuffer

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("ERROR::POSTPROCESSOR: Failed to initialize MSFBO");
        }

        // inicializa também o FBO/textura para o qual o buffer de cor com multisampling será copiado (blit); usado para operações de shader (para efeitos de pós-processamento)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferObject);
        Texture.Generate(width, height, null);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture.ID, 0); // anexa a textura ao framebuffer como seu anexo de cor

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("ERROR::POSTPROCESSOR: Failed to initialize FBO");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // inicializar dados de renderização e uniforms
        InitRenderData();
        PostProcessingShader.SetInteger("scene", 0, true);
        float offset = 1.0f / 300.0f;
        float[,] offsets = new float[9, 2]
        {
            { -offset,  offset  },  // superior esquerdo
            {  0.0f,    offset  },  // superior centro
            {  offset,  offset  },  // superior direito
            { -offset,  0.0f    },  // centro esquerdo
            {  0.0f,    0.0f    },  // centro centro
            {  offset,  0.0f    },  // centro direito
            { -offset, -offset  },  // inferior esquerdo
            {  0.0f,   -offset  },  // inferior centro
            {  offset, -offset  }   // inferior direito
        };

        unsafe
        {
            fixed (float* ptr = offsets)
            {
                GL.Uniform2(GL.GetUniformLocation(PostProcessingShader.ID, "offsets"), 9, (float*)ptr);
            }
        }

        int[] edge_kernel = new int[9]
        {
            -1, -1, -1,
            -1,  8, -1,
            -1, -1, -1
        };

        GL.Uniform1(GL.GetUniformLocation(PostProcessingShader.ID, "edge_kernel"), 9, edge_kernel);

        float[] blur_kernel = new float[9]
        {
            1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f,
            2.0f / 16.0f, 4.0f / 16.0f, 2.0f / 16.0f,
            1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f
        };

        GL.Uniform1(GL.GetUniformLocation(PostProcessingShader.ID, "blur_kernel"), 9, blur_kernel);
    }

    // prepara as operações de framebuffer do pós-processador antes de renderizar o jogo
    public void BeginRender()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _multisampledFramebufferObject);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    // deve ser chamado após a renderização do jogo, para armazenar todos os dados renderizados em um objeto de textura
    public void EndRender()
    {
        // agora resolve o buffer de cor com multisampling para um FBO intermediário, para armazená-lo em uma textura
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _multisampledFramebufferObject);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _framebufferObject);
        GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    // renderiza o quad de textura do PostProcessor (como um sprite grande que cobre a tela inteira)
    public void Render(float time)
    {
        // definir uniforms/opções
        PostProcessingShader.Use();
        PostProcessingShader.SetFloat("time", time);
        PostProcessingShader.SetInteger("confuse", Confuse ? 1 : 0);
        PostProcessingShader.SetInteger("chaos", Chaos ? 1 : 0);
        PostProcessingShader.SetInteger("shake", Shake ? 1 : 0);

        // renderizar quadrilátero texturizado
        GL.ActiveTexture(TextureUnit.Texture0);
        Texture.Bind();

        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
    }

    // estado de renderização
    private uint _multisampledFramebufferObject, _framebufferObject; // MSFBO = FBO com multisampling. O FBO é padrão, usado para copiar (blit) o ​​buffer de cor MS para uma textura.
    private uint _renderbufferObject; // O RBO é usado para buffer de cor com multisampling
    private uint _vertexArrayObject;

    // inicializa o quad para renderizar a textura de pós-processamento
    private void InitRenderData()
    {
        // configurar VAO/VBO
        uint vertexBufferObject;

        float[] vertices =
        {
            // pos          // tex
            -1.0f, -1.0f,   0.0f, 0.0f,
             1.0f, -1.0f,   1.0f, 0.0f,
             1.0f,  1.0f,   1.0f, 1.0f,
            -1.0f, -1.0f,   0.0f, 0.0f,
             1.0f,  1.0f,   1.0f, 1.0f,
            -1.0f,  1.0f,   0.0f, 1.0f
        };

        GL.GenVertexArrays(1, out _vertexArrayObject);
        GL.GenBuffers(1, out vertexBufferObject);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(_vertexArrayObject);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }
}
