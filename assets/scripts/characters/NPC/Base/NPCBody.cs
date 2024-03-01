using Godot;

public partial class NPCBody
{
    private NPC npc;
    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback playback;
    private Vector2 headBlend;

    public Character lookTarget = null;
    private float lookTimer = 1.5f;

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

    public Vector3 GetDirToTarget(Node3D target)
    {
        Vector3 targetDirPos = target.GlobalTransform.Origin;
        targetDirPos.Y = npc.GlobalTransform.Origin.Y;
        return targetDirPos - npc.GlobalTransform.Origin;
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
        if (npc.state != NPCState.Idle || GodotObject.IsInstanceValid(lookTarget) || body == npc) return;
        if (!(body is Character character)) return;
        lookTimer = 1.5f;
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
        if (GodotObject.IsInstanceValid(lookTarget))
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

            Vector3 npcForward = -npc.GlobalTransform.Basis.Z;
            Vector3 dir = GetDirToTarget(lookTarget);

            float angle = npcForward.AngleTo(dir);
            if (npc.GlobalTransform.Basis.X.Dot(dir) < 0)
            {
                angle = -angle;
            }

            var targetY = lookTarget.GlobalTransform.Origin.Y;
            //точка центра игрока чуть выше, тк он умеет красться и приседать с:
            if (lookTarget is Player)
            {
                targetY -= 0.8f * npc.lookHeightFactor;
            }

            float diffY = targetY - npc.GlobalTransform.Origin.Y;


            SetValueTo(ref headBlend.X, angle / 1.5f, delta * 4);
            SetValueTo(ref headBlend.Y, diffY / 50, delta * 4);
        }
        else
        {
            SetValueTo(ref headBlend.X, 0, delta * 2);
            SetValueTo(ref headBlend.Y, 0, delta * 2);
        }
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