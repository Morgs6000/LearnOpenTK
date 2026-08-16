/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK.src;

// Objeto de shader de propósito geral. Compila a partir de um arquivo, gera
// mensagens de erro de compilação/vinculação e disponibiliza várias funções
// utilitárias para facilitar o gerenciamento.
public class Shader
{
    // estado
    public int ID;

    // construtor
    public Shader()
    {
        
    }

    // define o shader atual como ativo
    public Shader Use()
    {
        GL.UseProgram(ID);

        return this;
    }

    // compila o shader a partir do código-fonte fornecido
    public void Compile(string vertexSource, string fragmentSource, string? geometrySource = null)
    {
        int sVertex, sFragment, gShader = 0;

        // vertex Shader
        sVertex = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(sVertex, vertexSource);
        GL.CompileShader(sVertex);
        CheckCompileErrors(sVertex, "VERTEX");

        // fragment Shader
        sFragment = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(sFragment, fragmentSource);
        GL.CompileShader(sFragment);
        CheckCompileErrors(sFragment, "FRAGMENT");

        // se o código-fonte do shader de geometria for fornecido, compile também o shader de geometria
        if (geometrySource != null)
        {
            gShader = GL.CreateShader(ShaderType.GeometryShader);
            GL.ShaderSource(gShader, geometrySource);
            GL.CompileShader(gShader);
            CheckCompileErrors(gShader, "GEOMETRY");
        }

        // programa de shader
        ID = GL.CreateProgram();

        GL.AttachShader(ID, sVertex);
        GL.AttachShader(ID, sFragment);
        if (geometrySource != null)
        {            
            GL.AttachShader(ID, gShader);
        }

        GL.LinkProgram(ID);
        CheckCompileErrors(ID, "PROGRAM");

        // exclua os shaders, pois eles já estão vinculados ao nosso programa e não são mais necessários
        GL.DeleteShader(sVertex);
        GL.DeleteShader(sFragment);
        if (geometrySource != null)
        { 
            GL.DeleteShader(gShader);
        }
    }

    // funções utilitárias
    public void SetFloat(string name, float value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform1(location, value);
    }

    public void SetInteger(string name, int value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform1(location, value);
    }

    public void SetVector2f(string name, float x, float y, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform2(location, x, y);
    }

    public void SetVector2f(string name, Vector2 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform2(location, value);
    }

    public void SetVector3f(string name, float x, float y, float z, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform3(location, x, y, z);
    }

    public void SetVector3f(string name, Vector3 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform3(location, value);
    }

    public void SetVector4f(string name, float x, float y, float z, float w, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform4(location, x, y, z, w);
    }

    public void SetVector4f(string name, Vector4 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.Uniform4(location, value);
    }
    
    public void SetMatrix4(string name, Matrix4 matrix, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = GL.GetUniformLocation(ID, name);
        GL.UniformMatrix4(location, false, ref matrix);
    }

    // verifica se a compilação ou a vinculação falharam e, em caso afirmativo, imprime os logs de erro
    private void CheckCompileErrors(int obj, string type)
    {
        int success;
        string infoLog;

        if (type != "PROGRAM")
        {
            GL.GetShader(obj, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                GL.GetShaderInfoLog(obj, out infoLog);
                Console.WriteLine(
                    "| ERROR::SHADER: Compile-time error: Type: " + type + "\n" +
                    infoLog + "\n" + 
                    " -- --------------------------------------------------- -- "
                );
            }
        }
        else
        {
            GL.GetProgram(obj, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                GL.GetProgramInfoLog(obj, out infoLog);
                Console.WriteLine(
                    "| ERROR::Shader: Link-time error: Type: " + type + "\n" +
                    infoLog + "\n" + 
                    " -- --------------------------------------------------- -- "
                );
            }
        }
    }
}
