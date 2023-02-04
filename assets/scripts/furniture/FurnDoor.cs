using System;
using Godot;
using Godot.Collections;

public class FurnDoor: FurnBase, ISavable
{
    [Export] public string myKey;
    [Export] public AudioStreamSample closedSound;
    [Export] public AudioStreamSample openWithKeySound;
    [Export] public bool ForceOpening;
    [Export] public float openTimer = 0f;
    [Export] public string doorCode;

    public string KeyToRemember;

    public bool opening { get; private set; }
    bool standingOtherSide;

    private Player player => Global.Get().player;
    
    private void SetCollision(uint level) 
    {
        CollisionLayer = level;
        CollisionMask = level == 1 ? level : 0;
    }

    public override void Interact(PlayerCamera interactor)
    {
        var keys = player.inventory.GetKeys();
        interactor.closedTimer = ClickFurn(keys);
        interactor.onetimeHint = false;
    }

    public async void setOpen(AudioStreamSample keySound = null, float timer = 0, bool force = false) 
    {
        if (timer > 0 || openTimer > 0) 
        {
            opening = true;
        }

        string animForce = null;
        if (force)
        {
            myKey = "";
            if (!standingOtherSide) 
            {
                animForce = "open-force";
            } 
            else 
            {
                animForce = "open-force-2";
                OtherSided = true;
            }
        }
        base.ClickFurn(keySound, timer, animForce);
        if (openTimer != 0) 
        {
            await global.ToTimer(openTimer);
        }
        if (timer != 0) 
        {
            await global.ToTimer(timer);
        }

        SetCollision(IsOpen ? (uint)2 : 1);
        opening = false;
    }

    public float ClickFurn(Array<string> keys) 
    {
        if (opening) return 0;
        if (!string.IsNullOrEmpty(myKey) && !IsOpen) 
        {
            if (keys != null) 
            {
                if (keys.Contains(myKey)) 
                {
                    setOpen(openWithKeySound, 0.5f);
                    myKey = "";
                    return 0;
                }
            }
            audi.Stream = closedSound;
            audi.Play();
            return 0.5f;
        } 
        setOpen();
        return 0;
    }

    // Открывает дверь без проверки ключа
    public float ClickFurn() 
    {
        if (opening) return openTimer;
        
        if (!string.IsNullOrEmpty(myKey) && !IsOpen) 
        {
            setOpen(openWithKeySound, 0.5f);
            audi.Stream = openWithKeySound;
            audi.Play();
        } 
        else 
        {
            setOpen();
        }

        return openTimer;
    }

    public void _on_otherside_body_entered(Node body) 
    {
        if (body is Player) 
        {
            standingOtherSide = true;
        }
    }

    public void _on_otherside_body_exited(Node body) 
    {
        if (body is Player) 
        {
            standingOtherSide = false;
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"open", IsOpen},
            {"otherSided", OtherSided}
        };
    }

    public void LoadData(Dictionary data)
    {
        bool open = Convert.ToBoolean(data["open"]);
        bool otherSided = Convert.ToBoolean(data["otherSided"]);
        if (!open) return;
        LoadOpenTrue(otherSided);
        SetCollision(2);
    }
}