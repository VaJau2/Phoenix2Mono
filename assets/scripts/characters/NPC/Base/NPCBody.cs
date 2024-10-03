using Godot;

public class NPCBody
{
    private NPC npc;
    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback playback;
    private Vector2 headBlend;

    private Spatial defaultLookTarget;
    private Spatial lookTarget;
    private float lookTimer = 2f;

    public NPCBody(NPC npc)
    {
        this.npc = npc;

        animTree = npc.GetNode<AnimationTree>("animTree");
        playback = (AnimationNodeStateMachinePlayback) animTree.Get("parameters/StateMachine/playback");
        headBlend = (Vector2) animTree.Get("parameters/BlendSpace2D/blend_position");
        if (!string.IsNullOrEmpty(npc.IdleAnim))
        {
            playback.Start(npc.IdleAnim);
        }
    }

    public void PlayAnim(string animName)
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

    public void StopAnim()
    {
        playback.Stop();
    }

    public Vector3 GetDirToTarget(Spatial target)
    {
        Vector3 targetDirPos = target.GlobalTransform.origin;
        targetDirPos.y = npc.GlobalTransform.origin.y;
        return targetDirPos - npc.GlobalTransform.origin;
    }

    public void SetDefaultLookTarget(Spatial value)
    {
        defaultLookTarget = value;
    }

    public void SetLookTarget(Spatial value)
    {
        lookTarget = value;
    }

    public void Update(float delta)
    {
        if (npc.WalkSpeed == 0)
        {
            return;
        }

        UpdateHeadRotation(delta);
        animTree.Set("parameters/BlendSpace2D/blend_position", headBlend);
    }

    public void _on_lookArea_body_entered(Node body)
    {
        if (npc.state != NPCState.Idle || Object.IsInstanceValid(lookTarget) || body == npc) return;
        if (!(body is Character character)) return;
        lookTimer = 2f;
        lookTarget = character;
    }

    public void _on_lookArea_body_exited(Node body)
    {
        if (npc.state == NPCState.Idle && body == lookTarget)
        {
            lookTarget = null;
        }
    }

    private void UpdateHeadRotation(float delta)
    {
        if (Object.IsInstanceValid(lookTarget))
        {
            if (npc.state == NPCState.Idle)
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