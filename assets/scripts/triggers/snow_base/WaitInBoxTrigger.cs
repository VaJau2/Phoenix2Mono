using Godot;
using Godot.Collections;

//Триггер на базе МТН
//Активируется, если игрок закрывает коробку изнутри
//Лишает игрока движений, анимирует "движение" коробки
//Затем телепортирует игрока в лабораторию
public class WaitInBoxTrigger : TriggerBase
{
    [Export] private NodePath pathToBox;
    [Export] private AudioStream movingSound;
    [Export] private NodePath pathToDoorTeleport;
    private FurnBase myBox;
    private AnimationPlayer movingBoxAnim;
    private AudioStreamPlayer3D movingBoxAudi;
    private Player playerHere;
    private Spatial newBoxPosition;
    private DoorTeleport doorToParking;
    private bool isAnimating;

    public void _on_body_entered(Node body)
    {
        if (!(body is Player player)) return;
        if (isAnimating || !IsActive) return;
        playerHere = player;
    }
    
    public void _on_body_exited(Node body)
    {
        if (!(body is Player)) return;
        if (isAnimating || !IsActive) return;
        playerHere = null;
    }
    
    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["animating"] = isAnimating;
        return saveData;
    }
    
    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        if (!(bool)data["animating"]) return;
        playerHere = Global.Get().player;
        playerHere.SitOnChair(true);
        SetProcess(false);
        AnimateMoving(false);
    }

    public override void _Ready()
    {
        myBox = GetNode<FurnBase>(pathToBox);
        movingBoxAnim = myBox.GetNode<AnimationPlayer>("anim");
        movingBoxAudi = myBox.GetNode<AudioStreamPlayer3D>("audi");
        newBoxPosition = GetNode<Spatial>("newBoxPosition");
        doorToParking = GetNode<DoorTeleport>(pathToDoorTeleport);
    }

    public override void _Process(float delta)
    {
        if (playerHere == null) return;
        if (myBox.IsOpen) return;
        isAnimating = true;
        playerHere.SitOnChair(true);
        AnimateMoving();
        SetProcess(false);
    }

    private async void AnimateMoving(bool startWait = true)
    {
        //перемещаем коробку и игрока заранее, чтобы анимация движения коробки не залезала за текстуры
        if (startWait)
        {
            await Global.Get().ToTimer(3f, this);
        }
       
        myBox.GlobalTransform = Global.setNewOrigin(myBox.GlobalTransform, newBoxPosition.GlobalTransform.origin);
        playerHere.GlobalTransform =
            Global.setNewOrigin(playerHere.GlobalTransform, newBoxPosition.GlobalTransform.origin);
        
        movingBoxAudi.Stream = movingSound;
        movingBoxAudi.Play();
        await Global.Get().ToTimer(2f, this);
        
        movingBoxAnim.Play("moving");
        await Global.Get().ToTimer(20f, this);
        
        movingBoxAnim.Stop();
        myBox.GetParent().RemoveChild(myBox);
        GetNode("/root/Main/Scene/rooms/2floor").AddChild(myBox);
        myBox.GlobalTransform = Global.setNewOrigin(myBox.GlobalTransform, newBoxPosition.GlobalTransform.origin);
        playerHere.GlobalTransform =
            Global.setNewOrigin(playerHere.GlobalTransform, newBoxPosition.GlobalTransform.origin);
        doorToParking.Open(null, true, false);
        playerHere.SitOnChair(false);
        _on_activate_trigger();
    }
}
