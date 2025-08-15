#version 330 core
layout (location = 0) in vec4 vertex; // <vec2 position, vec2 texCoords>

out vec2 TexCoords;

uniform mat4 model;
// observe que estamos omitindo a matriz de visualizacao; a visao nunca muda, entao basicamente temos uma matriz de visao de identidade e podemos, portanto, omiti-la.
uniform mat4 projection;

void main() {
    TexCoords = vertex.zw;
    gl_Position = projection * model * vec4(vertex.xy, 0.0, 1.0);
}
