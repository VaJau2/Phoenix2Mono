using Godot;

//Скрипт поддерживает только одного непися на воде
//Если надо несколько, нужно переводить tempChar в массив
//и добавлять спавн для частиц
public partial class Water : Node3D
{
    private AudioStreamWav waterSplashSound;
    private GpuParticles3D parts;

    public void _on_area_body_entered(Node body)
    {
        if (!(body is Character tempChar)) return;
        if (!(tempChar.Velocity.Length() > 6)) return;
        
        Vector3 newPos = tempChar.GlobalTransform.Origin;
        newPos.Y = parts.GlobalTransform.Origin.Y;

        parts.GlobalTransform = Global.SetNewOrigin(parts.GlobalTransform, newPos);
        parts.Restart();
        
        if (!(tempChar is Player player)) return;
        
        player.GetAudi(true).Stream = waterSplashSound;
        player.GetAudi(true).Play();
    }


    public override void _Ready()
    {
        waterSplashSound = GD.Load<AudioStreamWav>("res://assets/audio/steps/water/land.wav");
        parts = GetNode<GpuParticles3D>("particles");
    }
}