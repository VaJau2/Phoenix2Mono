using Godot;

public partial class Player_Earthpony : Player
{
    const float DASH_COOLDOWN = 0.02f;
    const float DASH_BLOCK_TIMER = 1;

    public bool IsRunning = false;

    private double dashCooldown = 0;
    private double dashBlockTimer;

    public override void _Ready()
    {
        base._Ready();
        BaseRecoil = 0;
        LegsDamage = 110;
        SetStartHealth(200);
    }

    public override void UpdateGoForward() 
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

    public override void UpdateStand()
    {
        IsRunning = false;
    }

    public override int GetVelocity()
    {
        if (IsRunning) {
            return BaseSpeed * 2;
        } 
        return base.GetVelocity();
    }

    private void DashBlock() 
    {
        dashBlockTimer = DASH_BLOCK_TIMER;
    }

    private bool IsDashKeyPressed => (Velocity.Length() > 0.5f && Input.IsActionJustPressed("dash") && dashCooldown <= 0);

    public override void Crouch()
    {
        bool dash = IsDashKeyPressed;

        if (!Input.IsActionJustReleased("crouch") && !dash) return;
        if (!(crouchCooldown <= 0) && IsCrouching) return;
        Sit(!IsCrouching);

        if (!IsCrouching || !dash) return;
        soundSteps.SoundDash();
        Velocity = Velocity.Normalized();
        Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
        Velocity *= 120f;
        dashCooldown = DASH_COOLDOWN;
        DashBlock();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (dashCooldown > 0) 
        {
            dashCooldown -= delta;
        }

        if (dashBlockTimer > 0)
        {
            dashBlockTimer -= delta;
        }
    }
}
