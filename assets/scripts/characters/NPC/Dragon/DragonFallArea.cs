using Godot;

namespace Phoenix2Mono.assets.scripts.characters.NPC.Dragon
{
    public class DragonFallArea : Area
    {
        public void _on_fallarea_body_entered(Node body)
        {
            var dragon = GetParent<Dragon>();
            if (dragon.Health <= 0 && dragon.startFalling)
            {
                dragon.isFalling = false;
            }
        }
    }
}
