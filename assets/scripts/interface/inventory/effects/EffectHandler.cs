using Godot;
using System.Collections.Generic;

//обработчик эффектов
//рисует иконки эффектов через интерфейс
//вызывает нужные методы эффектов
public class EffectHandler: Node
{
    const int BAD_EFFECTS_HEARTBEAT = 5;
    const int BAD_EFFECTS_DEALTH = 10;
    private List<Effect> tempEffects = new List<Effect>();
    private List<EffectIcon> effectIcons = new List<EffectIcon>();
    private PackedScene iconPrefab;

    public override void _Ready()
    {
        iconPrefab = GD.Load<PackedScene>("res://objects/interface/EffectIcon.tscn");
    }

    public void AddEffect(Effect newEffect)
    {
        newEffect.SetOn(this);
        tempEffects.Add(newEffect);

        EffectIcon newIcon = (EffectIcon)iconPrefab.Instance();
        AddChild(newIcon);
        newIcon.SetIcon(newEffect.texture);
        newEffect.icon = newIcon;
    }

    public void RemoveEffect(Effect oldEffect)
    {
        tempEffects.Remove(oldEffect);
    }

    public void ClearEffects()
    {
        foreach(Effect effect in tempEffects) {
            effect.SetOff(false);
        }
        tempEffects.Clear();
    }

    public void ClearEffect(Effect effect) 
    {
        if (tempEffects.Contains(effect)) {
            effect.SetOff(false);
            RemoveEffect(effect);
        }
    }

    public override void _Process(float delta)
    {
        if (tempEffects.Count > 0) {
            foreach(Effect effect in tempEffects) {
                if (!effect.Count(delta)) {
                    break;
                }
            }
        }
    }

    public Effect GetEffectByName(string name)
    {
        switch(name) {
            case "heal":
                return new HealEffect();
        }
        return null;
    }
}