using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

/// <summary>
/// Fornece acesso às informações exibidas.
/// </summary>
public class Screen
{
    private static GameWindow _gameWindow = null!;

    /// <summary>
    /// Largura atual da janela da tela em pixels (somente leitura).
    /// </summary>
    public static int widht
    {
        get
        {
            return _gameWindow.ClientSize.X;
        }
        private set
        {
            
        }
    }

    /// <summary>
    /// Altura atual da janela da tela em pixels (somente leitura).
    /// </summary>
    public static int height
    {
        get
        {
            return _gameWindow.ClientSize.Y;
        }
        private set
        {
            
        }
    }

    public static void Resize(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
    }
}
