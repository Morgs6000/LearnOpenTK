#version 330 core
layout(location = 0) in vec3 aPos;

uniform float xOffset;

void main() {
    gl_Position = vec4(aPos.x + xOffset, aPos.y, aPos.z, 1.0f); // adicione o xOffset a posicao x da posicao do vertice
}
