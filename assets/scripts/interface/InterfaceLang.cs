using Godot;
using Godot.Collections;

/// <summary>
/// грузит язык для интерфейса из файлов в папке lang/
/// </summary>
public static class InterfaceLang {
    private static string lang = "ru";

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

    /// <summary>
    /// Вытаскивает фразу нужного языка из лангового файла
    /// </summary>
    /// <param name="file">файл (название без формата)</param>
    /// <param name="section">группа фраз (секция)</param>
    /// <param name="phrase">название фразы</param>
    /// <returns></returns>
    public static string GetLang(string file, string section, string phrase) 
    {
        File langFile = new File();
        string path = "res://assets/lang/" + lang + "/" + file + ".json";

        Error fileError = langFile.Open(path, File.ModeFlags.Read);
        if (fileError == Error.Ok) {
            var text_json = langFile.GetAsText();
            langFile.Close();
            var result_json = JSON.Parse(text_json);

            if (result_json.Error == Error.Ok) {  
                var data = (Dictionary)result_json.Result;
                var sectionData = data[section] as Dictionary;
                return sectionData[phrase].ToString();
                
            } else { 
                GD.Print("Error: ", result_json.Error);
                GD.Print("Error Line: ", result_json.ErrorLine);
                GD.Print("Error String: ", result_json.ErrorString);

                return result_json.Error.ToString();
            }
        }
        return fileError.ToString();
    }
}

public enum Language 
{
    Russian,
    English
}