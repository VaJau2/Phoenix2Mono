using Godot;

public class InteractionPointManager : Control
{
    private InteractionPoint point;
    private InteractionPoint shadow;
    
    public override async void _Ready()
    {
        await ToSignal(GetTree(), "idle_frame");
        MenuBase.LoadColorForChildren(this);
        
        point = GetNode<InteractionPoint>("point");
        shadow = GetNode<InteractionPoint>("pointShadow");
    }
    
    public void SetInteractionVariant(InteractionVariant newVariant)
    {
        shadow.RectPosition = (newVariant == InteractionVariant.Square)
            ? new Vector2(2, 2)
            : new Vector2(1, 1);

        point.SetInteractionVariant(newVariant);
        shadow.SetInteractionVariant(newVariant);
    }
    
    public void HideSquare()
    {
        point.HideSquare();
        shadow.HideSquare();
    }

    public void ShowSquareAgain()
    {
        point.ShowSquareAgain();
        shadow.ShowSquareAgain();
    }
}
