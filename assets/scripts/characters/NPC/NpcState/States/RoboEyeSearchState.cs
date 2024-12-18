using Godot;

public class RoboEyeSearchState(
    RoboEyeBody body,
    NavigationMovingController movingController,
    StateMachine stateMachine
) : AbstractNpcState
{
    private const float SEARCH_TIMER = 10f;
    
    private Vector3 lastSeePos;
    private float searchTimer;
    
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        searchTimer = SEARCH_TIMER;
        
        if (!IsInstanceValid(npc.tempVictim))
        {
            //врага нет, позиции нет, искать нечего
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }
        
        lastSeePos = npc.tempVictim.GlobalTranslation;
        
        body.ChangeMaterial(RoboEyeMaterial.Orange);
    }

    public override void _Process(float delta)
    {
        if (movingController.cameToPlace) 
        {
            if (searchTimer > 0) 
            {
                searchTimer -= delta;
            } 
            else 
            {
                stateMachine.SetState(SetStateEnum.Idle);
            }
        }
        else 
        {
            movingController.GoTo(lastSeePos);
        }
    }
}
