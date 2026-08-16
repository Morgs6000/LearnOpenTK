/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

namespace LearnOpenTK.src;

// Representa o estado atual do jogo
public enum GameState
{
    GAME_ACTIVE,
    GAME_MENU,
    GAME_WIN
}

// A classe Game encapsula todo o estado e a funcionalidade relacionados ao jogo.
// Ela reúne todos os dados do jogo em uma única classe para
// facilitar o acesso a cada um dos componentes e o gerenciamento.
public class Game : IDisposable
{
    // estado do jogo
    public GameState State;
    public uint Width, Height;

    // construtor
    public Game(uint width, uint height)
    {
        State = GameState.GAME_ACTIVE;

        Width = width;
        Height = height;
    }

    // destrutor
    public void Dispose()
    {
        
    }

    // inicializar o estado do jogo (carregar todos os shaders/texturas/níveis)
    public void Init()
    {
        
    }

    // loop do jogo
    public void ProcessInput(float dt)
    {
        
    }

    public void Update(float dt)
    {
        
    }

    public void Render()
    {
        
    }
}
