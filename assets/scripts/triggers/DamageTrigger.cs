using System.Collections.Generic;

using Godot;

public class DamageTrigger : ActivateOtherTrigger
{
    [Export] private List<string> characterPaths;

    public override void _Ready()
    {
        base._Ready();

        if (!IsActive) return;
        
        foreach (var characterPath in characterPaths)
        {
            var character = GetNodeOrNull<Character>(characterPath);
            character?.Connect(nameof(Character.TakenDamage), this, nameof(_on_activate_trigger));
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
                character?.Connect(nameof(Character.TakenDamage), this, nameof(_on_activate_trigger));
            }
        }
        else
        {
            foreach (var characterPath in characterPaths)
            {
                var character = GetNodeOrNull<Character>(characterPath);
                character?.Disconnect(nameof(Character.TakenDamage), this, nameof(_on_activate_trigger));
            }
        }
    }
}
