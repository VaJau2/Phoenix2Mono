using Godot;

//Части коллизии дракона повторяют положение костей дракона во время анимации
//голова, тело, крылья и т.д.
public partial class DragonCollision : CollisionShape3D
{
    [Export] private NodePath myPosPath;
    private Node3D myPos;
    private Character myParent;

    public override void _Ready()
    {
        myPos = GetNode<Node3D>(myPosPath);
        myParent = GetParent<Character>();
    }

    public override void _Process(double delta)
    {
        if (myParent.Health <= 0) return;

        var newTransform = GlobalTransform;
        newTransform.Origin = myPos.GlobalTransform.Origin;
        newTransform.Basis = myPos.GlobalTransform.Basis;
        GlobalTransform = newTransform;
    }
}
