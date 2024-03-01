using Godot;

//Скрипт включает родительский Spatial, если в область заходит игрок
//И выключает, если он уходит
public partial class VisibleArea : Area3D
{
    public void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        if (!(GetParent() is Node3D parent)) return;
        parent.Visible = true;
    }
    
    public void _on_body_exited(Node body)
    {
        if (!(body is Player player)) return;
        if (player.Health <= 0) return;
        if (!(GetParent() is Node3D parent)) return; 
        parent.Visible = false;
    }
}
