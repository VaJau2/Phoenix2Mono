using Godot;

public partial class DragonMouth : Area3D
{
    private const float MOUTH_COOLDOWN = 1f;
    
    public Node3D mouthPos;
    public bool HasEnemy => enemyInMouth != null && IsInstanceValid(enemyInMouth);

    private NPC npc;
    private NpcAudio audi;
    private DragonBody body;
    private Character enemyInMouth;
    private float enemyMouthTimer;
    private float mouthCooldown;
    private float damageTimer = 3f;
    
    [Export] private AudioStreamWav eatSound;

    public override async void _Ready()
    {
        npc = GetParent<NPC>();
        body = npc.GetNode<DragonBody>("body");
        audi = npc.GetNode<NpcAudio>("audi");
        mouthPos = npc.GetNode<Node3D>("Armature/Skeleton/BoneAttachment/mouth");
        
        npc.Connect(Character.SignalName.TakenDamage, new Callable(this, nameof(OnTakeDamage)));
        
        await npc.ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Global.Get().player.Connect(Player.SignalName.FireWithWeapon, new Callable(this, nameof(CheckPlayerShooting)));
    }
    
    private void CheckPlayerShooting()
    {
        //игроку достаточно пострелять два раза, чтобы выбраться из пасти дракона
        if (enemyInMouth is Player)
        {
            enemyMouthTimer -= 1;
        }
    }

    public void OnTakeDamage()
    {
        LetMouthEnemyGo();
    }

    public void _on_smasharea_body_entered(Node body)
    {
        if (npc.GetState() != SetStateEnum.Attack || npc.Health <= 0) return;
        if (mouthCooldown > 0) return;
    
        if (body is Character character)
        {
            character.SetMayMove(false);
            if (character is Player_Pegasus pegasus)
            {
                pegasus.IsFlying = pegasus.IsFlyingFast = false;
            }

            character.CollisionLayer = 0;
            character.CollisionMask = 0;
            character.TakeDamage(npc, DragonBody.MOUTH_DAMAGE);
            character.GlobalTransform = Global.SetNewOrigin(
                character.GlobalTransform, 
                mouthPos.GlobalTransform.Origin
            );

            enemyMouthTimer = 3;
            enemyInMouth = character;
            audi.PlayStream(eatSound);
        }

        npc.SetState(SetStateEnum.Idle);
    }
    
    private void UpdateEnemyInMouth(float delta)
    {
        if (enemyInMouth.Health <= 0)
        {
            LetMouthEnemyGo();
            return;
        }
        
        enemyInMouth.GlobalTransform = Global.SetNewOrigin(
            enemyInMouth.GlobalTransform, 
            mouthPos.GlobalTransform.Origin
        );
        enemyInMouth.RotationDegrees = new Vector3(0, npc.RotationDegrees.Y + 90, -90);
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
            enemyInMouth.TakeDamage(npc, body.GetDamage(DragonBody.MOUTH_DAMAGE));
        }
    }

    private void LetMouthEnemyGo()
    {
        if (!HasEnemy)
        {
            enemyInMouth = null;
            return;
        }

        enemyMouthTimer = 0;
        mouthCooldown = MOUTH_COOLDOWN;
        npc.SetState(SetStateEnum.Idle);

        if (enemyInMouth is NPC npcVictim )
        {
            if (npcVictim.Weapons is { HasWeapon: true})
            {
                npcVictim.Weapons.SpawnPickableItem();
            }
            
            npcVictim.QueueFree();
            enemyInMouth = null;
            return;
        }
        
        enemyInMouth.SetMayMove(true);
        enemyInMouth.CollisionLayer = 1;
        enemyInMouth.CollisionMask = 1;
        enemyInMouth.RotationDegrees = new Vector3(0, enemyInMouth.RotationDegrees.Y, 0);
        enemyInMouth.impulse = -npc.GlobalTransform.Basis.Z * 3 + new Vector3(0, -2, 0);
        enemyInMouth = null;
    }

    public override void _Process(double delta)
    {
        if (npc.Health <= 0) return;

        if (mouthCooldown > 0)
        {
            mouthCooldown -= (float)delta;
        }
        
        if (!HasEnemy) return;
        
        if (enemyMouthTimer > 0)
        {
            UpdateEnemyInMouth((float)delta);
        }
        else
        {
            LetMouthEnemyGo();
        }
    }
}
