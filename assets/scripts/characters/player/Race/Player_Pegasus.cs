using System;
using Godot;
using Godot.Collections;

public class Player_Pegasus : Player
{
    const float FLYING_FAST_SMASH_COOLDOWN = 0.5f;
    const float FLY_SPEED = 2.5f;
    
    public bool IsFlyingFast;
    public bool MaySmash;

    private bool isFlying;
    private float flyingFastTimer;
    private float speedY;
    private float flyIncrease = 8f;
    private float flyDecrease = 4;
    private float flySpeed = FLY_SPEED;

    public AudioStreamPlayer wingsAudi;
    private AudioStreamSample wingsSound;

    public bool IsFlying
    {
        get => isFlying;

        set
        {
            isFlying = value;
            
            if (isFlying)
            {
                wingsAudi.Stream = wingsSound;
                wingsAudi.Play();
            }
            else
            {
                wingsAudi.Stop();
            }
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        wingsAudi = GetNode<AudioStreamPlayer>("sound/audi_wings");
        wingsSound = GD.Load<AudioStreamSample>("res://assets/audio/flying/pegasus-wings.wav");
        SetStartHealth(125);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (Body.RotationDegrees.z != 0)
        {
            Vector3 newRot = Body.RotationDegrees;
            float decreaseSpeed = Mathf.Abs(newRot.z) * delta;

            if (IsFlyingFast)
            {
                if (newRot.z > 1)
                {
                    newRot.z -= decreaseSpeed;
                }
                else if (newRot.z < -1)
                {
                    newRot.z += decreaseSpeed;
                }
                else
                {
                    newRot.z = 0;
                }
            }
            else if (IsFlying)
            {
                newRot.z = Mathf.MoveToward(newRot.z, 0, 150f * delta);
            }
            else
            {
                newRot.z = 0;
            }

            Body.RotationDegrees = newRot;
        }

        if (IsFlyingFast)
        {
            if (flyingFastTimer < FLYING_FAST_SMASH_COOLDOWN)
            {
                flyingFastTimer += delta;
            }
            else
            {
                MaySmash = true;
            }
        }
        else
        {
            if (MaySmash)
            {
                flyingFastTimer = 0;
                MaySmash = false;
            }
        }
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        base.TakeDamage(damager, damage, shapeID);

        if (Health <= 0)
        {
            IsFlying = false;
        }
    }

    protected override void UpdateGoForward()
    {
        if (IsFlying)
        {
            IsFlyingFast = true;
        }
    }

    protected override void UpdateStand()
    {
        IsFlyingFast = false;
    }

    protected override void Jump()
    {
        //is_on_floor
        if (IsFlying) IsFlying = false;

        //on_jump
        base.Jump();
    }

    protected override void Fly()
    {
        if (!IsFlying && MayMove)
        {
            if (Input.IsActionJustPressed("jump") && !JumpHint.Visible)
            {
                OnStairs = false;
                IsFlying = true;
            }
        }
        else
        {
            if (Input.IsActionPressed("jump"))
            {
                speedY = 15f;
            }
            else if (Input.IsActionPressed("ui_shift"))
            {
                speedY = -18f;
            }
            else
            {
                if (IsFlyingFast)
                {
                    speedY = GetVerticalLook() / 5f;
                }
                else
                {
                    speedY = 0;
                }
            }
        }
    }

    public override void SitOnChair(bool sitOn)
    {
        IsFlying = false;
        base.SitOnChair(sitOn);
    }

    protected override float GetGravitySpeed(float tempShake, float delta)
    {
        if (IsFlying)
        {
            return MayMove ? speedY : 0;
        }
        else
        {
            return Velocity.y + (GRAVITY * delta + tempShake);
        }
    }


    public override float GetSpeed()
    {
        if (IsFlying)
        {
            if (IsFlyingFast)
            {
                flySpeed += flyIncrease * 0.02f;

                if (flyIncrease > 0)
                {
                    flyIncrease -= 0.25f;
                }
            }
            else
            {
                flySpeed = FLY_SPEED;
                flyIncrease = 5f;
            }

            return (int)(base.GetSpeed() * flySpeed);
        }

        return base.GetSpeed();
    }

    protected override float GetDeacceleration()
    {
        return IsFlying ? flyDecrease : DEACCCEL;
    }

    protected override void OnCameraRotatingX(float speedX)
    {
        if (IsFlyingFast)
        {
            if (speedX != 0)
            {
                Vector3 newRot = Body.RotationDegrees;
                newRot.z += speedX * -MouseSensivity * 0.5f;
                Body.RotationDegrees = newRot;
            }
        }
    }

    public override Dictionary GetSaveData()
    {
        Dictionary saveData = base.GetSaveData();
        saveData.Add("is_flying", IsFlying);
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        if (!data.Contains("is_flying")) return;

        IsFlying = Convert.ToBoolean(data["is_flying"]);
    }
}