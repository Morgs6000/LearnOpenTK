using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK.src;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    private const int MAX_BONE_INFLUENCE = 4;

    // posição
    public Vector3 Position;

    // normal
    public Vector3 Normal;

    // coordenadas de textura
    public Vector2 TexCoords;

    // tangente
    public Vector3 Tangent;

    // bitangente
    public Vector3 Bitangent;

    // índices de ossos que influenciarão este vértice
    public unsafe fixed int m_BoneIDs[MAX_BONE_INFLUENCE];

    // pesos de cada osso
    public unsafe fixed int m_Weights[MAX_BONE_INFLUENCE];
}

public struct Texture
{
    public uint id;
    public string type;
    public string path;
}

public class Mesh
{
    // Dados da malha
    public List<Vertex> vertices = [];
    public List<int> indices = [];
    public List<Texture> textures = [];
    public uint VAO;

    // construtor
    public Mesh(List<Vertex> vertices, List<int> indices, List<Texture> textures)
    {
        this.vertices = vertices;
        this.indices = indices;
        this.textures = textures;

        // agora que temos todos os dados necessários, defina os buffers de vértices e seus ponteiros de atributos.
        SetupMesh();
    }

    // renderizar a malha
    public void Draw(Shader shader)
    {
        // vincular as texturas apropriadas
        uint diffuseNr = 1;
        uint specularNr = 1;
        uint normalNr = 1;
        uint heightNr = 1;

        for (int i = 0; i < textures.Count; i++)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + i); // ativar a unidade de textura correta antes de vincular

            // obtém o número da textura (o N em diffuse_textureN)
            string number = string.Empty;
            string name = textures[i].type;

            if (name == "texture_diffuse")
            {
                number = diffuseNr++.ToString();
            }
            else if (name == "texture_specular")
            {
                number = specularNr++.ToString(); // converte unsigned int para string
            }
            else if (name == "texture_normal")
            {
                number = normalNr++.ToString(); // converte unsigned int para string
            }
            else if (name == "texture_height")
            {
                number = heightNr++.ToString(); // converte unsigned int para string
            }

            // agora defina o sampler para a unidade de textura correta
            GL.Uniform1(GL.GetUniformLocation(shader.ID, name + number), i);

            // e, finalmente, vincule a textura
            GL.BindTexture(TextureTarget.Texture2D, textures[i].id);
        }

        // desenhar malha
        GL.BindVertexArray(VAO);
        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);

        // É sempre uma boa prática restaurar tudo para os padrões após a configuração.
        GL.ActiveTexture(TextureUnit.Texture0);
    }

    // renderizar dados
    private uint VBO, EBO;

    // inicializa todos os objetos/arrays de buffer
    private void SetupMesh()
    {
        // criar buffers/arrays
        GL.GenVertexArrays(1, out VAO);
        GL.GenBuffers(1, out VBO);
        GL.GenBuffers(1, out EBO);

        GL.BindVertexArray(VAO);

        // carregar dados em buffers de vértices
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

        // Uma grande vantagem das structs é que o layout de memória de seus itens é sequencial. 
        // Isso significa que podemos simplesmente passar um ponteiro para a struct, e ela é traduzida perfeitamente para um array de glm::vec3/2,
        // que por sua vez é traduzido para 3 ou 2 valores do tipo float, resultando finalmente em um array de bytes.
        unsafe
        {
            fixed (Vertex* buf = vertices.ToArray())
            {
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(Vertex), (nint)buf, BufferUsageHint.StaticDraw);
            }
        }

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        unsafe
        {
            fixed (int* buf = indices.ToArray())
            {
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(int), (nint)buf, BufferUsageHint.StaticDraw);
            }
        }

        // define os ponteiros de atributos de vértice

        // Posições dos vértices
        GL.EnableVertexAttribArray(0);
        unsafe
        {
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), 0);
        }

        // normais dos vértices
        GL.EnableVertexAttribArray(1);
        unsafe
        {
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal)));
        }

        // coordenadas de textura do vértice
        GL.EnableVertexAttribArray(2);
        unsafe
        {
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoords)));
        }

        // tangente do vértice
        GL.EnableVertexAttribArray(3);
        unsafe
        {
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>(nameof(Vertex.Tangent)));
        }

        // bitangente do vértice
        GL.EnableVertexAttribArray(4);
        unsafe
        {
            GL.VertexAttribPointer(4, 5, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>(nameof(Vertex.Bitangent)));
        }

        // ids
        GL.EnableVertexAttribArray(5);
        unsafe
        {
            GL.VertexAttribIPointer(5, 4, VertexAttribIntegerType.Int, sizeof(Vertex), Marshal.OffsetOf<Vertex>(nameof(Vertex.m_BoneIDs)));
        }

        // pesos
        GL.EnableVertexAttribArray(6);
        unsafe
        {
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>(nameof(Vertex.m_Weights)));
        }

        GL.BindVertexArray(0);
    }
}
