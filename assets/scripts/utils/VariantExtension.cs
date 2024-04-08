
public static class VariantExtension
{
	public static T AsEnum<T>(this Godot.Variant value)
    {
        return (T) System.Enum.Parse(typeof(T), value.ToString(), true);
    }
}
