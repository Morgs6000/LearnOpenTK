#version 330 core
out vec4 FragColor;

in vec3 LightingColor; 

uniform vec3 objectColor;

void main()
{
   FragColor = vec4(LightingColor * objectColor, 1.0);
}

/*
Então, o que vemos?
É possível observar (pessoalmente ou na imagem fornecida) a distinção clara entre os dois triângulos na parte frontal do cubo. Essa "faixa" é visível devido à interpolação de fragmentos. Na imagem de exemplo, vemos que o vértice superior direito da face frontal do cubo apresenta brilho especular. Como o vértice superior direito do triângulo inferior direito está iluminado, mas os outros dois vértices desse triângulo não, os valores de luminosidade são interpolados para esses outros dois vértices. O mesmo ocorre com o triângulo superior esquerdo. Visto que as cores dos fragmentos intermediários não provêm diretamente da fonte de luz, mas são resultado de interpolação, a iluminação nesses fragmentos fica incorreta; além disso, as luminosidades dos triângulos superior esquerdo e inferior direito entram em conflito, resultando em uma faixa visível entre eles.

Esse efeito torna-se mais evidente ao utilizar formas mais complexas.
*/
