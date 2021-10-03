using System;
using Godot;
using Godot.Collections;

public class FurnDoor: FurnBase, ISavable {
    [Export]
    public string myKey;
    [Export]
    public AudioStreamSample closedSound;
    [Export]
    public AudioStreamSample openWithKeySound;
    [Export]
    public bool ForceOpening;
    [Export]
    public float openTimer = 0f;
    [Export] 
    public string doorCode;

    public string KeyToRemember;

    bool opening = false;
    bool standingOtherSide = false;
    
    void setCollision(uint level) {
        CollisionLayer = level;
        if (level == 1) {
            CollisionMask = level;
        } else {
            CollisionMask = 0;
        }
    }

    public async void setOpen(AudioStreamSample keySound = null, float timer = 0, bool force = false) {
        if (timer > 0 || openTimer > 0) {
            opening = true;
        }

        string animForce = null;
        if (force) {
            myKey = "";
            if (!standingOtherSide) {
                animForce = "open-force";
            } else {
                animForce = "open-force-2";
                OtherSided = true;
            }
        }
        base.ClickFurn(keySound, timer, animForce);
        if (openTimer != 0) {
            await global.ToTimer(openTimer);
        }
        if (timer != 0) {
            await global.ToTimer(timer);
        }
        if (IsOpen) {
            setCollision(2);
        } else {
            setCollision(1);
        }
        opening = false;
    }

    public float ClickFurn(Array<string> keys = null) {
        if (!opening) {
            if (!string.IsNullOrEmpty(myKey) && !IsOpen) {
                if (keys != null) {
                    if (keys.Contains(myKey)) {
                        setOpen(openWithKeySound, 0.5f);
                        myKey = "";
                        return 0;
                    }
                }
                audi.Stream = closedSound;
                audi.Play();
                return 0.5f;
            } else {
                setOpen();
            }
        }
        return 0;
    }

    // Открывает дверь без проверки ключа
    public void ClickFurn() {
        if (!opening) {
            if (!string.IsNullOrEmpty(myKey) && !IsOpen) {
                setOpen(openWithKeySound, 0.5f);
                audi.Stream = openWithKeySound;
                audi.Play();
            } else {
                setOpen();
            }
        }
    }

    public void _on_otherside_body_entered(Node body) {
        if (body is Player) {
            standingOtherSide = true;
        }
    }

    public void _on_otherside_body_exited(Node body) {
        if (body is Player) {
            standingOtherSide = false;
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"open", IsOpen},
            {"otherSided", OtherSided}
        };
    }

    public void LoadData(Dictionary data)
    {
        bool open = Convert.ToBoolean(data["open"]);
        bool otherSided = Convert.ToBoolean(data["otherSided"]);
        if (open)
        {
            LoadOpenTrue(otherSided);
            setCollision(2);
        }
    }
}