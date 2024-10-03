using System;
using Godot;
using Godot.Collections;

public abstract class Character : KinematicBody, ISavable
{
    public const string IDLE_ANIM = "Idle";
    public const string IDLE_ANIM1 = "Idle1";
    public const float MIN_WALKING_SPEED = 2;
    public int Health {get; protected set;}
    public int HealthMax;
    public float BaseDamageBlock; //от 0 до 1, процентное блокирование
    public int BaseSpeed = 1; //скорость берется каждый кадр, поэтому применяется сразу
    public int BaseDamage;
    public int BaseRecoil;

    public Vector3 Velocity;
    public Vector3 impulse;
    public bool MayMove { get; protected set; } = true;
        
    [Signal]
    public delegate void TakenDamage();
    [Signal]
    public delegate void Die();
    
    [Signal]
    public delegate void ChangeMayMove();

    protected void SetStartHealth(int newHealth)
    {
        Health = HealthMax = newHealth;
    }
    public virtual float GetDamageBlock() => BaseDamageBlock;
    public virtual int GetSpeed()  => BaseSpeed;
    public virtual int GetDamage() => BaseDamage;
    public virtual int GetRecoil() => BaseRecoil;

    public void DecreaseHealth(int decrease) 
    {
        Health -= decrease;
        Health = Mathf.Clamp(Health, 0, HealthMax);
    }

    public virtual void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        EmitSignal(nameof(TakenDamage));
        damage -= (int)(damage * GetDamageBlock());
        DecreaseHealth(damage);
        if (Health <= 0)
        {
            EmitSignal(nameof(Die));
        }
    }
    
    public virtual void CheckShotgunShot(bool isShotgun) {}

    public virtual void HealHealth(int healing)
    {
        if (Health > 0) DecreaseHealth(-healing);
    }

    public void MakeDamage(Character victim, int shapeID = 0) 
    {
        victim.TakeDamage(this, GetDamage(), shapeID);
    }
    
    protected void HandleImpulse() 
    {
        if (impulse.Length() > 0)
        {
            Velocity += impulse;
            Vector3 newImpulse = impulse;
            newImpulse /= 1.5f;
            impulse = newImpulse;
        }
    }
    
    public virtual void SetMayMove(bool value)
    {
        if (MayMove == value) return;
        
        MayMove = value;
        EmitSignal(nameof(ChangeMayMove));
    }

    // Метод должен будет использоваться во время сохранения, когда игра проходит по всем Character
    public virtual Dictionary GetSaveData() 
    {
        var savingData = new Dictionary
        {
            { "pos", GlobalTranslation },
            { "rot", GlobalRotation },
            { "health", Health }
        };

        return savingData;
    }

    // Метод должен будет использоваться во время загрузки, когда игра проходит по всем Character
    public virtual void LoadData(Dictionary data)
    {
        GlobalTranslation = data["pos"].ToString().ParseToVector3();
        GlobalRotation = data["rot"].ToString().ParseToVector3();
        Health = Convert.ToInt32(data["health"]);
    }
}
