using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public class LoadMenu : Control
{
    [Signal]
    public delegate void BackPressed();
    
    private MenuBase parentMenu;
    
    private Label pageLabel;

    private Button backButton;
    private Control tableBack;
    private Table table;
    
    private Button loadButton;
    private Button deleteButton;

    private Control modalError;
    private Label modalHeader;
    private Label modalDesc;
    private Button modalOk;
    
    public void UpdateTable()
    {
        table.RedrawAllButtons();
    }

    public void LoadInterfaceLanguage()
    {
        var menuName = "main";
        switch (parentMenu.GetType().ToString())
        {
            case "PauseMenu":
                menuName = "pause";
                break;
            case "DealthMenu":
                menuName = "death";
                break;
        }

        pageLabel.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "page_load_" + menuName);
        backButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "back");
        loadButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "load");
        deleteButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "delete");

        modalHeader.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "header");
        modalDesc.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "desc");
        modalOk.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "ok");
        
        table.HeaderName = InterfaceLang.GetPhrase("saveloadMenu", "table", "name");
        table.HeaderDate = InterfaceLang.GetPhrase("saveloadMenu", "table", "date");
        table.HeaderLevel = InterfaceLang.GetPhrase("saveloadMenu", "table", "level");
        table.UpdateHeader();
    }

    public override void _Ready()
    {
        parentMenu = GetParent<MenuBase>();

        pageLabel = GetNode<Label>("page_label");
        backButton = GetNode<Button>("back");
        tableBack = GetNode<Control>("Table");
        table = tableBack.GetNode<Table>("table");
        loadButton = GetNode<Button>("Load");
        deleteButton = GetNode<Button>("Delete");
        modalError = GetNode<Control>("modalError");
        modalHeader = modalError.GetNode<Label>("back/Header");
        modalDesc = modalError.GetNode<Label>("back/Text");
        modalOk = modalError.GetNode<Button>("back/OK");
        LoadInterfaceLanguage();
        UpdateTable();
    }
    
    public static string GetLastSaveFile()
    {
        if (Global.saveFilesArray.Count == 0) return null;
        
        string lastFileName = null;
        ulong lastTime = 0;

        foreach (FileTableLine tempTableLine in Global.saveFilesArray)
        {
            var fileName = tempTableLine.name;
            var filePath = $"user://saves/{SaveMenu.GetLikeLatinString(fileName)}.sav";
            var tempTime = new File().GetModifiedTime(filePath);
            if (tempTime <= lastTime) continue;
            lastFileName = fileName;
            lastTime = tempTime;
        }

        return lastFileName;
    }

    public override void _Process(float delta)
    {
        if (!(parentMenu is PauseMenu)) return;
        if (!MenuManager.SomeMenuOpen && Input.IsActionJustPressed("ui_quickload"))
        {
            if (Global.GetSaveFiles().Contains("user://saves/quicksave.sav"))
            {
                LoadGame("quicksave");
            }
            else
            {
                Messages messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
                messages.ShowMessage("quicksaveNotFound");
            }
        }
    }
    

    public void _on_back_pressed()
    {
        parentMenu.SoundClick();
        EmitSignal(nameof(BackPressed));
    }

    public void _on_mouse_entered(string section, string messageLink)
    {
        parentMenu._on_mouse_entered(section, messageLink, "saveloadMenu");
    }

    public void _on_mouse_exited()
    {
        parentMenu._on_mouse_exited();
    }
    
    public void _on_TableButtonPressed(string fileName)
    {
        parentMenu.SoundClick();
        loadButton.Disabled = false;
        deleteButton.Disabled = false;
    }

    public void _on_Delete_pressed()
    {
        parentMenu.SoundClick();
        string saveName = table.GetTempFileData.name;
        string fileName = SaveMenu.GetLikeLatinString(saveName)+ ".sav";
        
        Global.DeleteSaveFile(fileName);
        table.DeleteButton(saveName);
        
        loadButton.Disabled = true;
        deleteButton.Disabled = true;
    }

    public void _on_Load_pressed()
    {
        parentMenu.SoundClick();
        LoadGame(table.GetTempFileData.name);
    }

    public static bool TryToLoadGame(string fileName, LevelsLoader loader)
    {
        try
        {
            var saveFile = new File();
            var filePath = $"user://saves/{SaveMenu.GetLikeLatinString(fileName)}.sav";
            saveFile.OpenCompressed(filePath, File.ModeFlags.Read);
            for (int i = 0; i < 2; i++) saveFile.GetLine();
            var levelNum = int.Parse(saveFile.GetLine());

            //для поддержки старых сохранений, в которых не было строчки с названием автосейва
            var checkLine = saveFile.GetLine();
            if (checkLine.BeginsWith(SaveMenu.AUTOSAVE_PREFIX))
            {
                Global.Get().autosaveName = checkLine.Remove(0, SaveMenu.AUTOSAVE_PREFIX.Length);
                Global.Get().playerRace = Global.RaceFromString(saveFile.GetLine());
            }
            else
            {
                Global.Get().autosaveName = "old_autosave";
                Global.Get().playerRace = Global.RaceFromString(checkLine);
            }
            
            var deletedObjects = (Godot.Collections.Array) JSON.Parse(saveFile.GetLine()).Result;
            var levelsData = (Dictionary) JSON.Parse(saveFile.GetLine()).Result;
            saveFile.Close();

            loader.LoadLevel(levelNum, levelsData, deletedObjects);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private void LoadGame(string fileName)
    {
        if (!TryToLoadGame(fileName, GetNode<LevelsLoader>("/root/Main")))
        {
            modalError.Visible = true;
        }
    }
    
    public void _on_Error_OK_pressed()
    {
        modalError.Visible = false;
    }
}
