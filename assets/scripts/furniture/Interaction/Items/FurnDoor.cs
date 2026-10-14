using System;
using Godot;
using Godot.Collections;

public partial class FurnDoor: FurnBase, ISavable
{
    private const int DOOR_OPEN_DAMAGE = 110;
    
    [Export] public string myKey;
    [Export] public AudioStreamWav closedSound;
    [Export] public AudioStreamWav openWithKeySound;
    [Export] public bool ForceOpening;
    [Export] public float openTimer;
    [Export] public string doorCode;

    public string KeyToRemember;

    public bool opening { get; private set; }
    
    private bool standingOtherSide;
    private Dictionary<string, AudioStreamWav> materalSounds;
    private Player player => Global.Get().player;

    public override void _Ready()
    {
        base._Ready();
        
        string matPath = "res://assets/audio/guns/legHits/";
        materalSounds = new Dictionary<string, AudioStreamWav>
        {
            { "door", GD.Load<AudioStreamWav>(matPath + "door_slam.wav") },
            { "door_open", GD.Load<AudioStreamWav>(matPath + "door_slam_open.wav") },
            { "stone", GD.Load<AudioStreamWav>(matPath + "stone_hit.wav") },
        };
    }
    
    public override void Interact(PlayerCamera interactor)
    {
        var keys = player.Inventory.GetKeys();
        interactor.closedTimer = ClickFurn(keys);
        interactor.onetimeHint = false;
    }

    public void TrySmashOpen(int damage)
    {
        if (IsOpen) return;
        
        if (!ForceOpening)
        {
            audi.Stream = materalSounds["stone"];
            audi.Play();
        }
        else if (damage < DOOR_OPEN_DAMAGE)
        {
            audi.Stream = materalSounds["door"];
            audi.Play();
        }
        else
        {
            SetOpen(materalSounds["door_open"], 0, true);
        }
    }

    public async void SetOpen(AudioStreamWav keySound = null, float timer = 0, bool force = false) 
    {
        string animForce = null;
        
        if (timer > 0 || openTimer > 0) 
        {
            opening = true;
        }

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
            await Global.ToTimer(openTimer);
        }
        
        if (timer != 0) 
        {
            await Global.ToTimer(timer);
        }
        
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
                    SetOpen(openWithKeySound, 0.5f);
                    myKey = "";
                    return 0;
                }
            }
            audi.Stream = closedSound;
            audi.Play();
            return 0.5f;
        }
        
        SetOpen();
        return 0;
    }

    // Открывает дверь без проверки ключа
    public float ClickFurn() 
    {
        if (opening) return openTimer;
        
        if (!string.IsNullOrEmpty(myKey) && !IsOpen) 
        {
            SetOpen(openWithKeySound, 0.5f);
            audi.Stream = openWithKeySound;
            audi.Play();
        } 
        else 
        {
            SetOpen();
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
            {"otherSided", OtherSided},
            {"myKey", myKey}
        };
    }

    public void LoadData(Dictionary data)
    {
        var open = data["open"].AsBool();
        var otherSided = data["otherSided"].AsBool();

        myKey = Convert.ToString(data["myKey"]);
        
        if (!open) return;
        
        LoadOpenTrue(otherSided);
    }
}