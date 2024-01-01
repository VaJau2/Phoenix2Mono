using Godot;

/**
 * Внутриигровой канвас
 * играет анимацию плавного черного экрана при загрузке локаций
 */
public class Canvas : CanvasLayer
{
    private const float SPEED = 2.0f;
    private const float TIME = 1.0f;
    
    private ColorRect blackScreen;
    private float timer;

    public override void _Ready()
    {
        blackScreen = GetNode<ColorRect>("black");
        FadeOut();
    }

    public void FadeOut()
    {
        blackScreen.Color = Colors.Black;
        timer = TIME;
        
        SetProcess(true);
    }
    
    public override void _Process(float delta)
    {
        if (timer > 0)
        {
            timer -= delta;
            return;
        }

        if (blackScreen.Color.a > 0)
        {
            var oldColor = blackScreen.Color;
            oldColor.a -= SPEED * delta;
            blackScreen.Color = oldColor;
            return;
        }
        
        SetProcess(false);
    }
}
