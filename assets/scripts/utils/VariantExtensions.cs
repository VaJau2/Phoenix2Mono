using Godot;
using Godot.Collections;

//В Godot 4 значения в Dictionary/Array хранятся как Variant, а не object,
//поэтому старые проверки "x is Dictionary d" и "x as Array" идут через эти методы
public static class VariantExtensions
{
    public static Dictionary AsDictionaryOrNull(this Variant value) =>
        value.VariantType == Variant.Type.Dictionary ? value.AsGodotDictionary() : null;

    public static Array AsArrayOrNull(this Variant value) =>
        value.VariantType == Variant.Type.Array ? value.AsGodotArray() : null;

    public static bool IsNull(this Variant value) => value.VariantType == Variant.Type.Nil;

    //аналог "value?.ToString()" для object из Godot 3
    public static string AsStringOrNull(this Variant value) => value.IsNull() ? null : value.ToString();
}
