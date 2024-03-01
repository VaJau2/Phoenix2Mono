using Godot;

public partial class FurnBase: StaticBody3D, IInteractable
{
    protected Global global;

    [Export] public AudioStreamWav OpenSound;
    [Export] public AudioStreamWav CloseSound;

    public bool IsOpen {get; private set;}
    protected bool OtherSided;

    public AudioStreamPlayer3D audi;
    private AnimationPlayer animator;

    public bool MayInteract => true;
    public string InteractionHintCode => IsOpen ? "close" : "open";

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        if (HasNode("anim")) 
        {
            animator = GetNode<AnimationPlayer>("anim");
        }
        
        global = Global.Get();
    }
    
    public virtual void Interact(PlayerCamera interactor)
    {
        interactor.HideInteractionSquare();
        ClickFurn();
    }

    public async void SetOpen(string anim, AudioStreamWav sound, float timer = 0, bool otherSide = false) 
    {
        if (IsInstanceValid(audi))
        {
            audi.Stream = sound;
            audi.Play();
        }
        
        if (timer != 0) 
        {
            await global.ToTimer(timer);
        }

        animator?.Play(anim);

        IsOpen = !IsOpen;
    }

    public virtual void ClickFurn(AudioStreamWav openSound = null, float timer = 0, string openAnim = null) 
    {
        if (IsOpen) 
        {
            if (OtherSided) 
            {
                SetOpen("close-2", CloseSound, timer);
                OtherSided = false;
            } 
            else 
            {
                SetOpen("close", CloseSound, timer);
            }
        }
        else 
        {
            var anim = "open";
            if (openAnim != null) 
            {
                anim = openAnim;
            }

            SetOpen(anim, openSound ?? OpenSound, timer);
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