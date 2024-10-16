using Godot;

public class NPCCovers : Node
{
    private readonly float[] COVER_TIMER = [1f, 5f];
    
    public Cover TempCover;
    public Vector3 TempCoverPlace;
    public float CoverTimer;
    
    public bool IsHidingInCover => TempCover != null;
    public bool InCover;

    private CoversManager coversManager;
    private RandomNumberGenerator rand = new();
    
    private float GetCoverTime()
    {
        var tempTime = rand.RandfRange(COVER_TIMER[0], COVER_TIMER[1]);
        tempTime *= 1 / Global.Get().Settings.npcAggressive;
        return tempTime;
    }
    
    public void FindCover(NPC npc)
    {
        if (coversManager == null) return;
        
        if (InCover)
        {
            CoverTimer = GetCoverTime();
        }
        else
        {
            TempCover = coversManager.GetCover(npc);
            if (TempCover != null)
            {
                TempCoverPlace = TempCover.GetFarPlace(npc.tempVictim.GlobalTranslation);
            }

            InCover = false;
            CoverTimer = GetCoverTime();
        }
    }
    
    public void StopHidingInCover()
    {
        if (TempCover != null) coversManager.ReturnCover(TempCover);
        TempCover = null;
        InCover = false;
    }

    public override void _Ready()
    {
        coversManager = GetNodeOrNull<CoversManager>("/root/Main/Scene/terrain/covers");
    }
}
