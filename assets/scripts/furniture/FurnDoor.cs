using System;
using Godot;
using Godot.Collections;

public class FurnDoor: FurnBase, ISavable
{
    private const int DOOR_OPEN_DAMAGE = 110;
    
    [Export] public string myKey;
    [Export] public AudioStreamSample closedSound;
    [Export] public AudioStreamSample openWithKeySound;
    [Export] public bool ForceOpening;
    [Export] public float openTimer;
    [Export] public string doorCode;

    public string KeyToRemember;

    public bool opening { get; private set; }
    
    private bool standingOtherSide;
    private Dictionary<string, AudioStreamSample> materalSounds;
    private Player player => Global.Get().player;

    public override void _Ready()
    {
        base._Ready();
        
        string matPath = "res://assets/audio/guns/legHits/";
        materalSounds = new Dictionary<string, AudioStreamSample>
        {
            { "door", GD.Load<AudioStreamSample>(matPath + "door_slam.wav") },
            { "door_open", GD.Load<AudioStreamSample>(matPath + "door_slam_open.wav") },
            { "stone", GD.Load<AudioStreamSample>(matPath + "stone_hit.wav") },
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

    public async void SetOpen(AudioStreamSample keySound = null, float timer = 0, bool force = false) 
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
            await global.ToTimer(openTimer);
        }
        
        if (timer != 0) 
        {
            await global.ToTimer(timer);
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
        var open = Convert.ToBoolean(data["open"]);
        var otherSided = Convert.ToBoolean(data["otherSided"]);

        myKey = Convert.ToString(data["myKey"]);
        
        if (!open) return;
        
        LoadOpenTrue(otherSided);
    }
}