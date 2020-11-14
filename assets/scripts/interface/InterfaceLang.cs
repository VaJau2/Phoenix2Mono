using Godot;
using Godot.Collections;

/// <summary>
/// грузит язык для интерфейса из файлов в папке lang/
/// </summary>
public static class InterfaceLang {
    private static string lang = "ru";

    public static string GetSaveLanguage()
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

    /// <summary>
    /// Вытаскивает фразу нужного языка из лангового файла
    /// </summary>
    /// <param name="file">файл (название без формата)</param>
    /// <param name="section">группа фраз (секция)</param>
    /// <param name="phrase">название фразы</param>
    /// <returns></returns>
    public static string GetLang(string file, string section, string phrase) 
    {
        Dictionary data = Global.loadJsonFile("assets/lang/" + lang + "/" + file + ".json");
        if (data != null) {
            var sectionData = data[section] as Dictionary;
            return sectionData[phrase].ToString();
        }
       
        return "data is null :c";
    }
}

public enum Language 
{
    Russian,
    English
}