using Godot;

public class Terminal_TestScript: ITerminalScript {

    public void initiate(Terminal terminal, string parameter) {
        terminal.mode.ShowMessage("Тестовый скрипт запустился. Йей!");
    }
}