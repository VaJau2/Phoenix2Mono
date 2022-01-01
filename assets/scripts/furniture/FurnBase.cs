using Godot;

public class FurnBase: StaticBody {
    protected Global global;

    [Export]
    public AudioStreamSample OpenSound;
    [Export]
    public AudioStreamSample CloseSound;

    public bool IsOpen {get; private set;}
    public bool OtherSided = false;

    public AudioStreamPlayer3D audi;
    private AnimationPlayer animator;

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        if (HasNode("anim")) {
            animator = GetNode<AnimationPlayer>("anim");
        }
        
        global = Global.Get();
    }

    private async void setOpen(string anim, AudioStreamSample sound, float timer = 0,
                               bool otherSide = false) {
        if (IsInstanceValid(audi)) {
            audi.Stream = sound;
            audi.Play();
        }
        
        if (timer != 0) {
            await global.ToTimer(timer);
        }

        if (animator != null) {
            animator.Play(anim);
        }
        
        IsOpen = !IsOpen;
    }

    public virtual void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null) {
        if (IsOpen) {
            if (OtherSided) {
                setOpen("close-2", CloseSound, timer);
                OtherSided = false;
            } else {
                setOpen("close", CloseSound, timer);
            }
        }
        else {
            var anim = "open";
            if (openAnim != null) {
                anim = openAnim;
            }
            if (openSound == null) {
                setOpen(anim, OpenSound, timer);
            } else {
                setOpen(anim, openSound, timer);
            }
        }
    }

    protected void LoadOpenTrue(bool otherSided)
    {
        IsOpen = true;
        string openAnim = otherSided ? "open-force-2" : "open";
        animator.Play(openAnim);
        animator.Seek(animator.CurrentAnimationLength, true);
    }
}