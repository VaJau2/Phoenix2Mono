using Godot;
using Godot.Collections;

/// <summary>
/// грузит язык для интерфейса из файлов в папке lang/
/// </summary>
public static class InterfaceLang {
    private static string lang = "ru";

    public static string GetLang()
    {
        return lang;
    }

    public static void LoadLanguage(string savedLanguage) 
    {
        lang = savedLanguage;
    }

    public static void ChangeLanguage(Language language)
    {
        switch (language) {
            case Language.English:
                lang = "en";
                break;
            case Language.Russian:
                lang = "ru";
                break;
        }
    }

    public static void SetNextLanguage()
    {
        if (lang == "en") {
            lang = "ru";
        } else {
            lang = "en";
        }
    }

    //Возвращает фразу нужного языка из лангового файла
    public static string GetPhrase(string file, string section, string phrase) 
    {
        Dictionary sectionData = GetPhrasesSection(file, section);
        if (sectionData != null) {
            return sectionData[phrase].ToString();
        }
       
        return "data is null :c";
    }

    //Возвращает словарь из фраз из лангового файла
    public static Dictionary GetPhrasesSection(string file, string section) 
    {
        Dictionary data = Global.loadJsonFile("assets/lang/" + lang + "/" + file + ".json");
        if (data != null) {
            var sectionData = data[section] as Dictionary;
            return sectionData;
        }
        return null;
    }

    public static Array GetPhrasesAsArray(string file, string section)
    {
        Dictionary data = Global.loadJsonFile("assets/lang/" + lang + "/" + file + ".json");
        if (data != null) {
            var sectionData = data[section] as Array;
            return sectionData;
        }
        return null;
    }
}

public enum Language 
{
    Russian,
    English
}