using Godot;

namespace Effects;

public partial class StealthBoyEffect : Effect
{
    private Player player;
    public StealthBoyEffect()
    {
        maxTime = 60;
        badEffect = false;
    }

    // проходит по первым дочерним элементам и имет там меш
    private static MeshInstance3D FindMeshesInParent(Node3D parent)
    {
        while (true)
        {
            if (parent == null || parent.GetChildCount() <= 0)
            {
                return null;
            }
            
            parent = parent.GetChild(0) as Node3D;

            if (parent is MeshInstance3D instance)
            {
                return instance;
            }
        }
    }

    private static bool CanChangeTransparency(Resource material)
    {
        return !material.ResourcePath.EndsWith("wings.tres");
    }

    private static void ChangeMeshVisibility(MeshInstance3D meshInstance, bool visible)
    {
        Mesh mesh = meshInstance.Mesh;
        int materialsCount = mesh.GetSurfaceCount();
        for (int i = 0; i < materialsCount; i++)
        {
            StandardMaterial3D tempMaterial = mesh.SurfaceGetMaterial(i) as StandardMaterial3D;
            if (tempMaterial == null) continue;
            if (CanChangeTransparency(tempMaterial))
            {
                tempMaterial.Transparency = visible ? BaseMaterial3D.TransparencyEnum.Disabled : BaseMaterial3D.TransparencyEnum.Alpha;
            }
            
            Color old = tempMaterial.AlbedoColor;
            tempMaterial.AlbedoColor = new Color(old.R, old.G, old.B, visible ? 1 : 0.1f);
        }
    }

    private void ChangePlayerVisibility(bool visible)
    {
        var bodyMesh = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton/Body");
        var bodyThirdMesh = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton/Body_third");

        ChangeMeshVisibility(bodyMesh, visible);
        ChangeMeshVisibility(bodyThirdMesh, visible);
        
        if (Global.Get().playerRace == Race.Pegasus)
        {
            var wingsShadow = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton/Wings");
            wingsShadow.Visible = visible;
        }
        
        ChangeWeaponVisibility(visible);
        ChangeArtifactVisibility(visible);
    }

    public void ChangeWeaponVisibility(bool visible)
    {
        if (player.Weapons.GunOn)
        {
            var weaponParent = player.Weapons.TempWeapon;
            var gunArmature = Global.FindNodeInScene(weaponParent, "Gun-armature") as Node3D;
            var weaponMesh = FindMeshesInParent(gunArmature);
            if (weaponMesh != null)
            {
                ChangeMeshVisibility(weaponMesh, visible);
            }

            if (!player.Weapons.isPistol)
            {
                var weaponBag = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag");
                ChangeMeshVisibility(weaponBag, visible);
            }
        }
    }

    public void ChangeArtifactVisibility(bool visible)
    {
        var artifact = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton/artifact");
        if (artifact.Visible)
        {
            ChangeMeshVisibility(artifact, visible);
        }
    }
    
    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "stealthBoy";
        
        player.StealthBoy = this;
        ChangePlayerVisibility(false);
        
        base.SetOn(handler);
    }

    public override void SetOff(bool startPostEffect = true)
    {
        player.StealthBoy = null;
        player.Inventory.SoundUsingItem(ItemJSON.GetItemData("stealthBoy"));
        ChangePlayerVisibility(true);
        base.SetOff(false);
    }
}