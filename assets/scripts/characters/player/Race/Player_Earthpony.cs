using Godot;

public class Player_Earthpony : Player
{
    const float DASH_COOLDOWN = 0.02f;
    const float DASH_BLOCK_TIMER = 1;

    public bool IsRunning = false;
    public bool IsDashing = false;

    private float dashCooldown = 0;

    public override void _Ready()
    {
        base._Ready();
        BaseRecoil = 0;
        LegsDamage = 110;
        SetStartHealth(200);
    }

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

    public override int GetSpeed()
    {
        if (IsRunning) {
            return BaseSpeed * 2;
        } 
        return base.GetSpeed();
    }

    public async void DashBlock() 
    {
        IsDashing = true;
        await global.ToTimer(DASH_BLOCK_TIMER);
        IsDashing = false;
    }

    public bool IsDashKeyPressed
    {
       get => (Velocity.Length() > 0.5f && Input.IsActionJustPressed("dash") && dashCooldown <= 0);
    }

    public override void Crouch()
    {
        bool dash = false;
        if (IsDashKeyPressed) {
            dash = true;
        }

        if (Input.IsActionJustReleased("crouch") || dash) {
            if (crouchCooldown <= 0 || !IsCrouching) {
                Sit(!IsCrouching);

                if (IsCrouching && dash) {
                    soundSteps.SoundDash();
                    Velocity = Velocity.Normalized();
                    Velocity *= 130f;
                    dashCooldown = DASH_COOLDOWN;
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

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        if (dashCooldown > 0) {
            dashCooldown -= delta;
        }
    }
}
