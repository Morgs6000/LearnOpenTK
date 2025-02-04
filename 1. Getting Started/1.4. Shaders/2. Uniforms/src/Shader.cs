using OpenTK.Graphics.OpenGL4;

namespace ConsoleApp1.src;

public class Shader {
    public readonly int Handle;

    public Shader(string vertexPath, string fragmentPath) {
        string VertexShaderSource = File.ReadAllText(vertexPath);

        var VertexShader = GL.CreateShader(ShaderType.VertexShader);

        GL.ShaderSource(VertexShader, VertexShaderSource);

        GL.CompileShader(VertexShader);

        string FragmentShaderSource = File.ReadAllText(fragmentPath);
        var FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(FragmentShader, FragmentShaderSource);
        GL.CompileShader(FragmentShader);

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

        Handle = GL.CreateProgram();

        GL.AttachShader(Handle, VertexShader);
        GL.AttachShader(Handle, FragmentShader);

        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
        if(success == 0) {
            string infoLog = GL.GetProgramInfoLog(Handle);
            Console.WriteLine(infoLog);
        }

        GL.DetachShader(Handle, VertexShader);
        GL.DetachShader(Handle, FragmentShader);
        GL.DeleteShader(FragmentShader);
        GL.DeleteShader(VertexShader);
    }

    public void Use() {
        GL.UseProgram(Handle);
    }

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
