using Godot;

public partial class BaseMovingController : Node
{
    public bool CloseToPoint;
    [Export] protected float Gravity;
    [Export] protected float RotationSpeed = 0.3f;

    protected Character character;

    public override void _Ready()
    {
        character = GetParent<Character>();

        //в Godot 4 упор в стену почти в лоб (<15°) гасит движение, в Godot 3 тело скользило — NPC шагали на месте (B-53)
        if (character is NPC)
        {
            character.WallMinSlideAngle = 0;
        }
    }

    public override void _Process(double delta)
    {
        if (character.Health <= 0) return;
        
        character.HandleImpulse();
        
        if (character.Velocity.Length() > 0)
        {
            character.MoveAndSlideVelocity(true);
            StepUpLowObstacle();
            (character as NPC)?.OpenCollidedDoor();
        }
    }

    private const float STEP_HEIGHT = 0.3f;

    //подъём на низкий порожек: в Jolt коробка-коллизия NPC упирается даже в сантиметровую ступеньку,
    //в Godot 3 её выталкивало вверх (B-27)
    private void StepUpLowObstacle()
    {
        var horizontal = new Vector3(character.Velocity.X, 0, character.Velocity.Z);
        if (horizontal.LengthSquared() < 0.01f || !character.IsOnFloor()) return;

        for (int i = 0; i < character.GetSlideCollisionCount(); i++)
        {
            var collision = character.GetSlideCollision(i);
            var normal = collision.GetNormal();
            if (Mathf.Abs(normal.Y) > 0.3f || normal.Dot(horizontal) >= 0) continue;   //не стена впереди
            if (collision.GetCollider() is Character) continue;
            float height = collision.GetPosition().Y - character.GlobalPosition.Y;
            if (height <= 0.01f || height > STEP_HEIGHT) continue;                  //не низкое препятствие

            //сверху и над препятствием должно быть свободно
            var up = new Vector3(0, STEP_HEIGHT, 0);
            if (character.TestMove(character.GlobalTransform, up)) return;
            if (character.TestMove(character.GlobalTransform.Translated(up), horizontal.Normalized() * 0.3f)) return;

            character.GlobalPosition += up;
            return;
        }
    }

    public void MoveTo(Vector3 place, float distance, float speed = 1)
    {
        var pos = character.GlobalTransform.Origin;
        place.Y = pos.Y;
        
        var tempDistance = pos.DistanceTo(place);
        CloseToPoint = tempDistance <= distance;

        if (CloseToPoint) return;

        RotateTo(place);

        character.Rotation = new Vector3(0, character.Rotation.Y, 0);
        character.Velocity = new Vector3(0, -Gravity, -speed).Rotated(Vector3.Up, character.Rotation.Y);
    }
    
    public virtual void Stop(bool moveDown = false)
    {
        if (character.MayMove && moveDown)
        {
            character.Velocity = new Vector3(0, -Gravity, 0);
        }
        else
        {
            character.Velocity = Vector3.Zero;
        }
    }
    
    private void RotateTo(Vector3 place)
    {
        var rotA = character.Transform.Basis.GetRotationQuaternion().Normalized();
        var rotB = character.Transform.LookingAt(place, Vector3.Up).Basis.GetRotationQuaternion().Normalized();
        //RotationSpeed — доля поворота за кадр при 60 FPS; пересчёт по delta, чтобы на 144 Гц не поворачивать резче
        var weight = RotationSpeed >= 1f
            ? 1f
            : 1f - Mathf.Pow(1f - RotationSpeed, (float)GetProcessDeltaTime() * 60f);
        var tempRotation = rotA.Slerp(rotB, weight);

        var oldScale = character.Scale;
        Transform3D tempTransform = character.Transform;
        tempTransform.Basis = new Basis(tempRotation);
        character.Transform = tempTransform;
        character.Scale = oldScale;
    }
    
    public void UpdateHeight(float speed, float newHeight)
    {
        if (character.Position.Y > newHeight + 2f)
        {
            Gravity = speed;
        } 
        else if (character.Position.Y < newHeight - 2f)
        {
            Gravity = -speed;
        }
        else
        {
            Gravity = 0;
        }
    }
}
