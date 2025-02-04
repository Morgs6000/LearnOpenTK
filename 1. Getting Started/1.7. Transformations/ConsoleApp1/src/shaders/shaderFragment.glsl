#version 330
out vec4 FragColor;

uniform sampler2D texture0;
uniform sampler2D texture1;

in vec2 texCoord;

void main() {
    FragColor = mix(texture(texture0, texCoord), texture(texture1, texCoord), 0.2);
}
