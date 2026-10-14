using Godot;

// Сохраняет позицию головы "статичной" на небольшом расстоянии
// Чтобы камера во время диалога не шаталась вслед за головой
public partial class CachedHeadPosition : BoneAttachment3D
{
    [Export] private NodePath npcPath;
    
    private const float MIN_CHANGE_DISTANCE = 3;
    
    private Vector3 tempHeadPosition = Vector3.Zero;

    public override void _Ready()
    {
        var npc = GetNode<NPC>(npcPath);
        npc.HeadPosition = this;
    }
    
    public Vector3 GetPosition()
    {
        if (NeedToUpdatePosition())
        {
            tempHeadPosition = GlobalPosition;
        }
        
        return tempHeadPosition;
    }

    private bool NeedToUpdatePosition()
    {
        if (tempHeadPosition == Vector3.Zero)
        {
            return true;
        }

        return tempHeadPosition.DistanceTo(GlobalPosition) > MIN_CHANGE_DISTANCE;
    }
}
