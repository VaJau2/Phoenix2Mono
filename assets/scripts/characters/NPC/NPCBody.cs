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

    public void Update(float delta)
    {
        if (lookTarget != null) {
            Vector3 npcForward = npc.GlobalTransform.basis.x;
            Vector3 dir = lookTarget.GlobalTransform.origin - npc.GlobalTransform.origin;

            float diffY = lookTarget.GlobalTransform.origin.y - npc.GlobalTransform.origin.y;

            SetValueTo(ref headBlend.x, npcForward.Dot(dir) / 11, delta * 2);
            SetValueTo(ref headBlend.y, diffY / 10, delta * 2);
        } else {
            if (npc.state == NPCState.Attack) {
                lookTarget = npc.tempVictim;
            }

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