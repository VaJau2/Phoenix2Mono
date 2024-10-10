using Godot;

public class BaseMovingController : Node
{
    public bool CloseToPoint;
    
    [Export] public float RagdollImpulse = 500;
    [Export] protected float Gravity;
    [Export] private float RotationSpeed = 0.15f;
    
    protected NPC Npc => GetParent<NPC>();
    
    public override void _Process(float delta)
    {
        if (Npc.Health <= 0) return;
        
        Npc.HandleImpulse();
        
        if (Npc.Velocity.Length() > 0) 
        {
            Npc.MoveAndSlide(Npc.Velocity, new Vector3(0, 1, 0), true);
        }
    }

    public void MoveTo(Vector3 place, float distance, float speed = 1)
    {
        var pos = Npc.GlobalTransform.origin;
        place.y = pos.y;

        RotateTo(place);

        speed += Npc.BaseSpeed;

        Npc.Rotation = new Vector3(0, Npc.Rotation.y, 0);
        Npc.Velocity = new Vector3(0, -Gravity, -speed).Rotated(Vector3.Up, Npc.Rotation.y);

        var tempDistance = pos.DistanceTo(place);
        CloseToPoint = tempDistance <= distance;
    }
    
    public virtual void Stop(bool moveDown = false)
    {
        if (Npc.MayMove && moveDown)
        {
            Npc.Velocity = new Vector3(0, -Gravity, 0);
        }
        else
        {
            Npc.Velocity = Vector3.Zero;
        }
    }
    
    private void RotateTo(Vector3 place)
    {
        var rotA = Npc.Transform.basis.Quat().Normalized();
        var rotB = Npc.Transform.LookingAt(place, Vector3.Up).basis.Quat().Normalized();
        var tempRotation = rotA.Slerp(rotB, RotationSpeed);

        Transform tempTransform = Npc.Transform;
        tempTransform.basis = new Basis(tempRotation);
        Npc.Transform = tempTransform;
    }
    
    public void UpdateHeight(float speed, float newHeight)
    {
        if (Npc.Translation.y > newHeight + 2f)
        {
            Gravity = speed;
        } 
        else if (Npc.Translation.y < newHeight - 2f)
        {
            Gravity = -speed;
        }
        else
        {
            Gravity = 0;
        }
    }
}
