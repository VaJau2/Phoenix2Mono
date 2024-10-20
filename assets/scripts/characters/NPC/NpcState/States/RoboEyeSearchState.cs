using Godot;

public class RoboEyeSearchState(
    RoboEyeBody body,
    NavigationMovingController movingController,
    StateMachine stateMachine
) : INpcState
{
    private const float SEARCH_TIMER = 10f;
    
    private Vector3 lastSeePos;
    private float searchTimer;
    
    public void Enable(NPC npc)
    {
        searchTimer = SEARCH_TIMER;
        
        if (!Object.IsInstanceValid(npc.tempVictim))
        {
            //врага нет, позиции нет, искать нечего
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }
        
        lastSeePos = npc.tempVictim.GlobalTranslation;
        
        body.ChangeMaterial(RoboEyeMaterial.Orange);
    }

    public void Update(NPC npc, float delta)
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
