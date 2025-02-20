﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

public class Window : GameWindow {
    private Shader shader;

    private Texture texture0;
    private Texture texture1;

    private float[] vertices = {
        //Position          Texture coordinates
        -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, // bottom left
         0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // bottom right
         0.5f,  0.5f, 0.0f, 1.0f, 1.0f, // top right
        -0.5f,  0.5f, 0.0f, 0.0f, 1.0f  // top left
    };

    private uint[] indices = {  // note that we start from 0!
        0, 1, 2,   // first triangle
        0, 2, 3    // second triangle
    };

    private int vertexArrayObject;
    private int vertexBufferObject;
    private int elementBufferObject;

    public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    protected override void OnLoad() {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        shader = new Shader("../../../src/shaders/Vertex.glsl", "../../../src/shaders/Fragment.glsl");

        texture0 = new Texture("../../../src/textures/container.jpg");
        texture1 = new Texture("../../../src/textures/awesomeface.png");

        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);
        
        vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);

        int vertexLocation = shader.GetAttribLocation("aPos");
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(vertexLocation);

        int texCoordLocation = shader.GetAttribLocation("aTexCoord");
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(texCoordLocation);

        elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StreamDraw);
    }

    protected override void OnUpdateFrame(FrameEventArgs args) {
        base.OnUpdateFrame(args);

        if(KeyboardState.IsKeyDown(Keys.Escape)) {
            Close();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        shader.Use();

        shader.SetInt("texture0", 0);
        texture0.Use(TextureUnit.Texture0);

        shader.SetInt("texture1", 1);
        texture1.Use(TextureUnit.Texture1);

        GL.BindVertexArray(vertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        /*
        Vector4 vec = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        Matrix4 trans = Matrix4.CreateTranslation(1.0f, 1.0f, 0.0f);
        vec *= trans;
        Console.WriteLine("{0}, {1}, {2}", vec.X, vec.Y, vec.Z);
        */

        Matrix4 rotaton = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90.0f));
        Matrix4 scale = Matrix4.CreateScale(0.5f, 0.5f, 0.5f);
        Matrix4 trans = rotaton * scale;

        int location = shader.GetUniformLocation("transform");
        GL.UniformMatrix4(location, true, ref trans);

        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e) {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
    }
}
