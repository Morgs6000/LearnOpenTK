﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ConsoleApp1.src;

public class Window : GameWindow {
    private Shader shaderOrange;
    private Shader shaderYellow;

    float[] firstTriangle = {
        -0.9f, -0.5f, 0.0f,  // left 
        -0.0f, -0.5f, 0.0f,  // right
        -0.45f, 0.5f, 0.0f,  // top 
    };

    float[] secondTriangle = {
        0.0f, -0.5f, 0.0f,  // left
        0.9f, -0.5f, 0.0f,  // right
        0.45f, 0.5f, 0.0f   // top 
    };

    private int[] vertexArrayObject = new int[2];
    private int[] vertexBufferObject = new int[2];

    public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {
        CenterWindow();
    }

    protected override void OnLoad() {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        shaderOrange = new Shader("../../../src/shaders/Vertex.glsl", "../../../src/shaders/Fragment1.glsl");
        shaderYellow = new Shader("../../../src/shaders/Vertex.glsl", "../../../src/shaders/Fragment2.glsl");

        // configuração do primeiro triângulo
        vertexArrayObject[0] = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject[0]);

        vertexBufferObject[0] = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject[0]);
        GL.BufferData(BufferTarget.ArrayBuffer, firstTriangle.Length * sizeof(float), firstTriangle, BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // configuração do segundo triângulo
        vertexArrayObject[1] = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject[1]);

        vertexBufferObject[1] = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject[1]);
        GL.BufferData(BufferTarget.ArrayBuffer, secondTriangle.Length * sizeof(float), secondTriangle, BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
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

        // agora, quando desenharmos o triângulo, primeiro usaremos o vértice e o shader de fragmento laranja
        shaderOrange.Use();
        GL.BindVertexArray(vertexArrayObject[0]);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // quando desenhamos o segundo triângulo, queremos usar um shader diferente, então mudamos para o shader com nosso shader de fragmento amarelo
        shaderYellow.Use();
        GL.BindVertexArray(vertexArrayObject[1]);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e) {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
    }
}
