using Godot;

// Управляет камерой во время диалога с НПЦ
// Искусственно отдаляет игрока от НПЦ, если он стоит слишком близко к нему
public class PlayerDialogueCheck : Node
{
    private const float MIN_NPC_DISTANCE_CHECK = 1f;
    private const float LOOK_POS_DELTA = 1.5f;
    
    private const float MIN_DISTANCE_TO_TALK = 4.6f;
    private const float MOVING_SPEED = 2f;
    
    private Vector3 lookTarget;
    private Player player;
    private NPC npc;

    public override void _Ready()
    {
        player = GetParent<Player>();
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        if (!player.IsTalking || npc == null) return;
        
        player.LookAt(GetLookAtPosition());
        
        var distance = player.GlobalTranslation.DistanceTo(npc.GlobalTranslation);
        
        if (distance < MIN_DISTANCE_TO_TALK)
        {
            var dir = player.GlobalTransform.basis.z;
            player.Velocity = dir * MOVING_SPEED;
        }
        else
        {
            player.Velocity = Vector3.Zero;
        }
    }
    
    public void SetNpcToTalk(NPC newNpc)
    {
        npc = newNpc;
        lookTarget = Vector3.Zero;
        SetProcess(npc != null);
    }
    
    public void SetLookAtTarget(Vector3 position)
    {
        lookTarget = position;
    }
    
    private Vector3 GetLookAtPosition()
    {
        var targetPos = lookTarget != Vector3.Zero
            ? lookTarget
            : npc.GetHeadPosition();
        
        var playerRelativePos = player.GlobalTransform.origin - targetPos;

        targetPos.x += GetLookPosDelta(playerRelativePos.z);
        targetPos.z += GetLookPosDelta(-playerRelativePos.x);
        
        return targetPos;
    }

    private float GetLookPosDelta(float sideRelativePos)
    {
        return sideRelativePos switch
        {
            > MIN_NPC_DISTANCE_CHECK => LOOK_POS_DELTA,
            < MIN_NPC_DISTANCE_CHECK => -LOOK_POS_DELTA,
            _ => 0
        };
    }
}
