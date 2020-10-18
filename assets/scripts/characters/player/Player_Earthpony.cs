using Godot;

public class Player_Earthpony : Player
{
    public bool IsRunning = false;
    private float RunSpeed = 30f;

    public override void UpdateGoForward() 
    {
        if (!IsCrouching && Input.IsActionPressed("ui_shift")) {
            IsRunning = true;
        }
    }

    public override void UpdateStand()
    {
        IsRunning = false;
    }

    public override float GetWalkSpeed(float delta)
    {
        if (IsRunning) {
            return RunSpeed;
        } else {
            return MaxSpeed;
        }
    }

    public void DashBlock() 
    {
        GD.Print("dash blocking...");
    }

    public override void Crouch()
    {
        bool dash = false;
        if (Input.IsActionJustPressed("dash")) {
            dash = true;
        }

        if (Input.IsActionJustReleased("crouch") || dash) {
            if (crouchCooldown <= 0 || !IsCrouching) {
                Sit(!IsCrouching);

                if (IsCrouching && dash) {
                    vel.x *= 5;
                    vel.z *= 5;
                    DashBlock();
                }
            }
        }
    }

    public override void Jump()
    {
        if (Input.IsActionJustPressed("jump") && !BlockJump) {
            if (IsCrouching) {
                Sit(false);
            } else {
                OnStairs = false;
                vel.y = JUMP_SPEED;
            }
        }
    }
}
