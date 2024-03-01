using Godot;

//Скрипт обновляет позицию точки телепорта
//Перемещает её спрайт вниз, если она оказывается над потолком
//Проверяет через рейкаст

//Также проверяет области коллизии вокруг себя
//Чтобы игрок не телепортировался в узкие "междуздания"
public partial class TeleportMark: Node3D
{
    public bool MayTeleport { get; private set; } = true;
    private int _collidingObjectsCount;
    
    private RayCast3D _rayUp;
    private Sprite3D _sprite;
    private Vector3 _spritePos;
    private Vector3 _offset;

    public override void _Ready()
    {
        var anim = GetNode<AnimationPlayer>("anim");
        anim.Play("idle");
        _sprite  = GetNode<Sprite3D>("Sprite3D");
        _rayUp   = GetNode<RayCast3D>("ray-up");
        _spritePos = _sprite.Transform.Origin;
    }
    
    private void UpdateOffset(Vector3 place)
    {
        _offset = Vector3.Zero;
        
        if (!_rayUp.IsColliding()) return;
        var collidePos = _rayUp.GetCollisionPoint();
        var dist = Mathf.Abs(place.X - collidePos.X);
        _offset.X -= _rayUp.TargetPosition.X - dist;
    }

    public void UpdatePosition(Vector3 place)
    {
        UpdateOffset(place);
        Vector3 newSpritePos = _spritePos + _offset;
        _sprite.Transform = Global.SetNewOrigin(_sprite.Transform, newSpritePos);
        GlobalTransform = Global.SetNewOrigin(GlobalTransform, place);
    }

    public void UpdateSprite(bool manaIsEnough)
    {
        var mayTeleport = manaIsEnough && MayTeleport;
        _sprite.Modulate = mayTeleport ? Colors.White : Colors.Red;
    }

    public Vector3 GetTeleportPoint()
    {
        return GlobalTransform.Origin + _offset;
    }

    public void _on_checkArea_body_entered(Node body)
    {
        _collidingObjectsCount++;
        MayTeleport = false;
    }

    public void _on_checkArea_body_exited(Node body)
    {
        _collidingObjectsCount--;
        
        if (_collidingObjectsCount <= 0)
        {
            MayTeleport = true;
        }
    }
}
