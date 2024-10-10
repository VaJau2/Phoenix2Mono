public class RoboEyeDisabledState(
    RoboEyeBody body
) : INpcState
{
    public void Enable(NPC npc)
    {
        body.ChangeMaterial(RoboEyeMaterial.Dead);
        body.Disable();
    }

    public void Update(NPC npc, float delta) { }
}
