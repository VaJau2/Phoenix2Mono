using Godot;
using Godot.Collections;

//Скрипт игрового прицела по центру, может иметь разные виды отображения:
//Если игрок ходит с оружием - крестик
//Если игрок взаимодействует с предметами - квадратные рамочки
//Если игрок ни с чем не взаимодействует - точка
//Плюс, пропадает через время, будучи точкой
public class InteractionPoint : Control
{
    const int ANIM_SPEED = 10;
    private const float ANIM_MAX_TIME = 1f;
    
    private Array<ColorRect> borders;

    private Vector2 borderPositionPoint;
    private Vector2 borderSizePoint;
    private Array<Vector2> borderPositionsSquare;
    private Array<Vector2> borderSizesSquare;
    private Array<Vector2> borderPositionsCross;
    private Array<Vector2> borderSizesCross;

    private InteractionVariant tempVariant = InteractionVariant.Point;

    private float animTimer;
    private Timer fadeTimer;

    public void SetInteractionVariant(InteractionVariant newVariant)
    {
        Visible = true;
        
        if (tempVariant == newVariant) return;
        tempVariant = newVariant;
        animTimer = 0;
        SetProcess(true);

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

    public override async void _Ready()
    {
        borders = new Array<ColorRect>();
        foreach (var child in GetChildren())
        {
            if (!(child is ColorRect rect)) continue;
            borders.Add(rect);
        }

        borderPositionPoint = new Vector2(23, 23);
        borderSizePoint = new Vector2(5, 5);

        borderPositionsSquare = new Array<Vector2>
        {
            new Vector2(0,0),
            new Vector2(0,0),
            new Vector2(35,0),
            new Vector2(47,0),
            new Vector2(35,47),
            new Vector2(47,35),
            new Vector2(0,47),
            new Vector2(0,35),
        };
        
        borderSizesSquare = new Array<Vector2>
        {
            new Vector2(15,3),
            new Vector2(3,15),
            new Vector2(15,3),
            new Vector2(3,15),
            new Vector2(15,3),
            new Vector2(3,15),
            new Vector2(15,3),
            new Vector2(3,15),
        };

        borderPositionsCross = new Array<Vector2>
        {
            new Vector2(24,12),
            new Vector2(12, 24),
            new Vector2(28, 24),
            new Vector2(28, 24),
            new Vector2(24, 28),
            new Vector2(24, 28),
            new Vector2(12, 24),
            new Vector2(24, 28),
        };
        
        borderSizesCross = new Array<Vector2>
        {
            new Vector2(2,10),
            new Vector2(10, 2),
            new Vector2(10, 2),
            new Vector2(10, 2),
            new Vector2(2, 10),
            new Vector2(2, 10),
            new Vector2(10, 2),
            new Vector2(2, 10),
        };

        
        SetProcess(false);

        await ToSignal(GetTree(), "idle_frame");
        MenuBase.LoadColorForChildren(this);
    }

    private Vector2 GetNewBorderPosition(int i)
    {
        switch (tempVariant)
        {
            case InteractionVariant.Point: return borderPositionPoint;
            case InteractionVariant.Square: return borderPositionsSquare[i];
            case InteractionVariant.Cross: return borderPositionsCross[i];
            default: return Vector2.Zero;
        }
    }

    private Vector2 GetNewBorderSize(int i)
    {
        switch (tempVariant)
        {
            case InteractionVariant.Point: return borderSizePoint;
            case InteractionVariant.Square: return borderSizesSquare[i];
            case InteractionVariant.Cross: return borderSizesCross[i];
            default: return Vector2.Zero;
        }
    }

    private void FinishAnimation()
    {
        int i = 0;
        
        foreach (var border in borders)
        {
            border.RectPosition = GetNewBorderPosition(i);
            border.RectSize = GetNewBorderSize(i);
            i++;
        }

        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        for (var i = 0; i < borders.Count; i++)
        {
            var border = borders[i];
            
            var posX = border.RectPosition.x;
            var posY = border.RectPosition.y;
            var sizeX = border.RectSize.x;
            var sizeY = border.RectSize.y;

            var newPos = GetNewBorderPosition(i);
            var newSize = GetNewBorderSize(i);

            posX = Mathf.Lerp(posX, newPos.x, delta * ANIM_SPEED);
            posY = Mathf.Lerp(posY, newPos.y, delta * ANIM_SPEED);
            sizeX = Mathf.Lerp(sizeX, newSize.x, delta * ANIM_SPEED);
            sizeY = Mathf.Lerp(sizeY, newSize.y, delta * ANIM_SPEED);

            border.RectPosition = new Vector2(posX, posY);
            border.RectSize = new Vector2(sizeX, sizeY);
        }

        if (animTimer < ANIM_MAX_TIME)
        {
            animTimer += delta;
            return;
        }

        FinishAnimation();
    }
}

public enum InteractionVariant
{
    Point, Square, Cross
}
