using OpenTK.Mathematics;

namespace LearnOpenTK;

public enum Camera_Movement
{
    FORWARD,
    BACKWARD,
    LEFT,
    RIGHT
}

public class Camera
{
    private const float YAW = -90.0f;
    private const float PITCH = 0.0f;
    private const float SPEED = 2.5f;
    private const float SENSITIVITY = 0.1f;
    private const float ZOOM = 45.0f;

    public Vector3 Position;
    public Vector3 Front;
    public Vector3 Up;
    public Vector3 Right;
    public Vector3 WorldUp;

    public float Yaw;
    public float Pitch;

    public float MovementSpeed;
    public float MouseSensitivity;
    public float Zoom;

    public Camera(Vector3 position = default, Vector3 up = default, float yaw = YAW, float pitch = PITCH)
    {
        Position = position == default ? new Vector3(0.0f, 0.0f, 0.0f) : position;
        WorldUp = up == default ? new Vector3(0.0f, 1.0f, 0.0f) : up;
        Yaw = yaw;
        Pitch = pitch;
        Front = new Vector3(0.0f, 0.0f, -1.0f);
        MovementSpeed = SPEED;
        MouseSensitivity = SENSITIVITY;
        Zoom = ZOOM;

        UpdateCameraVectors();
    }

    public Camera(float posX, float posY, float posZ, float upX, float upY, float upZ, float yaw, float pitch)
    {
        Position = new Vector3(posX, posY, posZ);
        WorldUp = new Vector3(upX, upY, upZ);
        Yaw = yaw;
        Pitch = pitch;
        Front = new Vector3(0.0f, 0.0f, -1.0f);
        MovementSpeed = SPEED;
        MouseSensitivity = SENSITIVITY;
        Zoom = ZOOM;

        UpdateCameraVectors();
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Front, Up);
    }

    public void ProcessKeyboard(Camera_Movement direction, float deltaTime)
    {
        float velocity = MovementSpeed * deltaTime;

        if (direction == Camera_Movement.FORWARD)
        {
            Position += velocity * Front;
        }
        if (direction == Camera_Movement.BACKWARD)
        {
            Position -= velocity * Front;
        }
        if (direction == Camera_Movement.LEFT)
        {
            Position -= velocity * Right;
        }
        if (direction == Camera_Movement.RIGHT)
        {
            Position += velocity * Right;
        }
    }

    public void ProcessMouseMovement(float xoffset, float yoffset, bool constrainPitch = true)
    {
        xoffset *= MouseSensitivity;
        yoffset *= MouseSensitivity;

        Yaw += xoffset;
        Pitch += yoffset;

        if (constrainPitch)
        {
            if (Pitch > 89.0f)
            {
                Pitch = 89.0f;
            }
            if (Pitch < -89.0f)
            {
                Pitch = -89.0f;
            }
        }

        UpdateCameraVectors();
    }
    
    public void ProcessMouseScroll(float yoffset)
    {
        Zoom -= yoffset;

        if(Zoom < 1.0f)
        {
            Zoom = 1.0f;
        }
        if(Zoom > 45.0f)
        {
            Zoom = 45.0f;
        }
    }

    private void UpdateCameraVectors()
    {
        Vector3 front;
        front.X = (float)(Math.Cos(MathHelper.DegreesToRadians(Pitch)) * Math.Cos(MathHelper.DegreesToRadians(Yaw)));
        front.Y = (float)(Math.Sin(MathHelper.DegreesToRadians(Pitch)));
        front.Z = (float)(Math.Cos(MathHelper.DegreesToRadians(Pitch)) * Math.Sin(MathHelper.DegreesToRadians(Yaw)));
        Front = Vector3.Normalize(front);

        Right = Vector3.Normalize(Vector3.Cross(Front, WorldUp));
        Up    = Vector3.Normalize(Vector3.Cross(Right, Front));
    }
}
