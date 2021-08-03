using Godot;

//Скрипт поддерживает только одного непися на воде
//Если надо несколько, нужно переводить tempChar в массив
//и добавлять спавн для частиц
public class Water : Spatial
{
    private AudioStreamSample waterSplashSound;
    private Particles parts;

    public void _on_area_body_entered(Node body)
    {
        if (!(body is Character tempChar)) return;
        if (!(tempChar.Velocity.Length() > 6)) return;
        
        Vector3 newPos = tempChar.GlobalTransform.origin;
        newPos.y = parts.GlobalTransform.origin.y;

        parts.GlobalTransform = Global.setNewOrigin(parts.GlobalTransform, newPos);
        parts.Restart();
        
        if (!(tempChar is Player player)) return;
        
        player.GetAudi(true).Stream = waterSplashSound;
        player.GetAudi(true).Play();
    }


    public override void _Ready()
    {
        waterSplashSound = GD.Load<AudioStreamSample>("res://assets/audio/steps/water/land.wav");
        parts = GetNode<Particles>("particles");
    }
}