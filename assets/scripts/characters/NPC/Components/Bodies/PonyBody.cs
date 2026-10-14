using Godot;
using Godot.Collections;

public partial class PonyBody : Node, ISavable
{
    [Export] private Array<AudioStreamWav> hittedSounds = [];
    [Export] private AudioStreamWav dieSound;
    
    public string IdleAnim = "Idle1";
    public string CustomIdleAnim = null;
    
    private NPC npc;
    private NpcAudio audi;
    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback playback;
    private Vector2 headBlend;

    private Node3D defaultLookTarget;
    private Node3D lookTarget;
    private float lookTimer = 2f;
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        audi = npc.GetNode<NpcAudio>("audi");
        animTree = npc.GetNode<AnimationTree>("animTree");
        playback = (AnimationNodeStateMachinePlayback) animTree.Get("parameters/StateMachine/playback");
        headBlend = (Vector2) animTree.Get("parameters/BlendSpace2D/blend_position");
        
        playback.Start(IdleAnim);
        
        npc.Connect(Character.SignalName.TakenDamage, new Callable(this, nameof(OnNpcHitted)));
        npc.Connect(Character.SignalName.DieEvent, new Callable(this, nameof(OnNpcDying)));
    }

    public override void _Process(double delta)
    {
        if (npc.BaseSpeed == 0)
        {
            return;
        }

        UpdateWalkingAnimations();
        UpdateHeadRotation((float)delta);
        animTree.Set("parameters/BlendSpace2D/blend_position", headBlend);
    }

    public void OnNpcHitted()
    {
        audi.PlayRandomSound(hittedSounds);
    }

    public void OnNpcDying()
    {
        audi.PlayStream(dieSound);
        
        PlayAnim(Character.IDLE_ANIM1);
    }
    
    public Vector3 GetDirToTarget(Node3D target)
    {
        Vector3 targetDirPos = target.GlobalPosition;
        targetDirPos.Y = npc.GlobalPosition.Y;
        return targetDirPos - npc.GlobalPosition;
    }

    public void SetDefaultLookTarget(Node3D value)
    {
        defaultLookTarget = value;
    }

    public void SetLookTarget(Node3D value)
    {
        lookTarget = value;
    }
    
    public void LookAtTarget()
    {
        if (npc.tempVictim == null) return;
        
        var npcForward = -npc.GlobalTransform.Basis.Z;
        var npcDir = GetDirToTarget(npc.tempVictim);
        var rotationToVictim = npcForward.AngleTo(npcDir);

        if (npc.Weapons is { IsPistol: true })
        {
            if (Mathf.RadToDeg(rotationToVictim) < 80)
            {
                return;
            }
        }
        
        Vector3 victimPos = npc.tempVictim.GlobalPosition;
        victimPos.Y = npc.GlobalTransform.Origin.Y;

        if (npc.GlobalPosition != victimPos)
        {
            npc.LookAt(victimPos, Vector3.Up);
        }
    }

    private void PlayAnim(string animName)
    {
        if (playback.GetCurrentNode() == animName)
        {
            return;
        }
        
        if (playback.IsPlaying())
        {
            playback.Travel(animName);
        }
        else
        {
            playback.Start(animName);
        }
    }

    public void _on_lookArea_body_entered(Node body)
    {
        if (npc.GetState() != SetStateEnum.Idle || IsInstanceValid(lookTarget) || body == npc) return;
        if (body is not Character character) return;
        lookTimer = 2f;
        lookTarget = character;
    }

    public void _on_lookArea_body_exited(Node body)
    {
        if (npc.GetState() == SetStateEnum.Idle && body == lookTarget)
        {
            lookTarget = null;
        }
    }

    private void UpdateWalkingAnimations()
    {
        if (npc.Velocity.Length() <= 0)
        {
            PlayIdleAnim();
            return;
        }
        
        PlayAnim(
            npc.MovingController is NavigationMovingController { IsRunning: true }
                ? "Run" 
                : "Walk"
        );
    }
    
    private void PlayIdleAnim()
    {
        if (!string.IsNullOrEmpty(CustomIdleAnim))
        {
            PlayAnim(CustomIdleAnim);
        }
        else if (!string.IsNullOrEmpty(IdleAnim))
        {
            PlayAnim(IdleAnim);
        } 
        else
        {
            StopAnim();
        }
    }
    
    private void StopAnim()
    {
        playback.Stop();
    }

    private void UpdateHeadRotation(float delta)
    {
        if (IsInstanceValid(lookTarget) && MayRotateHead)
        {
            if (npc.GetState() == SetStateEnum.Idle)
            {
                if (lookTimer > 0)
                {
                    lookTimer -= delta;
                }
                else
                {
                    lookTarget = null;
                    return;
                }
            }
            
            var headRotation = GetHeadRotationTo(lookTarget);

            headBlend = headBlend.MoveToward(headRotation, delta * 2);
        }
        else
        {
            var defaultHeadRotation = Vector2.Zero;
            
            if (defaultLookTarget != null)
            {
                defaultHeadRotation = GetHeadRotationTo(defaultLookTarget);
            }
            
            headBlend = headBlend.MoveToward(defaultHeadRotation, delta * 2);
        }
    }

    private bool MayRotateHead => IdleAnim != "Frozen";

    private Vector2 GetHeadRotationTo(Node3D target)
    {
        Vector3 npcForward = -npc.GlobalTransform.Basis.Z;
        Vector3 dir = GetDirToTarget(target);

        float angle = npcForward.AngleTo(dir);
        if (npc.GlobalTransform.Basis.X.Dot(dir) < 0)
        {
            angle = -angle;
        }
        
        var targetY = target.GlobalPosition.Y;
        //точка центра игрока чуть выше, тк он умеет красться и приседать с:
        if (target is Player)
        {
            targetY += 0.25f * npc.lookHeightFactor;
        }

        float diffY = targetY - npc.GlobalPosition.Y;

        return new Vector2(angle / 1.5f, diffY / 5);
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["idleAnim"] = IdleAnim;
        saveData["customIdleAnim"] = CustomIdleAnim;
        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        IdleAnim = data["idleAnim"].ToString();
        CustomIdleAnim = data["customIdleAnim"].AsStringOrNull();
    }
}
