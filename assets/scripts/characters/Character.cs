using Godot;
using Godot.Collections;

public class Character : KinematicBody, ISavable
{
    public int Health {get; private set;}
    public int HealthMax;
    public float BaseDamageBlock; //от 0 до 1, процентное блокирование
    public int BaseSpeed = 1; //скорость берется каждый кадр, поэтому применяется сразу
    public int BaseDamage;
    public int BaseRecoil;

    public Vector3 Velocity;

    protected void SetStartHealth(int newHealth)
    {
        Health = HealthMax = newHealth;
    }
    public virtual float GetDamageBlock() => BaseDamageBlock;
    public virtual int GetSpeed()  => BaseSpeed;
    public virtual int GetDamage() => BaseDamage;
    public virtual int GetRecoil() => BaseRecoil;

    protected void decreaseHealth(int decrease) 
    {
        Health -= decrease;
        Health = Mathf.Clamp(Health, 0, HealthMax);
    }

    public virtual void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        damage = damage - (int)(damage * GetDamageBlock());
        decreaseHealth(damage);
    }
    
    public virtual void CheckShotgunShot(bool isShotgun) {}

    public virtual void HealHealth(int healing)
    {
        if (Health > 0) decreaseHealth(-healing);
    }

    public void MakeDamage(Character victim, int shapeID = 0) {
        victim.TakeDamage(this, GetDamage(), shapeID);
    }

    // Метод должен будет использоваться во время сохранения, когда игра проходит по всем Character
    // код загрузки лежит в global.cs
    public virtual Dictionary GetSaveData() 
    {
        Dictionary savingData = new Dictionary
        {
            {"pos_x", GlobalTransform.origin.x.ToString()},
            {"pos_y", GlobalTransform.origin.y.ToString()},
            {"pos_z", GlobalTransform.origin.z.ToString()},
            {"rot_x", GlobalTransform.basis.GetEuler().x.ToString()},
            {"rot_y", GlobalTransform.basis.GetEuler().y.ToString()},
            {"rot_z", GlobalTransform.basis.GetEuler().z.ToString()},
            {"health", Health.ToString()},
        };

        return savingData;
    }

    // Метод должен будет использоваться во время загрузки, когда игра проходит по всем Character
    public virtual void LoadData(Dictionary data) 
    {
        Vector3 newPos = new Vector3((float)data["pos_x"], (float)data["pos_y"], (float)data["pos_z"]);
        Vector3 newRot = new Vector3((float)data["rot_x"], (float)data["rot_y"], (float)data["rot_z"]);

        Basis newBasis = new Basis(newRot);
        Transform newTransform = new Transform(newBasis, newPos);
        GlobalTransform = newTransform;

        Health = (int)data["health"];
    }
}
