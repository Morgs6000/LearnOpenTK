using Assimp;
using Assimp.Unmanaged;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using AssimpMesh = Assimp.Mesh;

namespace LearnOpenTK.src;

public class Model
{
    // dados do modelo
    public List<Texture> textures_loaded = []; // armazena todas as texturas carregadas até o momento; uma otimização para garantir que as texturas não sejam carregadas mais de uma vez.
    public List<Mesh> meshes = [];
    public string directory = string.Empty;
    public bool gammaCorrection;

    // construtor, espera um caminho de arquivo para um modelo 3D.
    public Model(string path, bool gamma = false)
    {
        gammaCorrection = gamma;

        LoadModel(path);
    }

    // desenha o modelo e, consequentemente, todas as suas malhas
    public void Draw(Shader shader)
    {
        for (int i = 0; i < meshes.Count(); i++)
        {
            meshes[i].Draw(shader);
        }
    }

    // carrega um modelo a partir de um arquivo, utilizando extensões suportadas pelo ASSIMP, e armazena as malhas resultantes no vetor de malhas.
    private void LoadModel(string path)
    {
        // ler arquivo via ASSIMP
        AssimpContext importer = new AssimpContext();
        Scene scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);

        // verificar se há erros
        if (scene == null || (scene.SceneFlags & SceneFlags.Incomplete) == SceneFlags.Incomplete || scene.RootNode == null)
        {
            Console.WriteLine("ERROR::ASSIMP:: " + AssimpLibrary.Instance.GetErrorString());
            return;
        }

        // obtém o caminho do diretório a partir do caminho do arquivo
        directory = path.Substring(0, path.LastIndexOf('/'));

