using Godot;

//перед диалогом делает проверку на оружие
//если у игрока нет оружия, запускает другой диалог
public class DialogueCheckGunTrigger: DialogueTrigger
{
    [Export] public string codeWithoutGun;

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        var player = Global.Get().player;
        if (!player.Weapons.GunOn)
        {
            otherDialogueCode = codeWithoutGun;
        }
        base._on_activate_trigger();
    }
}