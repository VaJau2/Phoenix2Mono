using Godot;

/// <summary>
///  Загружает расу игрока из клоновой колбы при первой загрузке уровня AfterwarBelllgatesBase
/// (следующие загрузки рас из колб лежат в PhoenixSystem)
/// </summary>
public partial class RaceLoader : Node
{
    [Export] private NodePath cloneFlaskPath;
    
    public override void _Ready()
    {
        var cloneFlask = GetNode<CloneFlask>(cloneFlaskPath);
        Global.Get().playerRace = cloneFlask.GetRace();
    }
}
