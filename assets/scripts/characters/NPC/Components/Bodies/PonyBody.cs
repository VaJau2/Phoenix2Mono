using Godot;
using Godot.Collections;

public class PonyBody : Node
{
    [Export] private Array<AudioStreamSample> hittedSounds;
    [Export] private AudioStreamSample dieSound;
    [Export] public string IdleAnim = "Idle1";
    
    private NPC npc;
    private NpcAudio audi;
    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback playback;
    private Vector2 headBlend;

    private Spatial defaultLookTarget;
    private Spatial lookTarget;
    private float lookTimer = 2f;
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        audi = npc.GetNode<NpcAudio>("audi");
        animTree = npc.GetNode<AnimationTree>("animTree");
        playback = (AnimationNodeStateMachinePlayback) animTree.Get("parameters/StateMachine/playback");
        headBlend = (Vector2) animTree.Get("parameters/BlendSpace2D/blend_position");
        
        if (!string.IsNullOrEmpty(IdleAnim))
        {
            playback.Start(IdleAnim);
        }

        PlayIdleAnim();
        npc.Connect(nameof(NPC.IsDying), this, nameof(OnNpcDying));
        npc.Connect(nameof(Character.TakenDamage), this, nameof(OnNpcHitted));
    }

    public override void _Process(float delta)
    {
        if (npc.MovingController.WalkSpeed == 0)
        {
            return;
        }

        UpdateWalkingAnimations();
        UpdateHeadRotation(delta);
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
    
    public Vector3 GetDirToTarget(Spatial target)
    {
        Vector3 targetDirPos = target.GlobalTranslation;
        targetDirPos.y = npc.GlobalTranslation.y;
        return targetDirPos - npc.GlobalTranslation;
    }

    public void SetDefaultLookTarget(Spatial value)
    {
        defaultLookTarget = value;
    }

    public void SetLookTarget(Spatial value)
    {
        lookTarget = value;
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
        if (npc.Velocity.Length() <= Character.MIN_WALKING_SPEED)
        {
            PlayIdleAnim();
            return;
        }
        
        PlayAnim(npc.MovingController.IsRunning ? "Run" : "Walk");
    }
    
    private void PlayIdleAnim()
    {
        if (!string.IsNullOrEmpty(IdleAnim))
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
        if (IsInstanceValid(lookTarget))
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

            SetValueTo(ref headBlend.x, headRotation.x, delta * 4);
            SetValueTo(ref headBlend.y, headRotation.y, delta * 4);
        }
        else
        {
            var defaultHeadRotation = Vector2.Zero;
            
            if (defaultLookTarget != null)
            {
                defaultHeadRotation = GetHeadRotationTo(defaultLookTarget);
            }
            
            SetValueTo(ref headBlend.x, defaultHeadRotation.x, delta * 2);
            SetValueTo(ref headBlend.y, defaultHeadRotation.y, delta * 2);
        }
    }

    private Vector2 GetHeadRotationTo(Spatial target)
    {
        Vector3 npcForward = -npc.GlobalTransform.basis.z;
        Vector3 dir = GetDirToTarget(target);

        float angle = npcForward.AngleTo(dir);
        if (npc.GlobalTransform.basis.x.Dot(dir) < 0)
        {
            angle = -angle;
        }
        
        var targetY = target.GlobalTranslation.y;
        //точка центра игрока чуть выше, тк он умеет красться и приседать с:
        if (target is Player)
        {
            targetY += 0.25f * npc.lookHeightFactor;
        }

        float diffY = targetY - npc.GlobalTranslation.y;

        return new Vector2(angle / 1.5f, diffY / 5);
    }

    private void SetValueTo(ref float value, float to, float delta)
    {
        if (value > to + 0.05f)
        {
            value -= delta;
        }
        else if (value < to - 0.05f)
        {
            value += delta;
        }
        else
        {
            value = to;
        }
    }
}
