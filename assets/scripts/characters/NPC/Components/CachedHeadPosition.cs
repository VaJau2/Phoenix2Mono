using Godot;

// Сохраняет позицию головы "статичной" на небольшом расстоянии
// Чтобы камера во время диалога не шаталась вслед за головой
public class CachedHeadPosition : BoneAttachment
{
    private const float MIN_CHANGE_DISTANCE = 3;
    
    private Vector3 tempHeadPosition = Vector3.Zero;
    
    public Vector3 GetPosition()
    {
        if (NeedToUpdatePosition())
        {
            tempHeadPosition = GlobalTranslation;
        }
        
        return tempHeadPosition;
    }

    private bool NeedToUpdatePosition()
    {
        if (tempHeadPosition == Vector3.Zero)
        {
            return true;
        }

        return tempHeadPosition.DistanceTo(GlobalTranslation) > MIN_CHANGE_DISTANCE;
    }
}
