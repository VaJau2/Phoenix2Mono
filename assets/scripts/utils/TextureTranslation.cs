using Godot;

//Меняет материал на англ.версию (по умолчанию сначала должна стоять русская)
//Не меняет материал при смене языка для уже загруженного уровня (при желании можно исправить)
public partial class TextureTranslation : MeshInstance3D
{
    [Export] private StandardMaterial3D engMaterial;
    [Export] private int materialNum;
    
    public override void _Ready()
    {
        if (InterfaceLang.GetLanguage() == Language.English)
        {
            Mesh.SurfaceSetMaterial(materialNum, engMaterial);
        }
    }
}
