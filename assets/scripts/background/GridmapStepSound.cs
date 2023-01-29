using Godot;

/**
 * Проверяет, двигается ли игрок внутри объекта гридмапа
 * если двигается, играет звуки
 */
public class GridmapStepSound : GridMap
{
    private const float CHECK_DELAY_TIME = 0.5f;
    
    private const float MAX_VOLUME = -2;
    private const float MIN_VOLUME = -40;
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
    
    public override void _Process(float delta)
    {
        var tempIsOnGridItem = CheckGridWithDelay(delta);
        if (!tempIsOnGridItem || Player.GetCurrentSpeed() <= Character.MIN_WALKING_SPEED)
        {
            StopPlaying(delta);
            return;
        }
        
        UpdatePlaying();
    }

    private void UpdatePlaying()
    {
        audi.UnitDb = MAX_VOLUME;
        audi.GlobalTransform = Global.setNewOrigin(audi.GlobalTransform, Player.GlobalTransform.origin);
        
        if (audi.Playing && audi.Stream == TempSound) return;
        
        audi.Stream = TempSound;
        audi.Play();
    }

    private void StopPlaying(float delta)
    {
        if (!audi.Playing) return;
        
        if (audi.UnitDb > MIN_VOLUME)
        {
            audi.UnitDb -= VOLUME_SPEED * delta;
        }
        else
        {
            audi.Stop();
        }
    }

    private bool CheckGridWithDelay(float delta)
    {
        var tempIsOnGrid = playerIsOnGridItem();
        if (isInGridItem == tempIsOnGrid) return isInGridItem;
        
        if (tempIsOnGrid)
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

    private bool playerIsOnGridItem()
    {
        if (Global.Get().player == null) return false;
        var playerGlobalPos = Global.Get().player.GlobalTransform.origin;
        var playerLocalPos = playerGlobalPos - GlobalTransform.origin;
        var mapPos = WorldToMap(playerLocalPos);
        mapPos.y -= 1;
        var cellItem = GetCellItem((int)mapPos.x, (int)mapPos.y, (int)mapPos.z);
        return cellItem > -1;
    }
}
