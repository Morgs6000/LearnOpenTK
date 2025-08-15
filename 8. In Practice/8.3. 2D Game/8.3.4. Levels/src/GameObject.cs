using OpenTK.Mathematics;

namespace ConsoleApp1.src;

public class GameObject {
    // estado do objeto
    public Vector2 Position, Size, Velocity;
    public Vector3 Color;
    public float Rotation;
    public bool IsSolid;
    public bool Destroyed;
    // renderiza estado
    public Texture Sprite;

    // construtor(es)
    public GameObject() {
        Position = new Vector2(0.0f, 0.0f);
        Size = new Vector2(1.0f, 1.0f);
        Velocity = new Vector2(0.0f);
        Color = new Vector3(1.0f);
        Rotation = 0.0f;
        //Sprite = ;
        IsSolid = false;
        Destroyed = false;
    }

    public GameObject(Vector2 pos, Vector2 size, Texture sprite, Vector3 color, Vector2 velocity) {
        Position = pos;
        Size = size;
        Velocity = velocity;
        Color = color;
        Rotation = 0.0f;
        Sprite = sprite;
        IsSolid = false;
        Destroyed = false;
    }

    // desenha sprite
    public virtual void Draw(SpriteRenderer renderer) {
        renderer.DrawSprite(this.Sprite, this.Position, this.Size, this.Rotation, this.Color);
    }
}
