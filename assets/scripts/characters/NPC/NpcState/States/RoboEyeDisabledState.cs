public class RoboEyeDisabledState(
    NavigationMovingController movingController,
    RoboEyeBody body,
    SeekArea seekArea
) : AbstractNpcState
{
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        movingController.Stop();
        body.ChangeMaterial(RoboEyeMaterial.Dead);
        body.Disable();
        seekArea.SetActive(false);
    }

    public override void _Process(float delta) { }
}
