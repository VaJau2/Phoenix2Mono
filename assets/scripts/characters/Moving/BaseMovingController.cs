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
    }

    public override void _Process(float delta)
    {
        if (character.Health <= 0) return;
        
        character.HandleImpulse();
        
        if (character.Velocity.Length() > 0) 
        {
            character.MoveAndSlide(character.Velocity, new Vector3(0, 1, 0), true);
        }
    }

    public void MoveTo(Vector3 place, float distance, float speed = 1)
    {
        var pos = character.GlobalTransform.origin;
        place.y = pos.y;
        
        var tempDistance = pos.DistanceTo(place);
        CloseToPoint = tempDistance <= distance;

        if (CloseToPoint) return;

        RotateTo(place);

        character.Rotation = new Vector3(0, character.Rotation.y, 0);
        character.Velocity = new Vector3(0, -Gravity, -speed).Rotated(Vector3.Up, character.Rotation.y);
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
        var rotA = character.Transform3D.basis.Quaternion().Normalized();
        var rotB = character.Transform3D.LookingAt(place, Vector3.Up).basis.Quaternion().Normalized();
        var tempRotation = rotA.Slerp(rotB, RotationSpeed);

        var oldScale = character.Scale;
        Transform3D tempTransform = character.Transform3D;
        tempTransform.basis = new Basis(tempRotation);
        character.Transform3D = tempTransform;
        character.Scale = oldScale;
    }
    
    public void UpdateHeight(float speed, float newHeight)
    {
        if (character.Position.y > newHeight + 2f)
        {
            Gravity = speed;
        } 
        else if (character.Position.y < newHeight - 2f)
        {
            Gravity = -speed;
        }
        else
        {
            Gravity = 0;
        }
    }
}
