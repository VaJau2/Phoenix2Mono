using Godot;

namespace Phoenix2Mono.assets.scripts.furniture.StableDoors
{
    public class DoorTimer : Node
    {
        private FurnDoor myDoor;
        private float timer = 10;
        
        public override void _Ready()
        {
            myDoor = GetParent() as FurnDoor;
        }

        public override void _Process(float delta)
        {
            if (myDoor == null) return;

            if (myDoor.IsOpen)
            {
                if (timer > 0)
                {
                    timer -= delta;
                }
                else
                {
                    myDoor.ClickFurn();
                }
            }
            else
            {
                timer = 10;
            }
        }
    }
}
