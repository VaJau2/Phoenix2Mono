using Godot;

namespace Phoenix2Mono.assets.scripts.furniture.StableDoors
{
    //Закрывающаяся бункерная дверь убивает персонажей, которые под ней стоят
    public class KillArea : Area
    {
        private const int DOOR_DAMAGE = 50;
        private FurnDoor myDoor;
        private Character bodyHere;
        private float timer = 0.1f;
    
        public override void _Ready()
        {
            myDoor = GetNode<FurnDoor>("../../../");
        }

        public override void _Process(float delta)
        {
            if (bodyHere == null || !IsInstanceValid(bodyHere)) return;
            if (timer > 0)
            {
                timer -= delta;
            }
            else
            {
                bodyHere.TakeDamage(bodyHere, DOOR_DAMAGE);
                timer = 0.1f;
            }
        }

        public void _on_killArea_body_entered(Node body)
        {
            if (myDoor.IsOpen || !myDoor.opening) return;
            if (body is Character character)
            {
                bodyHere = character;
            }
        }

        public void _on_killArea_body_exited(Node body)
        {
            if (bodyHere == body)
            {
                bodyHere = null;
            }
        }
    }
}
