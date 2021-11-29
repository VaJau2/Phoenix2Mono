using Godot;

//Скрипт включает родительский Spatial, если в область заходит игрок
//И выключает, если он уходит
public class VisibleArea : Area
{
    public void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        if (!(GetParent() is Spatial parent)) return;
        parent.Visible = true;
    }
    
    public void _on_body_exited(Node body)
    {
        if (!(body is Player)) return;
        if (!(GetParent() is Spatial parent)) return;
        parent.Visible = false;
    }
}
