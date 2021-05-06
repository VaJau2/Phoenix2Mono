using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

public class Table : VBoxContainer
{
    private const int SYMBOL_PIXEL_SIZE = 8;
    private static List<FileTableLine> filesArray => Global.saveFilesArray;

    public string HeaderName = "название";
    public string HeaderDate = "дата";
    public string HeaderLevel = "уровень";

    private Theme buttonsTheme;
    private Label Header, HeaderBorders;
    private TableSignals parent;

    private int buttonsCount = 0;
    public Button tempButton { get; private set; }

    private int allSymbolsSize,
                firstColumnsSize, 
                secondColumnSize = 12;

    public void UpdateHeader()
    {
        Header.Text = GetLine(HeaderName, HeaderDate, HeaderLevel);
    }

    public void SpawnButtons()
    {
        foreach (var fileData in filesArray)
        {
            SpawnButton(fileData);
        }
    }
    
    public Button SpawnButton(FileTableLine fileData)
    {
        var newButton = new Button {Name = buttonsCount.ToString()};
        newButton.Connect("pressed", this, nameof(_on_table_button_click), new Godot.Collections.Array() {newButton.Name});
        newButton.ToggleMode = true;
        newButton.Align = Button.TextAlign.Left;
        newButton.Theme = buttonsTheme;
        newButton.AddToGroup("ignore_color");
        newButton.Text = GetLine(fileData.name, fileData.date, 
            InterfaceLang.GetPhrase("levels", "levelNames",fileData.level));
        AddChild(newButton);
        buttonsCount++;
        return newButton;
    }

    public void UpdateButton(FileTableLine fileData)
    {
        Button foundButton = FindButton(fileData.name);
        if (foundButton != null)
        {
            foundButton.Text = GetLine(fileData.name, fileData.date, 
                InterfaceLang.GetPhrase("levels", "levelNames",fileData.level));
        }
    }

    //находит первую кнопку в таблице по названию сохранения и выделяет её
    public bool LineExists(string saveName)
    {
        Button foundButton = FindButton(saveName);
        if (foundButton != null)
        {
            if (foundButton == tempButton) return true;
            foundButton.Pressed = true;
            if (tempButton != null) tempButton.Pressed = false;
            tempButton = foundButton;
            return true;
        }

        if (tempButton == null) return false;
        tempButton.Pressed = false;
        tempButton = null;
        return false;
    }
    
    public void DeleteButton(string saveName)
    {
        foreach (var tempLine in filesArray.Where(tempLine => tempLine.name == saveName))
        {
            filesArray.Remove(tempLine);
            break;
        }

        RedrawAllButtons();
    }

    public async void RedrawAllButtons()
    {
        for (int i = 0; i < buttonsCount; i++)
        {
            Button deletedButton = GetNode<Button>(i.ToString());
            RemoveChild(deletedButton);
            deletedButton.QueueFree();
        }
        buttonsCount = 0;

        SpawnButtons();
    }

    public FileTableLine GetTempFileData => filesArray[Convert.ToInt32(tempButton.Name)];

    private Button FindButton(string saveName)
    {
        for (var i = 0; i < filesArray.Count; i++)
        {
            if (filesArray[i].name != saveName) continue;

            var newTempButton = GetNode<Button>(i.ToString());
            return newTempButton;
        }

        return null;
    }
    
    
    //добавляет определенное количество одинаковых символов в строку
    private static void AddSymbolsCount(ref string line, char symbol, int count) 
    {
        for(var i = 0; i < count; i++) {
            line += symbol;
        }
    }

    //добавляет надпись в ячейку и выравнивает её посередине
    private static void AddColumn(ref string result, string line, int columnSize, char space = ' ')
    {
        if (line.Length < columnSize) {
            var spacesCount = (columnSize - line.Length) / 2;

            AddSymbolsCount(ref result, space, spacesCount);
            result += line;
            AddSymbolsCount(ref result, space, spacesCount);

            if (result.Length < columnSize) {
                AddSymbolsCount(ref result, space, columnSize - result.Length);
            }
        } else {
            result += line.Substr(0, columnSize);
        }
    }

    //объединяет ячейки в строку
    private string GetLine(string firstColumn, string secondColumn, string thirdColumn, 
                           char space = ' ', char separator = '|')
    {
        var result = "";

        AddColumn(ref result, firstColumn, firstColumnsSize, space);
        result += separator;
        AddColumn(ref result, secondColumn, secondColumnSize, space);
        result += separator;
        AddColumn(ref result, thirdColumn, firstColumnsSize, space);

        return result;
    }

    public override void _Ready()
    {
        Header        = GetNode<Label>("Header");
        HeaderBorders = GetNode<Label>("HeaderBorders");
        parent = GetParent<TableSignals>();
        buttonsTheme = GD.Load<Theme>("res://assets/materials/fonts/terminal_theme.tres");

        allSymbolsSize = (int)((RectSize.x - 10) / SYMBOL_PIXEL_SIZE);
        firstColumnsSize = (allSymbolsSize - secondColumnSize) / 2;

        UpdateHeader();
        HeaderBorders.Text = GetLine("", "", "", '-', '+');
    }

    public void _on_table_button_click(string buttonName)
    {
        if (tempButton != null) {
            tempButton.Pressed = false;
        }

        tempButton = GetNodeOrNull<Button>(buttonName);
        if (tempButton == null) return;
        
        var fileI = int.Parse(buttonName);
        parent.EmitSignal(nameof(TableSignals.TableButtonPressed), filesArray[fileI].name);
    }
}

public readonly struct FileTableLine
{
    public readonly string name;
    public readonly string date;
    public readonly string level;

    public FileTableLine(string name, string date, string level) 
    {
        this.name = name;
        this.date = date;
        this.level = level;
    }
}