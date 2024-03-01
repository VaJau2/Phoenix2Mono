using Godot;
using Godot.Collections;

public partial class Credits : Control
{
    private const float TIMER_COOLDOWN = 3f;
    
    private Array<Node> titles;
    private int step;

    private AnimationPlayer anim;

    private Label skip;
    private AnimationPlayer skipAnim;
    private readonly Color visible = new (1, 1, 1);
    private readonly Color invisible = new (1, 1, 1, 0);
    private double timer;

    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);

        anim = GetNode<AnimationPlayer>("Animator");
        anim.AnimationFinished += _ => NextCredit();

        skip = GetNode<Label>("Skip");
        skipAnim = skip.GetNode<AnimationPlayer>("anim");
        skip.Text = InterfaceLang.GetPhrase("inGame", "labels", "skip");
        skip.Modulate = invisible;
        SetProcess(false);
        
        titles = GetNode("Titles").GetChildren();
        NextCredit();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("jump") && skip.Modulate == visible)
        {
            LoadMainMenu();
        }

        if (@event is not InputEventKey || skip.Modulate == visible) return;
        
        if (skipAnim.IsPlaying()) skipAnim.Stop();
        skip.Modulate = visible;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        timer += delta;

        if (timer < TIMER_COOLDOWN) return;
        
        timer = 0;
        skipAnim.Play("Fade Out Skip");
        SetProcess(false);
    }

    private void NextCredit()
    {
        if (step > titles.Count - 1)
        {
            LoadMainMenu();
            return;
        }

        if (step > 0)
        {
            var previousTitle = (TitleItem)titles[step - 1];
            previousTitle.Visible = false;
        }
        
        var currentItem = (TitleItem)titles[step];
        currentItem.Visible = true;
        
        var animName = currentItem.GetAnimationName();
        anim.Play(animName);
        
        step++;
    }

    private void LoadMainMenu()
    {
        GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
    }
}
