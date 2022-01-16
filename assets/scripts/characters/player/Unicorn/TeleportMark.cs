using Godot;

//Скрипт обновляет позицию точки телепорта
//Перемещает её спрайт вниз, если она оказывается над потолком
//Проверяет через рейкаст
public class TeleportMark: Spatial
{
    private RayCast rayUp;

    private Sprite3D sprite;
    private Vector3 spritePos;
    private Vector3 offset;

    public override void _Ready()
    {
        var anim = GetNode<AnimationPlayer>("anim");
        anim.Play("idle");
        sprite  = GetNode<Sprite3D>("Sprite3D");
        rayUp   = GetNode<RayCast>("ray-up");
        spritePos = sprite.Transform.origin;
    }
    
    private void UpdateOffset(Vector3 place)
    {
        offset = Vector3.Zero;
        
        if (!rayUp.IsColliding()) return;
        var collidePos = rayUp.GetCollisionPoint();
        var dist = Mathf.Abs(place.y - collidePos.y);
        offset.y -= rayUp.CastTo.y - dist;
    }

    public void UpdatePosition(Vector3 place)
    {
        UpdateOffset(place);
        Vector3 newSpritePos = spritePos + offset;
        sprite.Transform = Global.setNewOrigin(sprite.Transform, newSpritePos);
        GlobalTransform = Global.setNewOrigin(GlobalTransform, place);
    }

    public void UpdateSprite(bool manaIsEnough)
    {
        sprite.Modulate = manaIsEnough ? Colors.White : Colors.Red;
    }

    public Vector3 GetTeportPoint()
    {
        return GlobalTransform.origin + offset;
    }
}
