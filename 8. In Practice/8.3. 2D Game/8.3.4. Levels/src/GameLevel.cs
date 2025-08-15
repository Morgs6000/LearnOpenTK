using OpenTK.Mathematics;

namespace ConsoleApp1.src;

public class GameLevel {
    // estado do nível
    public List<GameObject> Bricks = new List<GameObject>();

    // construtor
    public GameLevel() {

    }

    // carrega o nível do arquivo
    public void Load(string file, int levelWidth, int levelHeight) {
        // limpa dados antigos
        this.Bricks.Clear();
        // carrega do arquivo
        string[] lines = File.ReadAllLines(file);
        List<List<int>> tileData = new List<List<int>>();
        foreach(string line in lines) {
            List<int> row = new List<int>();
            string[] tiles = line.Split(' ');
            foreach(string tile in tiles) {
                if(int.TryParse(tile, out int tileCode)) {
                    row.Add(tileCode);
                }
            }
            tileData.Add(row);
        }
        if(tileData.Count > 0) {
            init(tileData, levelWidth, levelHeight);
        }
    }

    // nível de renderização
    public void Draw(SpriteRenderer renderer) {
        foreach(GameObject tile in this.Bricks) {
            if(!tile.Destroyed) {
                tile.Draw(renderer);
            }
        }
    }

    // verifica se o nível foi concluído (todas as peças não sólidas são destruídas)
    public bool IsCompleted() {
        foreach(GameObject tile in this.Bricks) {
            if(!tile.IsSolid && !tile.Destroyed) {
                return false;
            }
        }
        return true;
    }

    // inicializa o nível a partir dos dados do bloco
    private void init(List<List<int>> tileData, int levelWidth, int levelHeight) {
        // calcula dimensões
        int height = tileData.Count;
        int width = tileData[0].Count;
        float unit_width = levelWidth / (float)width;
        float unit_height = levelHeight / height;
        // inicializa blocos de nível com base em tileData
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                // verifica o tipo de bloco a partir dos dados de nível (matriz de nível 2D)
                if(tileData[y][x] == 1) { // sólido
                    Vector2 pos = new Vector2(unit_width * x, unit_height * y);
                    Vector2 size = new Vector2(unit_width, unit_height);
                    GameObject obj = new GameObject(pos, size, ResourceManager.GetTexture("block_solid"), new Vector3(0.8f, 0.8f, 0.7f), new Vector2(0.0f));
                    obj.IsSolid = true;
                    this.Bricks.Add(obj);
                }
                else if(tileData[y][x] > 1) {
                    Vector3 color = new Vector3(1.0f); // original: branco
                    if(tileData[y][x] == 2) {
                        color = new Vector3(0.2f, 0.6f, 1.0f);
                    }
                    if(tileData[y][x] == 3) {
                        color = new Vector3(0.0f, 0.7f, 0.0f);
                    }
                    if(tileData[y][x] == 4) {
                        color = new Vector3(0.8f, 0.8f, 0.4f);
                    }
                    if(tileData[y][x] == 5) {
                        color = new Vector3(1.0f, 0.5f, 0.0f);
                    }

                    Vector2 pos = new Vector2(unit_width * x, unit_height * y);
                    Vector2 size = new Vector2(unit_width, unit_height);
                    this.Bricks.Add(new GameObject(pos, size, ResourceManager.GetTexture("block"), color, new Vector2(0.0f)));
                }
            }
        }
    }
}
