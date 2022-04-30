using Godot;

//Если игрок попадает в эту область
//Враги не могут его обнаружить
public class InvisibleArea : Area
{
    public void _on_invisibleArea_body_entered(Node body)
    {
        if (!(body is Player player)) return;
        player.IsInvisibleForEnemy = true;
    }

    public void _on_invisibleArea_body_exited(Node body)
    {
        if (!(body is Player player)) return;
        player.IsInvisibleForEnemy = false;
    }
}
