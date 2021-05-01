using Godot;

public class NPCBody 
{
    private NPC npc;
    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback playback;
    private Vector2 headBlend;

    public Character lookTarget = null;

    public NPCBody(NPC npc) {
        this.npc = npc;

        animTree = npc.GetNode<AnimationTree>("animTree");
        playback = (AnimationNodeStateMachinePlayback)animTree.Get("parameters/StateMachine/playback");
        headBlend = (Vector2)animTree.Get("parameters/BlendSpace2D/blend_position");
        playback.Start("Idle1");
    }

    public void PlayAnim(string animName)
    {
        playback.Travel(animName);
    }

    public Vector3 GetDirToTarget(Spatial target)
    {
        Vector3 targetDirPos = target.GlobalTransform.origin;
        targetDirPos.y = npc.GlobalTransform.origin.y;
        return targetDirPos - npc.GlobalTransform.origin;
    }

    public void Update(float delta)
    {
        if (lookTarget != null) {
            Vector3 npcForward = -npc.GlobalTransform.basis.z;
            Vector3 dir = GetDirToTarget(lookTarget);

            float angle = npcForward.AngleTo(dir);
            if (npc.GlobalTransform.basis.x.Dot(dir) < 0) {
                angle = -angle;
            }

            var targetY = lookTarget.GlobalTransform.origin.y;
            //точка центра игрока чуть выше, тк он умеет красться и приседать с:
            if (lookTarget is Player) {
                targetY -= 0.8f;
            }

            float diffY = targetY - npc.GlobalTransform.origin.y;
            

            SetValueTo(ref headBlend.x, angle / 1.5f, delta * 4);
            SetValueTo(ref headBlend.y, diffY / 50, delta * 4);
        } else {
            SetValueTo(ref headBlend.x, 0, delta * 2);
            SetValueTo(ref headBlend.y, 0, delta * 2);
        }

        animTree.Set("parameters/BlendSpace2D/blend_position", headBlend);
    }

    public void _on_lookArea_body_entered(Node body) 
    {  
        if (body is Character && body != npc) {
            var character = body as Character;
            if (npc.state == NPCState.Idle && lookTarget == null) {
                lookTarget = character;
            }
        }
    }

    public void _on_lookArea_body_exited(Node body)
    {
        if (npc.state == NPCState.Idle && body == lookTarget) {
            lookTarget = null;
        }
    }

    private void SetValueTo(ref float value, float to, float delta) 
    {
        if (value > to + 0.05f) {
            value -= delta;
        } else if (value < to - 0.05f) {
            value += delta;
        } else {
            value = to;
        }
    }
}