using Godot;

public class FurnBase: StaticBody, IInteractable
{
    protected Global Global => Global.Get();

    [Export] public AudioStreamSample OpenSound;
    [Export] public AudioStreamSample CloseSound;

    public bool IsOpen {get; private set;}
    protected bool OtherSided;

    public AudioStreamPlayer3D audi;
    private AnimationPlayer animator;

    public bool MayInteract => true;
    public string InteractionHintCode => IsOpen ? "close" : "open";

    public bool IsAnimating => animator.IsPlaying();
    
    [Signal]
    public delegate void AnimationFinished();
    
    [Signal]
    public delegate void Opened();

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        if (HasNode("anim")) 
        {
            animator = GetNode<AnimationPlayer>("anim");
            animator.Connect("animation_finished", this, nameof(AnimFinished));
        }
    }
    
    private void AnimFinished(string animation)
    {
        EmitSignal(nameof(AnimationFinished));
        
        if (animation.ToLower().Contains("open"))
        {
            EmitSignal(nameof(Opened));
        }
    }
    
    public virtual void Interact(PlayerCamera interactor)
    {
        interactor.HideInteractionSquare();
        ClickFurn();
    }

    public async void SetOpen(string anim, AudioStreamSample sound, float timer = 0, bool otherSide = false) 
    {
        if (IsInstanceValid(audi))
        {
            audi.Stream = sound;
            audi.Play();
        }
        
        if (timer != 0) 
        {
            await Global.ToTimer(timer);
        }

        animator?.Play(anim);

        IsOpen = !IsOpen;
    }

    public virtual void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null) 
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