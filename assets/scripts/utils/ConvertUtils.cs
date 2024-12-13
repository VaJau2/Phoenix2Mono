using System.Globalization;
using System;
using Godot;
using Godot.Collections;

public static class ConvertUtils
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
    
    public static string ArrayToString(Array<int> value)
    {
        if (value.Count <= 0) return "";
        var eventsString = "";

        for (var i = 0; i < value.Count; i++)
        {
            eventsString += value[i];
            if (i < value.Count - 1)
            {
                eventsString += ",";
            }
        }
            
        return eventsString;
    }

    public static Array<int> StringToArray(string value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        var connect = value.Split(',');
        
        Array<int> result = [];

        foreach (var item in connect)
        {
            result.Add(int.Parse(item));
        }
        
        return result;
    }
}
