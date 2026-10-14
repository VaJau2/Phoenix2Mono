using Godot;

public class DragonMouth : Area
{
    private const float MOUTH_COOLDOWN = 1f;
    
    public Spatial mouthPos;
    public bool HasEnemy => enemyInMouth != null && IsInstanceValid(enemyInMouth);

    private NPC npc;
    private NpcAudio audi;
    private DragonBody body;
    private Character enemyInMouth;
    private float enemyMouthTimer;
    private float mouthCooldown;
    private float damageTimer = 3f;
    
    [Export] private AudioStreamSample eatSound;

    public override async void _Ready()
    {
        npc = GetParent<NPC>();
        body = npc.GetNode<DragonBody>("body");
        audi = npc.GetNode<NpcAudio>("audi");
        mouthPos = npc.GetNode<Spatial>("Armature/Skeleton/BoneAttachment/mouth");
        
        npc.Connect(nameof(Character.TakenDamage), this, nameof(OnTakeDamage));
        
        await npc.ToSignal(GetTree(), "idle_frame");
        Global.Get().player.Connect(nameof(Player.FireWithWeapon), this, nameof(CheckPlayerShooting));
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
                mouthPos.GlobalTransform.origin
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
            mouthPos.GlobalTransform.origin
        );
        enemyInMouth.RotationDegrees = new Vector3(0, npc.RotationDegrees.y + 90, -90);
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
        enemyInMouth.RotationDegrees = new Vector3(0, enemyInMouth.RotationDegrees.y, 0);
        enemyInMouth.impulse = -npc.GlobalTransform.basis.z * 3 + new Vector3(0, -2, 0);
        enemyInMouth = null;
    }

    public override void _Process(float delta)
    {
        if (npc.Health <= 0) return;

        if (mouthCooldown > 0)
        {
            mouthCooldown -= delta;
        }
        
        if (!HasEnemy) return;
        
        if (enemyMouthTimer > 0)
        {
            UpdateEnemyInMouth(delta);
        }
        else
        {
            LetMouthEnemyGo();
        }
    }
}
