using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK;

public class Shader
{
    private int ID;

    public Shader(string vertexPath, string fragmentPath)
    {
        string vShaderCode = File.ReadAllText(vertexPath);
        string fShaderCode = File.ReadAllText(fragmentPath);

        int vertex, fragment;
        int success;
        string infoLog;

        vertex = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertex, vShaderCode);
        GL.CompileShader(vertex);        

        GL.GetShader(vertex, ShaderParameter.CompileStatus, out success);
        if (success == 0)
        {
            GL.GetShaderInfoLog(vertex, out infoLog);
            Console.WriteLine("ERROR::SHADER::VERTEX::COMPILATION_FAILED\n" + infoLog);
        }

        fragment = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragment, fShaderCode);
        GL.CompileShader(fragment);

        GL.GetShader(fragment, ShaderParameter.CompileStatus, out success);
        if (success == 0)
        {
            GL.GetShaderInfoLog(fragment, out infoLog);
            Console.WriteLine("ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n" + infoLog);
        }

        ID = GL.CreateProgram();
        GL.AttachShader(ID, vertex);
        GL.AttachShader(ID, fragment);
        GL.LinkProgram(ID);

        GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out success);
        if (success == 0)
        {
            GL.GetProgramInfoLog(ID, out infoLog);
            Console.WriteLine("ERROR::SHADER::PROGRAM::LINKING_FAILED\n" + infoLog);
        }

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
    }

    public void Use()
    {
        GL.UseProgram(ID);
    }

    public void SetBool(string name, bool value)
    {
        GL.Uniform1(GL.GetUniformLocation(ID, name), value ? 1 : 0);
    }

    public void SetInt(string name, int value)
    {
        GL.Uniform1(GL.GetUniformLocation(ID, name), value);
    }

    public void SetFloat(string name, float value)
    {
        GL.Uniform1(GL.GetUniformLocation(ID, name), value);
    }

    public void SetMatrix4(string name, Matrix4 matrix)
    {
        GL.UniformMatrix4(GL.GetUniformLocation(ID, name), true, ref matrix);
    }
}
