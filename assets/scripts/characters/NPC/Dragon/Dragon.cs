using Godot;
using Godot.Collections;

public partial class Dragon: NPC
{
    private const float ATTACK_COOLDOWN = 1;
    public const int MOUTH_DAMAGE = 40;
    private const int FIRE_DAMAGE = 30;
    private const float FIRE_DISTANCE = 50;
    private const int IDLE_FLY_HEIGHT = 40;
    private const float SMACH_ATTACK_CHANCE = 0.5f;

    public bool IsAttacking;
    public bool isFalling;
    public bool isFireClose;
    private bool isSmashAttack;
    private float attackTimer = 1f;
    private float damageTimer = 3f;
    private float fireTimer = 3.5f;
    private float fireWaitTimer = 1;
    private float idleSoundTimer = 5f;

    public AudioStreamPlayer3D GetAudi() => audi;
    
    [Export] private Array<AudioStreamWav> idleSounds;
    [Export] private AudioStreamWav fireSound;

    private Control healthBarObj;
    private ProgressBar healthBar;

    private bool onetimeAnim;
    private AnimationPlayer anim;

    private Node3D fireObj;
    private Array<GpuParticles3D> fireParts = new();
    private AnimationPlayer fireAnim;
    private AudioStreamPlayer3D audiFire;

    public Node3D mouthPos;
    public Character enemyInMouth;
    public float enemyMouthTimer;
    private bool onetimeDie;

    public override async void _Ready()
    {
        base._Ready();
        healthBarObj = GetNode<Control>("/root/Main/Scene/canvas/dragonHealth");
        healthBar = healthBarObj.GetNode<ProgressBar>("ProgressBar");
        anim = GetNode<AnimationPlayer>("anim");
        fireObj = GetNode<Node3D>("Armature/Skeleton3D/BoneAttachment3D/mouth/fire");
        fireParts.Add(fireObj.GetNode<GpuParticles3D>("Particles"));
        fireParts.Add(fireObj.GetNode<GpuParticles3D>("Particles2"));
        fireParts.Add(fireObj.GetNode<GpuParticles3D>("Particles3"));
        fireAnim = fireObj.GetNode<AnimationPlayer>("fireAnim");
        audiFire = GetNode<AudioStreamPlayer3D>("audi-fire");
        mouthPos = GetNode<Node3D>("Armature/Skeleton3D/BoneAttachment3D/mouth");
        GRAVITY = 0;
        ROTATION_SPEED = 0.05f;

        await ToSignal(GetTree(), "process_frame");
        Global.Get().player.FireWithWeapon += CheckPlayerShooting;
    }

    private void CheckPlayerShooting()
    {
        //игроку достаточно пострелять два раза, чтобы выбраться из пасти дракона
        if (enemyInMouth is Player)
        {
            enemyMouthTimer -= 1;
        }
    }

    private int GetDamage(int baseDamage)
    {
        float tempDamage = baseDamage;
        tempDamage *= Global.Get().Settings.npcDamage;
        return (int) tempDamage;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        base.TakeDamage(damager, damage, shapeID);

        healthBarObj.Visible = Health > 0;
        if (Health > 0)
        {
            healthBar.Value = Health;
        }
        else
        {
            isFalling = true;
            SetFireOn(false);
            LetMouthEnemyGo();
            GetNode<AudioStreamPlayer3D>("audi-wings").Stop();
            anim.Play("die1");
        }
    }

    private void PlayIdleSounds(float delta)
    {
        if (idleSoundTimer > 0)
        {
            idleSoundTimer -= delta;
        }
        else
        {
            PlayRandomSound(idleSounds);
            var rand = new RandomNumberGenerator();
            idleSoundTimer = rand.RandfRange(2, 10);
        }
    }

    public override void SetState(NPCState newState)
    {
        base.SetState(newState);
        if (state == NPCState.Attack && attackTimer < 1f)
        {
            attackTimer = 0;
        }
    }

