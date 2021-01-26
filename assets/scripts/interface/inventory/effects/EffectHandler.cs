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
    public Messages messages;

    private Dictionary<string, int> startParameters = new Dictionary<string, int>();

    //параметр передается по ссылке, чтоб изменяться прям внутри метода
    //и одновременно сохранять стартовое значение в переменную
    public void SetPlayerParameter(string parameterName, ref int playerParameter, int delta) 
    {
        if (startParameters.ContainsKey(parameterName)) {
            GD.Print("someone is trying to set the same effect!");
            return;
        }
        startParameters.Add(parameterName, playerParameter);
        playerParameter += delta;
        if (playerParameter < 0) playerParameter = 0;
    }

    public void ClearPlayerParameter(string parameterName, ref int playerParameter)
    {
        playerParameter = startParameters[parameterName];
        startParameters.Remove(parameterName);
    }

    public bool HasEffect(Effect effect)
    {
        foreach(Effect temp in tempEffects) {
            if (effect.GetType() == temp.GetType()) {
                return true;
            }
        }
        return false;
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
            effect.SetOff();
        }
        tempEffects.Clear();
    }

    public void ClearEffect(Effect effect) 
    {
        if (tempEffects.Contains(effect)) {
            effect.SetOff();
            RemoveEffect(effect);
        }
    }

    public Effect GetEffectByName(string name)
    {
        switch(name) {
            case "heal":
                return new HealEffect();
            case "buck":
                return new BuckEffect();
        }
        return null;
    }

    public override void _Ready()
    {
        iconPrefab = GD.Load<PackedScene>("res://objects/interface/EffectIcon.tscn");
        messages = GetNode<Messages>("../messages");
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
}