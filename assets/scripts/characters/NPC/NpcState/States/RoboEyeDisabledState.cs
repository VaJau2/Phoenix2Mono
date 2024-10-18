public class RoboEyeDisabledState(
    NavigationMovingController movingController,
    RoboEyeBody body,
    SeekArea seekArea
) : INpcState
{
    public void Enable(NPC npc)
    {
        movingController.Stop();
        body.ChangeMaterial(RoboEyeMaterial.Dead);
        body.Disable();
        seekArea.SetActive(false);
    }

    public void Update(NPC npc, float delta) { }
}
