using Godot;

//Части коллизии дракона повторяют положение костей дракона во время анимации
//голова, тело, крылья и т.д.
public class DragonCollision : CollisionShape
{
    [Export] private NodePath myPosPath;
    private Spatial myPos;
    private Character myParent;

    public override void _Ready()
    {
        myPos = GetNode<Spatial>(myPosPath);
        myParent = GetParent<Character>();
    }

    public override void _Process(float delta)
    {
        if (myParent.Health <= 0) return;

        var newTransform = GlobalTransform;
        newTransform.origin = myPos.GlobalTransform.origin;
        newTransform.basis = myPos.GlobalTransform.basis;
        GlobalTransform = newTransform;
    }
}
