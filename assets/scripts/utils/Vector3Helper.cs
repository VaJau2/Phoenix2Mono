using System.Globalization;
using System;
using Godot;

public static class Vector3Helper
{
    public static Vector3 ParseToVector3(this string value)
    {
        var data = value
            .Trim(['(', ')'])
            .Split(", ");

        return new Vector3(
            Convert.ToSingle(data[0], CultureInfo.InvariantCulture), 
            Convert.ToSingle(data[1], CultureInfo.InvariantCulture), 
            Convert.ToSingle(data[2], CultureInfo.InvariantCulture)
        );
    }
}
