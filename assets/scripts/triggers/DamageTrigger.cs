using System.Collections.Generic;

using Godot;

public partial class DamageTrigger : ActivateOtherTrigger
{
    [Export] private List<string> characterPaths;

    public override void _Ready()
    {
        base._Ready();

        if (!IsActive) return;
        
        foreach (var characterPath in characterPaths)
        {
            var character = GetNodeOrNull<Character>(characterPath);
            character?.Connect(Character.SignalName.TakenDamage, new Callable(this, nameof(_on_activate_trigger)));
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);

        if (newActive)
        {
            foreach (var characterPath in characterPaths)
            {
                var character = GetNodeOrNull<Character>(characterPath);
                character?.Connect(Character.SignalName.TakenDamage, new Callable(this, nameof(_on_activate_trigger)));
            }
        }
        else
        {
            foreach (var characterPath in characterPaths)
            {
                var character = GetNodeOrNull<Character>(characterPath);
                character?.Disconnect(Character.SignalName.TakenDamage, new Callable(this, nameof(_on_activate_trigger)));
            }
        }
    }
}
