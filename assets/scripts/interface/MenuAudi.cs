using Godot;

/**
 * Плеер звуков для всех менюшек, в которых нужно играть звуки кликов
 */
public partial class MenuAudi : AudioStreamPlayer
{
    private AudioStream hoverSound;
    private AudioStream clickSound;

    public override void _Ready()
    {
        hoverSound = GD.Load<AudioStream>("res://assets/audio/button_sound.wav");
        clickSound = GD.Load<AudioStream>("res://assets/audio/radio/Switch.ogg");
    }

    public void PlayClick()
    {
        Stream = clickSound;
        Play();
    }

    public void PlayHover()
    {
        if (Playing) return;
        Stream = hoverSound;
        Play();
    }
}
