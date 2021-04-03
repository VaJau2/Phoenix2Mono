using Godot;
using System.Collections.Generic;
using System.Linq;

//обработчик эффектов
//рисует иконки эффектов через интерфейс
//вызывает нужные методы эффектов
public class EffectHandler: Node
{
    public List<Effect> tempEffects { get; } = new List<Effect>();
    private PackedScene iconPrefab;
    public Messages messages;
    private HeartbeatEffect heartbeat = new HeartbeatEffect();

    private Player player => Global.Get().player;

    private Dictionary<string, int> startParameters = new Dictionary<string, int>();
    private Dictionary<string, float> startFloatParameters = new Dictionary<string, float>();

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

    //копия для float-параметров
    public void SetPlayerParameter(string parameterName, ref float playerParameter, float delta) 
    {
        if (startParameters.ContainsKey(parameterName)) {
            GD.Print("someone is trying to set the same effect!");
            return;
        }
        startFloatParameters.Add(parameterName, playerParameter);
        playerParameter += delta;
        if (playerParameter < 0) playerParameter = 0;
    }

    public void ClearPlayerParameter(string parameterName, ref float playerParameter)
    {
        playerParameter = startFloatParameters[parameterName];
        startFloatParameters.Remove(parameterName);
    }

    public bool HasEffect(Effect effect)
    {
        return tempEffects.Any(temp => effect.GetType() == temp.GetType());
    }

    public bool HasEffectWithEmotion(string emotion)
    {
        return tempEffects.Any(temp => temp.emotion == emotion);
    }

    public void AddEffect(Effect newEffect)
    {
        newEffect.SetOn(this);
        tempEffects.Add(newEffect);

        EffectIcon newIcon = (EffectIcon)iconPrefab.Instance();
        AddChild(newIcon);
        newIcon.SetData(newEffect.GetType().Name, newEffect.iconTexture);
        newEffect.icon = newIcon;

        heartbeat.CheckAddEffect(newEffect);
        CheckEmotions(newEffect.emotion);
    }

    public void RemoveEffect(Effect oldEffect, bool changeHeartbeat = true)
    {
        tempEffects.Remove(oldEffect);
        if (changeHeartbeat) {
            heartbeat.CheckRemoveEffect(oldEffect);
            CheckEmotions("empty");
        }
    }

    public void ClearEffects()
    {
        while(tempEffects.Count > 0) {
            tempEffects[0].SetOff(false);
        }
        tempEffects.Clear();
        heartbeat.ClearEffects();
        CheckEmotions("empty");
    }

    //очищаем эффект нужного класса
    public void ClearEffect(Effect effect) 
    {
        //эффект в массиве и класс убираемого эффекта - не всегда одно и то же
        Effect effectInList = GetTheSameEffect(effect);
        effectInList?.SetOff();
    }

    public Effect GetTheSameEffect(Effect effect)
    {
        return tempEffects.FirstOrDefault(temp => effect.GetType() == temp.GetType());
    }

    public static Effect GetEffectByName(string name)
    {
        switch(name) {
            case "heal":
                return new HealEffect();
            case "mana":
                return new ManaEffect();
            case "buck":
                return new BuckEffect();
            case "dash":
                return new DashEffect();
            case "hydra":
                return new HydraEffect();
            case "rage":
                return new RageEffect();
            case "medX":
                return new MedXEffect();
            case "mentats":
                return new MentatsEffect();
            case "detoxine":
                return new DetoxineEffect();
        }
        return null;
    }

    public static string GetNameByEffect(Effect effect)
    {
        switch (effect.GetType().ToString())
        {
            case "HealEffect": return "heal";
            case "ManaEffect": return "mana";
            case "BuckEffect": return "buck";
            case "DashEffect": return "dash";
            case "HydraEffect": return "hydra";
            case "RageEffect": return "rage";
            case "MedXEffect": return "medX";
            case "MentatsEffect": return "mentats";
            case "DetoxineEffect": return "detoxine";
            
        }

        return null;
    }

    private void CheckEmotions(string newEmotion) 
    {
        switch (newEmotion) {
            case "empty":
                if (!HasEffectWithEmotion("meds") && 
                    !HasEffectWithEmotion("meds_after")) {
                    player.Body.Head.SetEmptyFace();
                }
                break;
            case "meds":
                player.Body.Head.MedsOn();
                break;
            case "meds_after":
                if (!HasEffectWithEmotion("meds")) {
                    player.Body.Head.MedsAfterOn();
                }
                break;
        }
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
            heartbeat.CheckOverdose(delta);
        }
    }
}