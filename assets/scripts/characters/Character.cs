using System;
using Godot;
using Godot.Collections;

public class Character : KinematicBody, ISavable
{
    public const float MIN_WALKING_SPEED = 2;
    public int Health {get; private set;}
    public int HealthMax;
    public float BaseDamageBlock; //от 0 до 1, процентное блокирование
    public int BaseSpeed = 1; //скорость берется каждый кадр, поэтому применяется сразу
    public int BaseDamage;
    public int BaseRecoil;

    public Vector3 Velocity;
    public Vector3 impulse;
    public bool MayMove = true;
        
    [Signal]
    public delegate void TakenDamage();
    [Signal]
    public delegate void Die();

    protected void SetStartHealth(int newHealth)
    {
        Health = HealthMax = newHealth;
    }
    public virtual float GetDamageBlock() => BaseDamageBlock;
    public virtual int GetSpeed()  => BaseSpeed;
    public virtual int GetDamage() => BaseDamage;
    public virtual int GetRecoil() => BaseRecoil;

    private void decreaseHealth(int decrease) 
    {
        Health -= decrease;
        Health = Mathf.Clamp(Health, 0, HealthMax);
    }

    public virtual void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        EmitSignal(nameof(TakenDamage));
        damage -= (int)(damage * GetDamageBlock());
        decreaseHealth(damage);
        if (Health <= 0)
        {
            EmitSignal(nameof(Die));
        }
    }
    
    public virtual void CheckShotgunShot(bool isShotgun) {}

    public virtual void HealHealth(int healing)
    {
        if (Health > 0) decreaseHealth(-healing);
    }

    public void MakeDamage(Character victim, int shapeID = 0) {
        victim.TakeDamage(this, GetDamage(), shapeID);
    }
    
    protected void HandleImpulse() 
    {
        if(impulse.Length() > 0)
        {
            Velocity += impulse;
            Vector3 newImpulse = impulse;
            newImpulse /= 1.5f;
            impulse = newImpulse;
        }
    }

    // Метод должен будет использоваться во время сохранения, когда игра проходит по всем Character
    public virtual Dictionary GetSaveData() 
    {
        Dictionary savingData = new Dictionary
        {
            {"parent", GetParent().Name},  //используется в LevelsLoader
            {"fileName", Filename},
            
            {"pos_x", GlobalTransform.origin.x},
            {"pos_y", GlobalTransform.origin.y},
            {"pos_z", GlobalTransform.origin.z},
            {"rot_x", GlobalTransform.basis.GetEuler().x},
            {"rot_y", GlobalTransform.basis.GetEuler().y},
            {"rot_z", GlobalTransform.basis.GetEuler().z},
            {"health", Health},
        };

        return savingData;
    }

    // Метод должен будет использоваться во время загрузки, когда игра проходит по всем Character
    public virtual void LoadData(Dictionary data) 
    {
        Vector3 newPos = new Vector3(Convert.ToSingle(data["pos_x"]), Convert.ToSingle(data["pos_y"]), Convert.ToSingle(data["pos_z"]));
        Vector3 newRot = new Vector3(Convert.ToSingle(data["rot_x"]), Convert.ToSingle(data["rot_y"]), Convert.ToSingle(data["rot_z"]));
        Vector3 oldScale = Scale;

        Basis newBasis = new Basis(newRot);
        Transform newTransform = new Transform(newBasis, newPos);
        GlobalTransform = newTransform;
        Scale = oldScale;

        Health = Convert.ToInt32(data["health"]);
    }
}
