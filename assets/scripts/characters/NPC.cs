using Godot;

//класс отвечает за поведение НПЦ
public class NPC : Character
{
    AnimationPlayer anim;
    AudioStreamPlayer3D audi;
    public override void _Ready()
    {
        anim = GetNode<AnimationPlayer>("anim");
        audi = GetNode<AudioStreamPlayer3D>("audi");

        anim.Play("Idle");
    }

    public override void _Process(float delta)
    {
    }
}
