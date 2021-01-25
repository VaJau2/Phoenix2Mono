using Godot;

//базовый класс для эффектов
public class Effect 
{
    protected string iconName;
    protected Effect postEffect = null;
    private EffectHandler handler;
    public float time {get; private set;}
    public float maxTime {get; protected set;}
    public StreamTexture texture {get; private set;}
    public EffectIcon icon;
    public bool badEffect = false;
    

    //включение эффекта
    public virtual void SetOn(EffectHandler handler) {
        this.handler = handler;
        time = maxTime;
        texture = GD.Load<StreamTexture>("res://assets/textures/interface/icons/items/" + iconName + ".png");

        handler.ClearEffect(postEffect);
    }

    //выключение эффекта
    //если параметр false, эффект снимается детоксином
    public virtual void SetOff(bool setPostEffect = true) {
        icon.UpdateTime(0);
        if (setPostEffect && postEffect != null) {
            postEffect.SetOn(handler);
        }
        handler.RemoveEffect(this);
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