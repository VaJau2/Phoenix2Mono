using System.Collections.Generic;

using Godot;

public class DamageTrigger : ActivateOtherTrigger
{
    [Export] private List<string> characterPaths;
    private List<Character> characters = [];

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);

        foreach (var characterPath in characterPaths)
        {
            var character = GetNodeOrNull<Character>(characterPath);
            
            if (character == null) return;
            
            character.Connect(nameof(Character.TakenDamage), this, nameof(_on_activate_trigger));
            characters.Add(character);
        }
    }
}
