using Godot;

public class Player_Earthpony : Player
{
    const float DASH_COOLDOWN = 0.02f;
    const float DASH_BLOCK_TIMER = 1;

    public bool IsRunning = false;

    private float dashCooldown = 0;
    private float dashBlockTimer;

    public override void _Ready()
    {
        base._Ready();
        BaseRecoil = 0;
        LegsDamage = 110;
        SetStartHealth(200);
    }

    protected override void UpdateGoForward() 
    {
        if (!IsCrouching && Input.IsActionPressed("ui_shift")) 
        {
            IsRunning = true;
        }
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (dashBlockTimer > 0)
        {
            damage /= 2;
        }
        base.TakeDamage(damager, damage, shapeID);
    }

    protected override void UpdateStand()
    {
        IsRunning = false;
    }

    public override float GetSpeed()
    {
        if (IsRunning) return BaseSpeed * 3;
        return base.GetSpeed();
    }

    private void DashBlock() 
    {
        dashBlockTimer = DASH_BLOCK_TIMER;
    }

    private bool IsDashKeyPressed => (Velocity.Length() > 0.5f && Input.IsActionJustPressed("dash") && dashCooldown <= 0);

    protected override void Crouch()
    {
        bool dash = IsDashKeyPressed;

        if (!Input.IsActionJustReleased("crouch") && !dash) return;
        if (!(crouchCooldown <= 0) && IsCrouching) return;
        Sit(!IsCrouching);

        if (!IsCrouching || !dash) return;
        soundSteps.SoundDash();
        Velocity = Velocity.Normalized();
        Velocity.y = 0;
        Velocity *= 120f;
        dashCooldown = DASH_COOLDOWN;
        DashBlock();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        if (dashCooldown > 0) {
            dashCooldown -= delta;
        }

        if (dashBlockTimer > 0)
        {
            dashBlockTimer -= delta;
        }
    }
}
