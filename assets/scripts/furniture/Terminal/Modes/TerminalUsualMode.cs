using System.Linq;
using Godot;
using Godot.Collections;

//режим консоли
//управляется через ввод команд в консоль
public class TerminalUsualMode: TerminalMode {
    
    bool shiftKeyPressed = false;
    string tempCommand;

    public TerminalUsualMode(Terminal terminal): base(terminal) { }

    public override void LoadMode()
    {
        base.LoadMode();
        string startPhrase = InterfaceLang.GetPhrase("terminal", "phrases", terminal.startPhraseCode);
        ShowMessage(startPhrase);
    }

    private void AddLetterToCommand(string letter)
    {
        tempCommand += letter;
        textLabel.Text += letter;
    }

    private void RemoveLetterFromCommand()
    {
        if (!string.IsNullOrEmpty(tempCommand)) {
            tempCommand    = tempCommand.Remove(tempCommand.Length - 1);
            textLabel.Text = textLabel.Text.Remove(textLabel.Text.Length - 1);
        }
    }

    private void EnableCommand()
    {
        textLabel.Text += "\n";
        if (tempCommand != null) {
            string[] commands = tempCommand.Split(' ');
            string command = commands[0];
            string[] properties = null;

            if (commands.Length > 1) {
                properties = new string[commands.Length - 1];
                for(int i = 1; i < commands.Length; i++) {
                    properties[i - 1] = commands[i];
                }
            }
            
            ProcessCommand(command, properties);
            tempCommand = "";
        } else {
            textLabel.Text += terminal.startCommand;
        }
    }

    private void ProcessCommand(string command, string[] properties = null)
    {
        string answer = null;

        switch (command) {
            case "clear":
                ClearOutput();
                textLabel.Text += terminal.startCommand;
                return;

            case "help":
                if (properties == null) {
                    //выводим список команд
                    Array answers = InterfaceLang.GetPhrasesAsArray("terminal", "helpAnswer");
                    foreach(string temp in answers) {
                        answer += temp + "\n";
                    }
                } else {
                    //ищем написанную команду
                    string commandName = properties[0];
                    Dictionary section = InterfaceLang.GetPhrasesSection("terminal", "helpCommands");
                    if (section.Contains(commandName)) {
                        //если команда найдена, выводим помощь по ней
                        Array commandHelp = section[commandName] as Array;
                        foreach(string temp in commandHelp) {
                            answer += temp + "\n";
                        }
                    } else {
                        //если команда не найдена
                        answer = InterfaceLang.GetPhrase("terminal", "phrases", "commandNotFound");
                        answer = answer.Replace("#command#", commandName);
                    }
                }
                break;

            case "dir":
                answer = GetFilesList();
                break;

            case "read":
                string fileName = null;
                if (properties != null) {
                    fileName = properties[0];
                } 
                var readMode = new TerminalReadMode(terminal, fileName);
                terminal.ChangeMode(readMode);
                break;
            
            case "doors":
                if (terminal.doors.Count > 0)
                {
                    var doorsMode = new TerminalDoorsMode(terminal);
                    terminal.ChangeMode(doorsMode);
                }
                else
                {
                    answer = InterfaceLang.GetPhrase("terminal", "door", "isEmpty");
                }
                break;
            
            default:
                answer = InterfaceLang.GetPhrase("terminal", "phrases", "commandNotFound");
                answer = answer.Replace("#command#", command);
                break;
        }
        if (answer != null) {
            ShowMessage(answer);
        }
        
    }

    private string GetFilesList()
    {
        string result = "";
        return terminal.files == null 
            ? InterfaceLang.GetPhrase("terminal", "dir", "isEmpty") 
            : terminal.files.Select(fileCode => InterfaceLang.GetPhrasesSection("files", fileCode))
                .Select(fileData => fileData["name"].ToString())
                .Aggregate(result, (current, fileName) => current + (" - " + fileName + "\n"));
    }
   
    private bool IsLetterKey(uint keyCode)
    {
        return (keyCode >= 48 && keyCode <= 57) || (keyCode >= 65 && keyCode <= 90);
    }

    public override void UpdateInput(InputEventKey keyEvent)
    {
        if (keyEvent.Scancode == (uint)KeyList.Shift) {
            shiftKeyPressed = keyEvent.Pressed;
        }
        else if(keyEvent.Pressed) {
            if(keyEvent.Scancode == (uint)KeyList.Enter) {
                EnableCommand();
            }
            else if(keyEvent.Scancode == (uint)KeyList.Backspace) {
                RemoveLetterFromCommand();
            }
            else if(keyEvent.Scancode == (uint)KeyList.Space) {
                AddLetterToCommand(" ");
            }
            else if(keyEvent.Scancode == (uint)KeyList.Slash
                    || keyEvent.Scancode == (uint)KeyList.Period) {
                AddLetterToCommand(".");
            }
            else if (IsLetterKey(keyEvent.Scancode)) {
                if (startKeyPressed) {
                    startKeyPressed = false;
                } else {
                    string newKey = OS.GetScancodeString(keyEvent.Scancode);
                    AddLetterToCommand(shiftKeyPressed ? newKey : newKey.ToLower());
                }
            }
        }  
    }
}