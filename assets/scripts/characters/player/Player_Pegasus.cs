using Godot;

public class Player_Pegasus : Player
{
    const float FLYING_FAST_SMASH_COOLDOWN = 1f;
    public bool IsFlying = false;
    public bool IsFlyingFast = false;

    public bool MaySmash = false;
    private float flyingFastTimer = 0;
    private float speedY;
    public float flySpeed {get; private set;} = 35f;
    private float flyIncrease = 8f;
    private float flyDecrease = 4;

    public AudioStreamPlayer wingsAudi;
    private AudioStreamSample wingsSound;

    public override void _Ready()
    {
        base._Ready();
        wingsAudi = GetNode<AudioStreamPlayer>("sound/audi_wings");
        wingsSound = GD.Load<AudioStreamSample>("res://assets/audio/flying/pegasus-wings.wav");
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (Body.RotationDegrees.z != 0) {
            Vector3 newRot = Body.RotationDegrees;
            float decreaseSpeed = Mathf.Abs(newRot.z) * delta;

            if (IsFlyingFast) {
                if (newRot.z > 1) {
                    newRot.z -= decreaseSpeed;
                } else if(newRot.z < -1) {
                    newRot.z += decreaseSpeed;
                } else {
                    newRot.z = 0;
                }
            } else if(IsFlying) {
                newRot.z = Mathf.MoveToward(newRot.z, 0, 150f * delta);
            } else {
                newRot.z = 0;
            }
            Body.RotationDegrees = newRot;
        }

        if (IsFlyingFast) {
            if (flyingFastTimer < FLYING_FAST_SMASH_COOLDOWN) {
                flyingFastTimer += delta;
            } else {
                MaySmash = true;
            }
        } else {
            if (MaySmash) {
                flyingFastTimer = 0;
                MaySmash = false;
            }
        }
    }

    public override void UpdateGoForward()
    {
        if (IsFlying) {
            IsFlyingFast = true;
        }
    }

    public override void UpdateStand()
    {
        IsFlyingFast = false;
    }

    public override void Jump()
    {
        //is_on_floor
        if (IsFlying) {
            IsFlying = false;
            wingsAudi.Stop();
        }

        //on_jump
        if (Input.IsActionJustPressed("jump") && !BlockJump) {
            if (IsCrouching) {
                Sit(false);
            } else {
                OnStairs = false;
                Velocity.y = JUMP_SPEED;
            }
        }
    }

    public override void Fly()
    {
        if (!IsFlying && MayMove) {
            if (Input.IsActionJustPressed("jump") && !JumpHint.Visible) {
                OnStairs = false;
                IsFlying = true;
                wingsAudi.Stream = wingsSound;
                wingsAudi.Play();
            }
        } else {
            if (Input.IsActionPressed("jump")) {
                speedY = 15f;
            } else if (Input.IsActionPressed("ui_shift")) {
                speedY = -18f;
            } else {
                if (IsFlyingFast) {
                    speedY = GetVerticalLook() / 5f;
                } else {
                    speedY = 0;
                }
            }
        }
    }

    public override float GetGravitySpeed(float tempShake, float delta)
    {
        if (IsFlying) {
            return speedY;
        } else {
            return Velocity.y + (GRAVITY * delta + tempShake);
        }
    }

    public override float GetWalkSpeed(float delta)
    {
        if (IsFlying) {
            if (IsFlyingFast) {
                GD.Print(flySpeed);
                flySpeed += flyIncrease * delta;

                if (flyIncrease > 0) {
                    flyIncrease -= delta * 2f;
                } 
            } else {
                flySpeed = 35f;
                flyIncrease = 8f;
            }

            return flySpeed;
        }
        return MaxSpeed;
    }

    public override float GetDeacceleration()
    {
        if (IsFlying) {
            return flyDecrease;
        } else {
            return DEACCCEL;
        }
    }

    public override void OnCameraRotatingX(float speedX)
    {
        if (IsFlyingFast) {
            if (speedX != 0) {
                Vector3 newRot = Body.RotationDegrees;
                newRot.z += speedX * -MouseSensivity * 0.5f;
                Body.RotationDegrees = newRot;
            }
        }
    }
}