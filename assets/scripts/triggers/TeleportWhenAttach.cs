using Godot;
using Godot.Collections;

//коннектится к событию получения урона у персонажей
//заставляет их пропасть и добавляет вместо них эффект телепорта
public partial class TeleportWhenAttach: TriggerBase
{
    [Export] private PackedScene teleportEffect;
    [Export] private Array<NodePath> charPaths;
    private Array<Character> characters = new Array<Character>();

    public override void _Ready()
    {
        if (IsActive)
        {
            foreach (var charPath in charPaths)
            {
                var tempChar = GetNode<Character>(charPath);
                characters.Add(tempChar);
                tempChar.Connect(Character.SignalName.TakenDamage, new Callable(this, nameof(_on_activate_trigger)));
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
                _on_activate_trigger();
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

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        foreach (var npc in characters)
        {
            npc.Disconnect(Character.SignalName.TakenDamage, new Callable(this, nameof(_on_activate_trigger)));
            if (teleportEffect.Instantiate() is Node3D effect)
            {
                GetNode("/root/Main/Scene").AddChild(effect);
                effect.GlobalTransform =
                    Global.SetNewOrigin(effect.GlobalTransform, npc.GlobalTransform.Origin);
            }

            Global.AddDeletedObject(npc);
            npc.QueueFree();
        }

        base._on_activate_trigger();
    }
}
