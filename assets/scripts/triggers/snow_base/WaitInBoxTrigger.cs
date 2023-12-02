using System;
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

    private float timer;
    private int step;

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
        saveData["timer"] = timer;
        saveData["step"] = step;
        return saveData;
    }
    
    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        if (!(bool)data["animating"]) return;
        timer = Convert.ToSingle(data["timer"]);
        step = Convert.ToInt16(data["step"]);
        playerHere = Global.Get().player;
        playerHere.SitOnChair(true);
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

        if (!isAnimating)
        {
            isAnimating = true;
            playerHere.SitOnChair(true);
        }

        if (timer > 0)
        {
            timer -= delta;
            return;
        }
        
        AnimateMoving();
    }

    private void AnimateMoving(bool startWait = true)
    {
        switch (step)
        {
            case 0:
                //перемещаем коробку и игрока заранее, чтобы анимация движения коробки не залезала за текстуры
                if (startWait)
                {
                    timer = 3;
                    step = 1;
                }
                else
                {
                    step = 1;
                }

                return;
            
            case 1:
                AmbientVolSync ambientSync = GetNode<AmbientVolSync>(playerHere.GetPath() + "/radioCheck");
                ambientSync.Clear();

                myBox.GlobalTransform = Global.SetNewOrigin(myBox.GlobalTransform, newBoxPosition.GlobalTransform.origin);
                playerHere.GlobalTransform =
                    Global.SetNewOrigin(playerHere.GlobalTransform, newBoxPosition.GlobalTransform.origin);
        
                movingBoxAudi.Stream = movingSound;
                movingBoxAudi.Play();
                timer = 2f;
                step = 2;
                return;
            
            case 2:
                movingBoxAnim.Play("moving");
                timer = 20f;
                step = 3;
                return;
            
            case 3:
                movingBoxAnim.Stop();

                myBox.GetParent().RemoveChild(myBox);
                GetNode("/root/Main/Scene/rooms/2floor").AddChild(myBox);
                myBox.GlobalTransform = Global.SetNewOrigin(myBox.GlobalTransform, newBoxPosition.GlobalTransform.origin);
                Vector3 scale = myBox.Scale;
                myBox.GlobalRotation = newBoxPosition.GlobalRotation;
                myBox.Scale = scale;

                playerHere.GlobalTransform =
                    Global.SetNewOrigin(playerHere.GlobalTransform, newBoxPosition.GlobalTransform.origin);
                playerHere.GlobalRotation = newBoxPosition.GlobalRotation;

                doorToParking.Open(null, true, false);
                playerHere.SitOnChair(false);
                SetProcess(false);
                _on_activate_trigger();
                return;
        }
    }
}
