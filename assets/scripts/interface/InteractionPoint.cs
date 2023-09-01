using Godot;

//Скрипт игрового прицела по центру, может иметь разные виды отображения:
//Если игрок ходит с оружием - крестик
//Если игрок взаимодействует с предметами - квадратные рамочки
//Если игрок ни с чем не взаимодействует - точка
//Плюс, пропадает через время, будучи точкой
public class InteractionPoint : Control
{
    private AnimationPlayer anim;
    private InteractionVariant tempVariant = InteractionVariant.Point;
    
    private Timer fadeTimer;
    private bool isHidden;
    
    public void SetInteractionVariant(InteractionVariant newVariant)
    {
        if (tempVariant == newVariant) return;
        
        Visible = true;
        isHidden = false;
        
        string animName = GetAnimName(newVariant);
        anim.Play(animName);
        
        tempVariant = newVariant;

        if (newVariant != InteractionVariant.Point) return;
        
        if (fadeTimer == null)
        {
            fadeTimer = new Timer();
            fadeTimer.Connect("timeout", this, nameof(HidePoint));
            AddChild(fadeTimer);
        }
            
        fadeTimer.Stop();
        fadeTimer.Start(2);
    }

    public void HidePoint()
    {
        if (tempVariant == InteractionVariant.Point)
        {
            Visible = false;
        }
        
        fadeTimer.QueueFree();
        fadeTimer = null;
    }

    public void HideSquare()
    {
        if (tempVariant != InteractionVariant.Square) return;
        Visible = false;
        isHidden = true;
    }

    public void ShowSquareAgain()
    {
        if (!isHidden) return;
        Visible = true;
        isHidden = false;
    }

    public override void _Ready()
    {
        anim = GetNode<AnimationPlayer>("anim");
        anim.Play("point");
    }

    private string VariantToString(InteractionVariant variant)
    {
        switch (variant)
        {
            case InteractionVariant.Cross: return "cross";
            case InteractionVariant.Point: return "point";
            case InteractionVariant.Square: return "square";
            default: return null;
        }
    }

    private string GetAnimName(InteractionVariant newVariant)
    {
        string oldVariantString = VariantToString(tempVariant);
        string newVariantString = VariantToString(newVariant);
        return oldVariantString + "_to_" + newVariantString;
    }
}

public enum InteractionVariant
{
    Point, Square, Cross
}
