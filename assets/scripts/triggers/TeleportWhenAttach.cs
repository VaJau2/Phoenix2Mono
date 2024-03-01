using Godot;
using Godot.Collections;

//коннектится к событию получения урона у персонажей
//заставляет их пропасть и добавляет вместо них эффект телепорта
public partial class TeleportWhenAttach: TriggerBase
{
    [Export] private PackedScene teleportEffect;
    [Export] private Array<NodePath> charPaths;
    private Array<Character> characters = new();

    public override void _Ready()
    {
        if (IsActive)
        {
            foreach (var charPath in charPaths)
            {
                var tempChar = GetNode<Character>(charPath);
                characters.Add(tempChar);
                tempChar.TakenDamage += OnActivateTrigger;
            }
        }
    }

    public override void SetActive(bool newActive)
    {
        if (newActive)
        {
            if (IsActive)
            {
                //если уже был активен, запускаем триггер
                OnActivateTrigger();
            }
            else
            {
                //если включается первый раз, загружаем события
                base.SetActive(true);
                _Ready();
            }
        }

        base.SetActive(newActive);
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;

        foreach (var npc in characters)
        {
            npc.TakenDamage -= OnActivateTrigger;
            
            if (teleportEffect.Instantiate<Node3D>() is { } effect)
            {
                GetNode("/root/Main/Scene").AddChild(effect);
                effect.GlobalTransform =
                    Global.SetNewOrigin(effect.GlobalTransform, npc.GlobalTransform.Origin);
            }

            Global.AddDeletedObject(npc.Name);
            npc.QueueFree();
        }

        base.OnActivateTrigger();
    }
}