    private void UpdateAttackTimer(float delta, bool seeEnemy)
    {
        if (attackTimer > 0)
        {
            attackTimer -= delta;
        }
        else
        {
            attackTimer = 1f;
            IsAttacking = seeEnemy;
            if (IsAttacking)
            {
                var rand = new RandomNumberGenerator();
                rand.Randomize();
                isSmashAttack = rand.Randf() < SMACH_ATTACK_CHANCE;
                damageTimer = isSmashAttack ? 0.5f : 3;
            }
            else
            {
                if (fireTimer > 0)
                {
                    SetFireOn(false);
                }
            }
        }
    }

    public void SetAttackCooldown()
    {
        attackTimer = ATTACK_COOLDOWN;
    }

    private async void UpdateFlyAnims()
    {
        if (IsInstanceValid(enemyInMouth))
        {
            if (anim.CurrentAnimation != "fly-open-mouth")
            {
                anim.Play("fly-open-mouth");
            }
            
            return;
        }
        
        onetimeAnim = true;
        var rotY1 = Rotation.Y;
        await Global.Get().ToTimer(0.1f, this);
        var rotY2 = Rotation.Y;
        if (rotY2 < rotY1)
        {
            if (anim.CurrentAnimation != "fly-right")
            {
                anim.Play("fly-right");
            }
        }
        else if (rotY2 > rotY1)
        {
            if (anim.CurrentAnimation != "fly-left")
            {
                anim.Play("fly-left");
            }
        }
        else
        {
            if (anim.CurrentAnimation != "fly")
            {
                anim.Play("fly");
            }
        }

        onetimeAnim = false;
    }

    private void UpdateHeight(float speed, float newHeight)
    {
        if (Position.Y > newHeight + 2f)
        {
            GRAVITY = speed;
        } 
        else if (Position.Y < newHeight - 2f)
        {
            GRAVITY = -speed;
        }
        else
        {
            GRAVITY = 0;
        }
    }

    private void UpdatePatrolPoints()
    {
        if (!CloseToPoint)
        {
            MoveTo(patrolPoints[patrolI].GlobalTransform.Origin, 20);
        }
        else
        {
            if (patrolI < patrolPoints.Length - 1)
            {
                patrolI += 1; 
            }
            else
            {
                patrolI = 0;
            }

            CloseToPoint = false;
        }
    }

    private float GetEnemyDistance()
    {
        if (IsInstanceValid(tempVictim))
        {
            return GlobalTransform.Origin.DistanceTo(tempVictim.GlobalTransform.Origin);
        }

        return -1;
    }

    private void SetFireOn(bool on)
    {
        foreach (GpuParticles3D fire in fireParts)
        {
            fire.Emitting = on;
        }

        if (on)
        {
            audiFire.Stream = fireSound;
            audiFire.Play();
            fireAnim.Play("fire");
        }
        else
        {
            audiFire.Stop();
            fireAnim.CurrentAnimation = null;
            fireObj.GetNode<Sprite3D>("sprite").Frame = 0;
            fireObj.GetNode<Sprite3D>("sprite2").Frame = 0;
            fireObj.GetNode<Sprite3D>("sprite3").Frame = 0;
        }
    }

    private void Stop()
    {
        Velocity = Vector3.Zero;
        anim.Play("fly-on-place");
    }

    private void LookAtEnemy()
    {
        if (tempVictim == null || !IsInstanceValid(tempVictim))  return;

        var pos = tempVictim.GlobalTransform.Origin;
        pos.Y = GlobalTransform.Origin.Y;
        LookAt(pos, Vector3.Up);
    }

    private void UpdateEnemyInMouth(float delta)
    {
        enemyInMouth.GlobalTransform = Global.SetNewOrigin(
            enemyInMouth.GlobalTransform, 
            mouthPos.GlobalTransform.Origin
        );
        enemyInMouth.RotationDegrees = new Vector3(0, RotationDegrees.Y + 90, -90);
        if (enemyInMouth is Player player)
        {
            player.Body.bodyRot = 0;
        }

        if (damageTimer > 0)
        {
            damageTimer -= delta;
        }
        else
        {
            damageTimer = 0.5f;
            enemyInMouth.TakeDamage(this, GetDamage(MOUTH_DAMAGE));
        }

        if (enemyInMouth.Health <= 0)
        {
            enemyMouthTimer = 0;
        }
    }

