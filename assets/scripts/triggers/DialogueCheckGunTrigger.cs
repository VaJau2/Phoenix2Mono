using Godot;

//перед диалогом делает проверку на оружие
//если у игрока нет оружия, запускает другой диалог
public partial class DialogueCheckGunTrigger: DialogueTrigger
{
    [Export] public string codeWithoutGun;

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        
        var player = Global.Get().player;
        if (!player.Weapons.GunOn)
        {
            otherDialogueCode = codeWithoutGun;
        }
        base.OnActivateTrigger();
    }
}