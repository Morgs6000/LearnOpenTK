#version 330 core
out vec4 FragColor;

in vec3 ourPosition;

void main() {
    FragColor = vec4(ourPosition, 1.0f); // observe como o valor da posicao e interpolado linearmente para obter todas as cores diferentes
}
