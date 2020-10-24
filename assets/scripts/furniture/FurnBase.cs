using Godot;

public class FurnBase: StaticBody {
    Global global;

    [Export]
    public AudioStreamSample OpenSound;
    [Export]
    public AudioStreamSample CloseSound;

    public OpenType openType = OpenType.Closed;

    private AudioStreamPlayer3D audi;
    private AnimationPlayer animator;

    public bool IsOpen {
        get => (openType == OpenType.Open);
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        animator = GetNode<AnimationPlayer>("anim");
        global = Global.Get();
    }

    private async void setOpen(string anim, AudioStreamSample sound, float timer = 0,
                               bool force = false) {
        audi.Stream = sound;
        audi.Play();
        if (timer != 0) {
            await global.ToTimer(timer);
        }
        animator.Play(anim);
        if (openType == OpenType.Closed) {
            if (force) {
                openType = OpenType.OtherSided;
            } else {
                openType = OpenType.Open;
            }
        } else {
            openType = OpenType.Closed;
        }
    }

    public virtual void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string newAnim = null) {
        switch (openType) {
            case (OpenType.OtherSided):
                setOpen("close-2", CloseSound, timer);
                return;
            case (OpenType.Open):
                setOpen("close", CloseSound, timer);
                return;
        }
        //if closed
        var anim = "open";
        if (newAnim != null) {
            anim = newAnim;
        }
        if (openSound == null) {
            setOpen(anim, OpenSound, timer);
        } else {
            setOpen(anim, openSound, timer);
        }
    }
}

public enum OpenType {
    Open,
    Closed,
    OtherSided //для выбиваемых дверей
}