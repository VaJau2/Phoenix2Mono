using Godot;
using Godot.Collections;

//базовый класс для эффектов
public class Effect 
{
    protected string iconName;
    protected Effect postEffect = null;
    protected EffectHandler handler;
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
    public virtual void SetOff(bool startPostEffect = true) 
    {
        handler.RemoveEffect(this);
        icon.UpdateTime(0);
    }

    public void StartPostEffect() 
    {
        //если нет других таких же эффектов, накладываем пост-эффект
        if (postEffect != null) {
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