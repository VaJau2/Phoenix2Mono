using System;
using Godot.Collections;
using Array = Godot.Collections.Array;

/// <summary>
/// грузит язык для интерфейса из файлов в папке lang/
/// </summary>
public static class InterfaceLang 
{
    private static string lang = "ru";

    public static string GetLang()
    {
        return lang;
    }

    public static void LoadLanguage(string savedLanguage) 
    {
        lang = savedLanguage;
    }

    public static Language GetLanguage()
    {
        return lang == "en" ? Language.English : Language.Russian;
    }

    public static void ChangeLanguage(Language language)
    {
        switch (language) 
        {
            case Language.English:
                lang = "en";
                break;
            case Language.Russian:
                lang = "ru";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(language), language, null);
        }
    }

    public static void SetNextLanguage()
    {
        lang = lang == "en" ? "ru" : "en";
    }

    //Возвращает фразу нужного языка из лангового файла
    public static string GetPhrase(string file, string section, string phrase) 
    {
        Dictionary sectionData = GetPhrasesSection(file, section);
        if (sectionData == null || !sectionData.Contains(phrase))
        {
            return null;
        }
            
        var message = sectionData[phrase].ToString();
        if (message.Contains("#"))
        {
            message = ReplaceKeys(message);
        }
            
        return message;
    }

    //Возвращает словарь из фраз из лангового файла
    public static Dictionary GetPhrasesSection(string file, string section) 
    {
        Dictionary data = Global.loadJsonFile("assets/lang/" + lang + "/" + file + ".json");
        if (data == null || !data.Contains(section)) return null;
        var sectionData = data[section] as Dictionary;
        return sectionData;
    }

    public static Array GetPhrasesAsArray(string file, string section)
    {
        Dictionary data = Global.loadJsonFile("assets/lang/" + lang + "/" + file + ".json");
        var sectionData = data?[section] as Array;
        return sectionData;
    }
    
    //заменяет все #key#-значения с кодами кнопок на текущие кнопки из настроек управления
    private static string ReplaceKeys(string message)
    {
        var codes = message.Split('#');
        foreach (var tempCode in codes)
        {
            if (!(Global.GetKeyName(tempCode) is string newKey)) continue;
            message = message.Replace("#" + tempCode + "#", newKey);
        }

        return message;
    }
}

public enum Language 
{
    Russian,
    English
}