using OpenTK.Graphics.OpenGL4;

namespace ConsoleApp1.src;

public class ResourceManager {
    public static Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();
    public static Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

    public static Shader LoadShader(string vShaderFile, string fShaderFile, string name) {
        Shaders[name] = LoadShaderFromFile(vShaderFile, fShaderFile);
        return Shaders[name];
    }

    public static Texture LoadTexture(string file, string name) {
        Textures[name] = LoadTextureFromFile(file);
        return Textures[name];
    }

    public static Shader GetShader(string name) {
        return Shaders[name];
    }

    public static Texture GetTexture(string name) {
        return Textures[name];
    }

    private static Shader LoadShaderFromFile(string vShaderFile, string fShaderFile) {
        Shader shader = new Shader(vShaderFile, fShaderFile);
        return shader;
    }

    private static Texture LoadTextureFromFile(string file) {
        Texture texture = new Texture(file);
        return texture;
    }

    public static void Clear() {
        foreach(var inter in Shaders.Values) {
            GL.DeleteProgram(inter.handle);
        }

        foreach(var inter in Textures.Values) {
            GL.DeleteTextures(1, ref inter.handle);
        }
    }
}
