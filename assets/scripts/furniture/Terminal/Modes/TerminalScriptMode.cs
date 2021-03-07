using Godot;
using Godot.Collections;

//Режим выбора скрипта для запуска
//После выбора запускает обратно режим консоли
public class TerminalScriptMode: TerminalMode {

    const int MAX_LINES_COUNT = 10;

    private Array<string> scriptFiles = new Array<string>();
    int tempChoose = 0;

    public TerminalScriptMode(Terminal terminal): base (terminal) {
        foreach(string tempCode in terminal.files) {
            Dictionary fileData = InterfaceLang.GetPhrasesSection("files", tempCode);
            string fileType = fileData["type"].ToString();
            if (fileType == "script") {
                scriptFiles.Add(tempCode);
            }
        }
    }

    public override void LoadMode()
    {
        base.LoadMode();
        ShowFilesList();
    }

    public override void UpdateInput(InputEventKey keyEvent)
    {
        if (keyEvent.Pressed) {
            //обрабатываем кнопку выхода
            if (keyEvent.Scancode == (uint)KeyList.Backspace) {
                var newMode = new TerminalUsualMode(terminal);
                terminal.ChangeMode(newMode);
                return;
            }
            
            //обрабатываем стрелочки
            //если нажата стрелочка вверх, перемещаем курсор вверх
            if (keyEvent.Scancode == (uint)KeyList.Up) {
                if (tempChoose > 0) {
                    tempChoose--;
                } else {
                    tempChoose = scriptFiles.Count - 1;
                }
            }
            //если нажата стрелочка вниз, перемещаем курсор вниз
            if(keyEvent.Scancode == (uint)KeyList.Down) {
                if (tempChoose < scriptFiles.Count - 1) {
                    tempChoose++;
                } else {
                    tempChoose = 0;
                }
            }

            //обрабатываем enter
            if(keyEvent.Scancode == (uint)KeyList.Enter) {
                InitScript();
                return;
            }

            //в конце обновляем экран
            ClearOutput();
            ShowFilesList();
        }
    }

    private void ShowFilesList()
    {
        textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "chooseFile") + "\n";
        textLabel.Text += "-------------------------------------\n";

        for(int i = 0; i < MAX_LINES_COUNT; i++) {
            if (scriptFiles.Count > i) {
                string tempCode = scriptFiles[i];
                Dictionary fileData = InterfaceLang.GetPhrasesSection("files", tempCode);

                string fileName = fileData["name"].ToString() + "." + fileData["type"].ToString();
                if (i == tempChoose) {
                    textLabel.Text += " >" + fileName + "\n";
                } else {
                    textLabel.Text += "  " + fileName + "\n";
                }
            } else {
                textLabel.Text += "\n";
            }
        }        

        textLabel.Text += "-------------------------------------\n";
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "chooseHelp1") + "\n";
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "scriptHelp") + "\n";
    }

    private async void InitScript()
    {
        //выходим обратно в консоль
        var newMode = new TerminalUsualMode(terminal);
        terminal.ChangeMode(newMode);

        await terminal.ToSignal(terminal.GetTree(), "idle_frame");

        //запускаем скрипт
        ClearOutput();
        string scriptCode = scriptFiles[tempChoose];
        Dictionary fileData = InterfaceLang.GetPhrasesSection("files", scriptCode);
        string scriptName = fileData["script"].ToString();
        terminal.InitiateScript(scriptName, null);
    }
}  