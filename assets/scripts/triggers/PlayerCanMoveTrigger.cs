using Godot;

public partial class PlayerCanMoveTrigger : ActivateOtherTrigger
{
    [Export] public bool MakeMayMove;
    
    Player player => Global.Get().player;
    
    public override void OnActivateTrigger()
    {
        if (!IsActive) return;

        player.SetMayMove(MakeMayMove);
        base.OnActivateTrigger();
    }
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        OnActivateTrigger();
    }
}
