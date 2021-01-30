using Godot;
using Godot.Collections;

//базовый класс для эффектов
public class Effect 
{
    protected string iconName;
    protected Effect postEffect = null;
    protected float postEffectChance = 1f;
    protected EffectHandler handler;
    public float time {get; private set;}
    public float maxTime {get; protected set;}
    public string emotion {get; protected set;} = null;
    public StreamTexture iconTexture {get; private set;}
    public EffectIcon icon;
    public bool badEffect = false;

    //включение эффекта
    public virtual void SetOn(EffectHandler handler) {
        this.handler = handler;
        time = maxTime;
        iconTexture = GD.Load<StreamTexture>("res://assets/textures/interface/icons/items/" + iconName + ".png");

        if (postEffect != null) handler.ClearEffect(postEffect);
    }

    //выключение эффекта
    //если параметр false, эффект снимается детоксином
    public virtual void SetOff(bool startPostEffect = true) 
    {
        handler.RemoveEffect(this, startPostEffect);
        icon.UpdateTime(0);
    }

    public void StartPostEffect() 
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        if (postEffect != null && rng.Randf() <= postEffectChance) {
            handler.messages.ShowMessage("medsOff", "items", 2.5f);
            handler.AddEffect(postEffect);
        }
    }

    //процесс эффекта
    public virtual bool Count(float delta) {
        time -= delta;
        icon.UpdateTime(time, maxTime);
        if (time <= 0) {
            SetOff();
        }
        return time > 0;
    }
}