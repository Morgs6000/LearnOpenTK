using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK.src;

public class Shader
{
    public int ID;

    // o construtor gera o shader em tempo de execução
    // --------------------------------------------------
    public Shader(string vertexPath, string fragmentPath)
    {
        // 1. recuperar o código-fonte do vértice/fragmento a partir de filePath
        string vertexCode = string.Empty;
        string fragmentCode = string.Empty;

        try
        {
            vertexCode = RemoveComments(File.ReadAllText(vertexPath));
            fragmentCode = RemoveComments(File.ReadAllText(fragmentPath));
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR::SHADER::FILE_NOT_SUCCESSFULLY_READ: " + e.Message);
        }

        string vShaderCode = vertexCode;
        string fShaderCode = fragmentCode;

        // 2. compilar shaders
        int vertex, fragment;

        // shader de vértice
        vertex = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertex, vShaderCode);
        GL.CompileShader(vertex);
        CheckCompileErrors(vertex, "VERTEX");

        // shader de fragmento
        fragment = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragment, fShaderCode);
        GL.CompileShader(fragment);
        CheckCompileErrors(fragment, "FRAGMENT");

        // Programa de shader
        ID = GL.CreateProgram();
        GL.AttachShader(ID, vertex);
        GL.AttachShader(ID, fragment);
        GL.LinkProgram(ID);
        CheckCompileErrors(ID, "PROGRAM");

        // exclua os shaders, pois eles já estão vinculados ao nosso programa e não são mais necessários
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
    }

    // ativa o shader
    // --------------------------------------------------
    public void Use()
    {
        GL.UseProgram(ID);
    }

    // funções utilitárias de uniformes
    // --------------------------------------------------
    public void SetBool(string name, bool value)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform1(location, value ? 1 : 0);
    }
    // --------------------------------------------------
    public void SetInt(string name, int value)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform1(location, value);
    }
    // --------------------------------------------------
    public void SetFloat(string name, float value)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform1(location, value);
    }
    // --------------------------------------------------
    public void SetVec2(string name, float x, float y)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform2(location, x, y);
    }
    public void SetVec2(string name, Vector2 value)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform2(location, value);
    }
    // --------------------------------------------------
    public void SetVec3(string name, float x, float y, float z)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform3(location, x, y, z);
    }
    public void SetVec3(string name, Vector3 value)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform3(location, value);
    }
    // --------------------------------------------------
    public void SetVec4(string name, float x, float y, float z, float w)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform4(location, x, y, z, w);
    }
    public void SetVec4(string name, Vector4 value)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform4(location, value);
    }
    // --------------------------------------------------
    public void SetMat2(string name, Matrix2 mat)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.UniformMatrix2(location, false, ref mat);
    }
    // --------------------------------------------------
    public void SetMat3(string name, Matrix3 mat)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.UniformMatrix3(location, false, ref mat);
    }
    // --------------------------------------------------
    public void SetMat4(string name, Matrix4 mat)
    {
        int location = GL.GetUniformLocation(ID, name);
        GL.UniformMatrix4(location, false, ref mat);
    }

    // função utilitária para verificar erros de compilação/vinculação de shader.
    // --------------------------------------------------
    private void CheckCompileErrors(int shader, string type)
    {
        int success;
        string infoLog;

        if (type != "PROGRAM")
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                GL.GetShaderInfoLog(shader, out infoLog);
                Console.WriteLine(
                    "ERROR::SHADER_COMPILATION_ERROR of type: " + type + "\n" +
                    infoLog + "\n" +
                    " -- --------------------------------------------------- -- "
                );
            }
        }
        else
        {
            GL.GetProgram(shader, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                GL.GetProgramInfoLog(shader, out infoLog);
                Console.WriteLine(
                    "ERROR::PROGRAM_LINKING_ERROR of type: " + type + "\n" +
                    infoLog + "\n" +
                    " -- --------------------------------------------------- -- "
                );
            }
        }
    }

    private string RemoveComments(string source)
    {
        // Regex para encontrar comentários de bloco (/* */) e de linha (//)
        // O @"" é uma string literal, e o padrão lida com ambos os casos.
        string blockComments = @"/\*(.*?)\*/";
        string lineComments = @"//(.*?)\r?\n";

        // Primeiro removemos os blocos (multiline)
        string noBlockComments = Regex.Replace(source, blockComments, "", RegexOptions.Singleline);
        
        // Depois removemos as linhas simples
        string cleanedSource = Regex.Replace(noBlockComments, lineComments, Environment.NewLine);

        return cleanedSource.Trim();
    } 
}
