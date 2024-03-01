using Godot;

//Выводит список дверей (копия ReadMode)
//Открывает/Закрывает выбранные двери
public partial class TerminalDoorsMode: TerminalMode
{
    private const int MAX_LINES_COUNT = 10;
    private int tempChoose;

    public TerminalDoorsMode(Terminal terminal): base(terminal)  {}

    private static void ChangeDoorKey(FurnDoor door)
    {
        if (door.myKey != "")
        {
            door.KeyToRemember = door.myKey;
            door.myKey = "";
        }
        else if (!door.IsOpen)
        {
            door.myKey = door.KeyToRemember ?? "keyToAllDoors";
        }
    }
    
    public override void LoadMode()
    {
        base.LoadMode();
        UpdateScreen();
    }

    public override void UpdateInput(InputEventKey keyEvent)
    {
        if (keyEvent.Pressed) 
        {
            //обрабатываем кнопку выхода
            if (keyEvent.Keycode == Key.Backspace) 
            {
                var newMode = new TerminalUsualMode(terminal);
                terminal.ChangeMode(newMode);
                return;
            }
            
            //обрабатываем стрелочки
            //если нажата стрелочка вверх, перемещаем курсор вверх
            if (keyEvent.Keycode == Key.Up) 
            {
                if (tempChoose > 0) {
                    tempChoose--;
                } else {
                    tempChoose = terminal.doors.Count - 1;
                }
            }
            //если нажата стрелочка вниз, перемещаем курсор вниз
            if (keyEvent.Keycode == Key.Down) 
            {
                if (tempChoose < terminal.doors.Count - 1) {
                    tempChoose++;
                } else {
                    tempChoose = 0;
                }
            }

            //обрабатываем enter
            if(keyEvent.Keycode == Key.Enter) 
            {
                ChangeDoorKey(terminal.doors[tempChoose]);
            }

            //в конце обновляем экран
            UpdateScreen();
        }
    }
    
    private void UpdateScreen()
    {
        ClearOutput();
        ShowDoorsList();
    }
    
    private void ShowDoorsList()
    {
        textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "chooseDoor")  + "\n";
        textLabel.Text += "------------------------------------\n";

        for(int i = 0; i < MAX_LINES_COUNT - 1; i++) 
        {
            if (terminal.doors.Count > i)
            {
                var doorCode =  terminal.doors[i].doorCode;
                if (string.IsNullOrEmpty(doorCode)) continue;

                doorCode = InterfaceLang.GetPhrase("terminal", "doorCodes", doorCode);
                var chooseString = i == tempChoose ? " >" : "  ";
                var openString = string.IsNullOrEmpty(terminal.doors[i].myKey) 
                    ? InterfaceLang.GetPhrase("terminal", "door", "opened")
                    : InterfaceLang.GetPhrase("terminal", "door", "closed");
                textLabel.Text += chooseString + doorCode + openString + "\n";
            } 
            else 
            {
                textLabel.Text += "\n";
            }
        }        

        textLabel.Text += "------------------------------------\n";
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "chooseHelp") + "\n";
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "readFooter");
    }
}