        // processa o nó raiz do ASSIMP recursivamente
        ProcessNode(scene.RootNode, scene);
    }

    // processa um nó de forma recursiva. Processa cada malha individual localizada no nó e repete esse processo nos nós filhos (se houver).
    private void ProcessNode(Node node, Scene scene)
    {
        // processa cada malha localizada no nó atual
        for (int i = 0; i < node.MeshCount; i++)
        {
            // o objeto de nó contém apenas índices para referenciar os objetos reais na cena. 
            // a cena contém todos os dados; o nó serve apenas para manter as coisas organizadas (como as relações entre nós).
            AssimpMesh mesh = scene.Meshes[node.MeshIndices[i]];
            meshes.Add(ProcessMesh(mesh, scene));
        }

        // após processarmos todas as malhas (se houver), processamos recursivamente cada um dos nós filhos
        for (int i = 0; i < node.ChildCount; i++)
        {
            ProcessNode(node.Children[i], scene);
        }
    }

    private Mesh ProcessMesh(AssimpMesh mesh, Scene scene)
    {
        // dados a preencher
        List<Vertex> vertices = [];
        List<int> indices = [];
        List<Texture> textures = [];

        // percorre cada um dos vértices da malha
        for (int i = 0; i < mesh.VertexCount; i++)
        {
            Vertex vertex = new Vertex();
            Vector3 vector; // Declaramos um vetor temporário, já que o Assimp utiliza sua própria classe de vetor que não é convertida diretamente para a classe vec3 do GLM; portanto, transferimos os dados para esse glm::vec3 temporário primeiro.

            // posições
            vector.X = mesh.Vertices[i].X;
            vector.Y = mesh.Vertices[i].Y;
            vector.Z = mesh.Vertices[i].Z;

            vertex.Position = vector;

            // normais
            if (mesh.HasNormals)
            {
                vector.X = mesh.Normals[i].X;
                vector.Y = mesh.Normals[i].Y;
                vector.Z = mesh.Normals[i].Z;

                vertex.Normal = vector;
            }

            // coordenadas de textura
            if (mesh.TextureCoordinateChannels[0] != null) // a malha contém coordenadas de textura?
            {
                Vector2 vec;

                // Um ​​vértice pode conter até 8 coordenadas de textura diferentes. Portanto, assumimos que não
                // utilizaremos modelos nos quais um vértice possa ter múltiplas coordenadas de textura; assim, sempre utilizamos o primeiro conjunto (0).
                vec.X = mesh.TextureCoordinateChannels[0][i].X;
                vec.Y = mesh.TextureCoordinateChannels[0][i].Y;

                vertex.TexCoords = vec;

                // tangente
                vector.X = mesh.Tangents[i].X;
                vector.Y = mesh.Tangents[i].Y;
                vector.Z = mesh.Tangents[i].Z;

                vertex.Tangent = vector;

                // bitangente
                vector.X = mesh.BiTangents[i].X;
                vector.Y = mesh.BiTangents[i].Y;
                vector.Z = mesh.BiTangents[i].Z;

                vertex.Bitangent = vector;
            }
            else
            {
                vertex.TexCoords = new Vector2(0.0f, 0.0f);
            }

            vertices.Add(vertex);
        }

        // agora, percorra cada uma das faces da malha (uma face é um triângulo da malha) e obtenha os índices de vértice correspondentes.
        for (int i = 0; i < mesh.FaceCount; i++)
        {
            Face face = mesh.Faces[i];

            // recupera todos os índices da face e os armazena no vetor de índices
            for (int j = 0; j < face.IndexCount; j++)
            {
                indices.Add(face.Indices[j]);
            }
        }

        // processar materiais
        Material material = scene.Materials[mesh.MaterialIndex];

        // Adotamos uma convenção para os nomes dos samplers nos shaders. Cada textura difusa deve ser nomeada
        // como 'texture_diffuseN', onde N é um número sequencial de 1 a MAX_SAMPLER_NUMBER. 
        // O mesmo se aplica a outras texturas, conforme resumido na lista a seguir:
        // difusa: texture_diffuseN
        // especular: texture_specularN
        // normal: texture_normalN

        // 1. mapas de difusão
        List<Texture> diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse");
        textures.AddRange(diffuseMaps);

        // 2. mapas de especularidade
        List<Texture> specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular");
        textures.AddRange(specularMaps);

        // 3. mapas de normais
        List<Texture> normalMaps = LoadMaterialTextures(material, TextureType.Height, "texture_normal");
        textures.AddRange(normalMaps);

        // 4. mapas de altura
        List<Texture> heightMaps = LoadMaterialTextures(material, TextureType.Ambient, "texture_height");
        textures.AddRange(heightMaps);

        // retorna um objeto de malha criado a partir dos dados de malha extraídos
        return new Mesh(vertices, indices, textures);
    }

    // verifica todas as texturas de material de um determinado tipo e carrega as texturas caso ainda não tenham sido carregadas. 
    // as informações necessárias são retornadas como uma estrutura Texture.
    private List<Texture> LoadMaterialTextures(Material mat, TextureType type, string typeName)
    {
        List<Texture> textures = [];

        for (int i = 0; i < mat.GetMaterialTextureCount(type); i++)
        {
            TextureSlot str;
            mat.GetMaterialTexture(type, i, out str);

            // verifica se a textura já foi carregada anteriormente e, em caso afirmativo, prossegue para a próxima iteração: pula o carregamento de uma nova textura
            bool skip = false;

            for (int j = 0; j < textures_loaded.Count(); j++)
            {
                if (textures_loaded[j].path == str.FilePath)
                {
                    textures.Add(textures_loaded[j]);
                    skip = true; // uma textura com o mesmo caminho de arquivo já foi carregada; prossiga para a próxima. (otimização)
                    break;
                }
            }
            if (!skip)
            {
                // se a textura ainda não tiver sido carregada, carregue-a
                Texture texture;
                texture.id = TextureFromFile(str.FilePath, directory);
                texture.type = typeName;
                texture.path = str.FilePath;

                textures.Add(texture);
                textures_loaded.Add(texture); // Armazena como textura carregada para o modelo inteiro, para garantir que não carregaremos texturas duplicadas desnecessariamente.
            }
        }

        return textures;
    }

    private uint TextureFromFile(string path, string directory, bool gamma = false)
    {
        string filename = path;
        filename = directory + '/' + filename;

        uint textureID;
        GL.GenTextures(1, out textureID);

        int width, height;
        byte[] data;

        ImageResult image;

        using (FileStream stream = File.OpenRead(filename))
        {
            image = ImageResult.FromStream(stream, ColorComponents.Default);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgb;
            PixelFormat pixelFormat = PixelFormat.Rgb;

            if (image.Comp == ColorComponents.Grey)
            {
                pixelInternalFormat = PixelInternalFormat.CompressedRed;
                pixelFormat = PixelFormat.Red;
            }
            else if (image.Comp == ColorComponents.RedGreenBlue)
            {
                pixelInternalFormat = PixelInternalFormat.Rgb;
                pixelFormat = PixelFormat.Rgb;
            }
            else if (image.Comp == ColorComponents.RedGreenBlueAlpha)
            {
                pixelInternalFormat = PixelInternalFormat.Rgba;
                pixelFormat = PixelFormat.Rgba;
            }

            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, pixelInternalFormat, width, height, 0, pixelFormat, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura no caminho: " + path);
        }

        return textureID;
    }
}
