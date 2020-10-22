using Godot;

public class Player_Earthpony : Player
{
    const float DASH_TIMER = 1;

    public bool IsRunning = false;
    public bool IsDashing = false;
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

    public async void DashBlock() 
    {
        IsDashing = true;
        await global.ToTimer(DASH_TIMER);
        IsDashing = false;
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
                    Velocity.x *= 5;
                    Velocity.z *= 5;
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
                Velocity.y = JUMP_SPEED;
            }
        }
    }
}
