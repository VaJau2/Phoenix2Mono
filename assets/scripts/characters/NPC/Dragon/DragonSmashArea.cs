using Godot;

public class DragonSmashArea : Area
{
    private Dragon dragon;
    [Export] private AudioStreamSample eatSound;

    public override void _Ready()
    {
        dragon = GetParent<Dragon>();
    }

    public void _on_smasharea_body_entered(Node body)
    {
        if (!dragon.IsAttacking || dragon.Health <= 0) return;
    
        if (body is Character character)
        {
            character.MayMove = false;
            if (character is Player_Pegasus pegasus)
            {
                pegasus.IsFlying = pegasus.IsFlyingFast = false;
            }

            character.CollisionLayer = 0;
            character.CollisionMask = 0;
            character.TakeDamage(dragon, Dragon.MOUTH_DAMAGE);
            character.GlobalTransform = Global.SetNewOrigin(
                character.GlobalTransform, 
                dragon.mouthPos.GlobalTransform.origin
            );

            dragon.enemyMouthTimer = 3;
            dragon.enemyInMouth = character;
            dragon.isFireClose = false;
            dragon.GetAudi().Stream = eatSound;
            dragon.GetAudi().Play();
        }

        dragon.IsAttacking = false;
        dragon.SetAttackCooldown();
    }
}