    private void LetMouthEnemyGo()
    {
        if (enemyInMouth == null) return;
        if (!IsInstanceValid(enemyInMouth))
        {
            enemyInMouth = null;
            return;
        }
        enemyInMouth.MayMove = true;
        enemyInMouth.CollisionLayer = 1;
        enemyInMouth.CollisionMask = 1;
        enemyInMouth.RotationDegrees = new Vector3(0, enemyInMouth.RotationDegrees.Y, 0);
        enemyInMouth.Impulse = -GlobalTransform.Basis.Z * 3 + new Vector3(0, -2, 0);
        enemyInMouth = null;
        
        IsAttacking = false;
        SetAttackCooldown();
    }

    public override void _Process(double delta)
    {
        if (Velocity.Length() > 0) {
            MoveAndSlide();
        }
    
        if (Health <= 0) {
            if (isFalling)
            {
                Velocity = new Vector3(
                    Velocity.X / 2,
                    Velocity.Y - 10,
                    Velocity.Z / 2
                );
            }
            else
            {
                Velocity = Vector3.Zero;
                if (onetimeDie) return;
            
                anim.Play("die2");
                onetimeDie = true;
            }

            return;
        }

        float enemyDistance = GetEnemyDistance();

        UpdateAttackTimer((float)delta, enemyDistance > 0);
        if (Velocity.Length() > 0 && !onetimeAnim)
        {
            UpdateFlyAnims();
        }

        if (IsInstanceValid(enemyInMouth))
        {
            if (enemyMouthTimer > 0)
            {
                UpdateEnemyInMouth((float)delta);
                IsAttacking = false;
            }
            else
            {
                LetMouthEnemyGo();
            }
        }

        if (isFireClose)
        {
            if (enemyDistance > FIRE_DISTANCE)
            {
                isFireClose = false;
                CloseToPoint = false;
                SetFireOn(false);
            }
            else
            {
                if (fireWaitTimer > 0)
                {
                    Stop();
                    LookAtEnemy();
                    fireWaitTimer -= (float)delta;
                }
                else
                {
                    if (!audiFire.Playing)
                    {
                        anim.Play("attack");
                        SetFireOn(true);
                    }

                    if (fireTimer > 0)
                    {
                        fireTimer -= (float)delta;
                        LookAtEnemy();

                        if (!(enemyDistance > 0)) return;
                        
                        if (damageTimer > 0)
                        {
                            damageTimer -= (float)delta;
                        }
                        else
                        {
                            tempVictim.TakeDamage(this, GetDamage(FIRE_DAMAGE));
                            damageTimer = 0.1f;
                        }
                    }
                    else
                    {
                        SetFireOn(false);
                        fireWaitTimer = 1;
                        fireTimer = 3.5f;
                        IsAttacking = false;
                        isFireClose = false;
                        SetAttackCooldown();
                    }
                }
            }
        }
        else //fireClose == false
        {
            if (!IsAttacking || !IsInstanceValid(tempVictim))
            {
                PlayIdleSounds((float)delta);
                UpdatePatrolPoints();
                UpdateHeight(BaseSpeed / 4f, IDLE_FLY_HEIGHT);
            }
            else
            {
                int newHeight = isSmashAttack ? 8 : 16;
                if (enemyDistance is > 0 and < 60)
                {
                    UpdateHeight(BaseSpeed, tempVictim.Position.Y + newHeight);
                }
                else
                {
                    UpdateHeight(BaseSpeed / 4f, IDLE_FLY_HEIGHT);
                }

                if (!CloseToPoint)
                {
                    int distance = isSmashAttack ? 4 : 20;
                    MoveTo(tempVictim.GlobalTransform.Origin, distance);
                }
                else
                {
                    if (isSmashAttack)
                    {
                        SetState(NPCState.Idle);
                        SetAttackCooldown();
                    }
                    else
                    {
                        isFireClose = true;
                    }
                }
            }
        }
    }
}
