using Godot;
using FurnStairs;

public class FurnStairsTriggerArea : StaticBody, IInteractable
{
    private const string VENT_TALKER_SUB_NAME = "vent_stairs";
    
    [Export] private NodePath ventDoorPath;
    [Export] private AudioStream tableSound;
    [Export] private AudioStream sofaSound;
    [Export] private AudioStream chairSound;
    
    public bool MayInteract => true;
    public string InteractionHintCode => GetInteractionCode();
    
    private FurnStairsClimbChecker checker;
    private FurnStairsSpawner spawner;
    
    private Player player => Global.Get().player;
    private Subtitles subtitles;

    private FurnDoor ventDoor;
    private Spatial furnParent;

    private Vector3 teleportPos;

    private enum FurnType
    {
        None,
        Table,
        Sofa,
        Chair
    }

    public override void _Ready()
    {
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");
        teleportPos = GetNode<Spatial>("teleportPoint").GlobalTranslation;
        furnParent = GetNode<Spatial>("furns");
        
        if (ventDoorPath != null)
        {
            ventDoor = GetNode<FurnDoor>(ventDoorPath);
        }
        
        checker = new FurnStairsClimbChecker(ventDoor, furnParent);
        spawner = new FurnStairsSpawner();
    }

    public void Interact(PlayerCamera interactor)
    {
        var mayClimbOption = checker.CheckMayClimb(player);

        switch (mayClimbOption)
        {
            case MayClimbOption.MayClimb:
                ClimbToVent();
                return;
            
            case MayClimbOption.NeedTable:
            case MayClimbOption.NeedSofa:
            case MayClimbOption.NeedChair:
            {
                if (TryToSpawnFurn(GetItemCodeByOption(mayClimbOption)))
                {
                    return;
                }

                break;
            }
        }

        SayNeedPhrase(mayClimbOption);
    }

    private static FurnType GetItemCodeByOption(MayClimbOption option)
    {
        return option switch
        {
            MayClimbOption.NeedTable => FurnType.Table,
            MayClimbOption.NeedSofa => FurnType.Sofa,
            MayClimbOption.NeedChair => FurnType.Chair,
            _ => FurnType.None
        };
    }

    private bool TryToSpawnFurn(FurnType furnType)
    {
        var typeString = furnType.ToString().ToLower();
        var inventory = player.Inventory;
        
        var furnCode = inventory.FindItemStartsWith(typeString);
        if (furnCode == null) return false;
        
        inventory.RemoveItem(furnCode);

        var furn = spawner.SpawnFurnByItemCode(furnCode);
        furnParent.GetNode<Spatial>(typeString).AddChild(furn);
        furn.Translation = furn.Rotation = Vector3.Zero;

        player.GetAudi(true).Stream = GetAudioFromFurnType(furnType);
        player.GetAudi(true).Play();
        
        return true;
    }

    private AudioStream GetAudioFromFurnType(FurnType type)
    {
        return type switch
        {
            FurnType.Table => tableSound,
            FurnType.Sofa => sofaSound,
            FurnType.Chair => chairSound,
            _ => null
        };
    }

    private void SayNeedPhrase(MayClimbOption mayClimbOption)
    {
        var phraseKey = mayClimbOption.ToString();
        
        subtitles.SetTalker(player.GetAudi(true), VENT_TALKER_SUB_NAME)
            .LoadSubtitlesFile(phraseKey)
            .StartAnimatingText();
    }
    
    private async void ClimbToVent()
    {
        if (ventDoor is { IsOpen: false })
        {
            ventDoor.ClickFurn();
            await ToSignal(ventDoor, nameof(FurnBase.Opened));
        }
        
        player.GlobalTranslation = teleportPos;
    }

    private string GetInteractionCode()
    {
        var inventory = player.Inventory;
        var mayClimbOption = checker.CheckMayClimb(player);

        return mayClimbOption switch
        {
            MayClimbOption.MayClimb => "climb",
            MayClimbOption.NeedTable when inventory.FindItemStartsWith("table") != null => "putTable",
            MayClimbOption.NeedSofa when inventory.FindItemStartsWith("sofa") != null => "putSofa",
            MayClimbOption.NeedChair when inventory.FindItemStartsWith("chair") != null => "putChair",
            _ => "investigate"
        };
    }
}
