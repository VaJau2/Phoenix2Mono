using Godot;

/**
 * Проверяет, двигается ли игрок внутри объекта гридмапа
 * если двигается, играет звуки
 */
public partial class GridmapStepSound : GridMap
{
    private const float CHECK_DELAY_TIME = 0.5f;
    
    private const float MIN_VOLUME = -80;
    private const float VOLUME_SPEED = 15f;

    [Export] private AudioStream walkingSound;
    [Export] private AudioStream crouchingSound;

    private float delayTimer;
    private bool isInGridItem;

    private static Player Player => Global.Get().player;

    private AudioStream TempSound => Player.IsCrouching && crouchingSound != null
        ? crouchingSound 
        : walkingSound;

    private AudioStreamPlayer3D audi;

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("sound");
    }
    
    public override void _Process(double delta)
    {
        if (Player == null) return;

        bool tempIsOnGridItem = CheckGridWithDelay((float)delta);
        bool isMove = Player.GetCurrentSpeed() > Character.MIN_WALKING_SPEED;
        bool isRotate = Player.IsIgnoringRotation() && (!Player.ThirdView || (Player.ThirdView && Player.Weapons.GunOn));

        if (tempIsOnGridItem && (isMove || isRotate)) UpdatePlaying();
        else StopPlaying((float)delta);
    }

    private void UpdatePlaying()
    {
        audi.VolumeDb = Player.IsCrouching ? 4 : -2;
        audi.GlobalTransform = Global.SetNewOrigin(audi.GlobalTransform, Player.GlobalTransform.Origin);

        if (!audi.Playing || audi.Stream != TempSound)
        {
            audi.Stream = TempSound;
            audi.Play();
        }
    }

    private void StopPlaying(float delta)
    {
        if (audi.Playing)
        {
            if (audi.VolumeDb > MIN_VOLUME) audi.VolumeDb -= VOLUME_SPEED * delta;
            else audi.Stop();
        }
    }

    private bool CheckGridWithDelay(float delta)
    {
        var tempIsOnGridItem = PlayerIsOnGridItem();
        if (isInGridItem == tempIsOnGridItem) return isInGridItem;
        
        if (tempIsOnGridItem)
        {
            delayTimer = CHECK_DELAY_TIME;
            isInGridItem = true;
        }
        else
        {
            if (delayTimer > 0)
            {
                delayTimer -= delta;
            }
            else
            {
                isInGridItem = false;
            }
        }

        return isInGridItem;
    }

    private bool PlayerIsOnGridItem()
    {
        if (Global.Get().player == null) return false;
        var playerGlobalPos = Global.Get().player.GlobalTransform.Origin;
        var playerLocalPos = playerGlobalPos - GlobalTransform.Origin;
        var mapPos = LocalToMap(playerLocalPos);
        if (!Player.IsCrouching) mapPos.Y -= 1;
        var cellItem = GetCellItem(new Vector3I(mapPos.X, mapPos.Y, mapPos.Z));
        return cellItem > -1;
    }
}
