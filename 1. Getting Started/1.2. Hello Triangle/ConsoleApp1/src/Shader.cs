using OpenTK.Graphics.OpenGL4;

namespace ConsoleApp1.src;

// Uma classe simples destinada a ajudar a criar shaders.
public class Shader {
    int Handle;

    // É assim que você cria um shader simples.
    // Shaders são escritos em GLSL, que é uma linguagem muito similar a C em sua semântica.
    // O código-fonte GLSL é compilado *em tempo de execução*, para que ele possa se otimizar para a placa de vídeo em que está sendo usado no momento.
    // Um ​​exemplo comentado de GLSL pode ser encontrado em shader.vert.
    public Shader(string vertexPath, string fragmentPath) {
        // Existem vários tipos diferentes de shaders, mas os únicos dois que você precisa para renderização básica são os shaders de vértice e fragmento.
        // O shader de vértice é responsável por mover os vértices e carregar esses dados para o shader de fragmento.
        // O shader de vértice não será muito importante aqui, mas será mais importante depois.
        // O shader de fragmento é responsável por converter os vértices em "fragmentos", que representam todos os dados que o OpenGL precisa para desenhar um pixel.
        // O shader de fragmento é o que mais usaremos aqui.

        // Carregar vertex shader e compilar
        string VertexShaderSource = File.ReadAllText(vertexPath);

        // GL.CreateShader criará um shader vazio (obviamente). O enum ShaderType denota qual tipo de shader será criado.
        var VertexShader = GL.CreateShader(ShaderType.VertexShader);

        // Agora, vincule o código-fonte GLSL
        GL.ShaderSource(VertexShader, VertexShaderSource);

        // E então compilar
        GL.CompileShader(VertexShader);

        // Fazemos o mesmo para o shader de fragmento.
        string FragmentShaderSource = File.ReadAllText(fragmentPath);
        var FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(FragmentShader, FragmentShaderSource);
        GL.CompileShader(FragmentShader);

        // Verifique se há erros de compilação

        int success;

        GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out success);
        if(success == 0) {
            string infoLog = GL.GetShaderInfoLog(VertexShader);
            Console.WriteLine(infoLog);
        }

        GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out success);
        if(success == 0) {
            string infoLog = GL.GetShaderInfoLog(FragmentShader);
            Console.WriteLine(infoLog);
        }

        // Esses dois shaders devem então ser mesclados em um programa shader, que pode então ser usado pelo OpenGL.
        // Para fazer isso, crie um programa...
        Handle = GL.CreateProgram();

        // Anexar ambos os shaders...
        GL.AttachShader(Handle, VertexShader);
        GL.AttachShader(Handle, FragmentShader);

        // E então vincule-os.
        GL.LinkProgram(Handle);

        // Verifique se há erros de vinculação
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
        if(success == 0) {
            string infoLog = GL.GetProgramInfoLog(Handle);
            Console.WriteLine(infoLog);
        }

        // Quando o programa shader é vinculado, ele não precisa mais dos shaders individuais anexados a ele; o código compilado é copiado para o programa shader.
        // Desanexe-os e, em seguida, exclua-os.
        GL.DetachShader(Handle, VertexShader);
        GL.DetachShader(Handle, FragmentShader);
        GL.DeleteShader(FragmentShader);
        GL.DeleteShader(VertexShader);
    }

    // Uma função wrapper que habilita o programa shader.
    public void Use() {
        GL.UseProgram(Handle);
    }

    // As fontes de shader fornecidas com este projeto usam layout(location)-s codificados. Se você quiser fazer isso dinamicamente, você pode omitir as linhas layout(location=X) no vertex shader e usar isso em VertexAttribPointer em vez dos valores codificados.
    public int GetAttribLocation(string attribName) {
        return GL.GetAttribLocation(Handle, attribName);
    }

    /* ... */

    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing) {
        if(!disposedValue) {
            GL.DeleteProgram(Handle);

            disposedValue = true;
        }
    }

    ~Shader() {
        if(disposedValue == false) {
            Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
        }
    }


    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
