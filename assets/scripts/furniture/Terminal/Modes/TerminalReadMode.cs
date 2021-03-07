using Godot;
using Godot.Collections;

//режим чтения
//делится на два подрежима - выбор файла и чтение файла
//выбор файла ограничен 9 текстовыми файлами
public class TerminalReadMode: TerminalMode {
    const int MAX_LINES_COUNT = 10;
    private Array<string> textFiles = new Array<string>();
    int tempChoose = 0;
    string fileCode = null;

    public TerminalReadMode(Terminal terminal, string fileName = null): 
    base(terminal) { 
        //добавляем в список только текстовые файлы
        foreach(string tempCode in terminal.files) {
            Dictionary fileData = InterfaceLang.GetPhrasesSection("files", tempCode);
            string fileType = fileData["type"].ToString();
            if (fileType == "text") {
                textFiles.Add(tempCode);

                //проверяем, не был ли этот текстовый файл введен как параметр
                //(в параметрах выводится имя.формат, а сами ссылки хранятся как коды)
                if (fileName != null) {
                    string tempName = fileData["name"].ToString() + "." + fileType;
                    if (tempName == fileName) {
                        this.fileCode = tempCode;
                    }
                }
            }        
        }

        //если файл из параметров не найден, для отображения ошибки его все равно надо добавить
        if (fileName != null && fileCode == null) {
            fileCode = "error";
        }
    }

    public override void LoadMode()
    {
        base.LoadMode();
        UpdateScreen();
    }

    public override void UpdateInput(InputEventKey keyEvent)
    {
        if (keyEvent.Pressed) {
            //обрабатываем кнопку выхода
            if (keyEvent.Scancode == (uint)KeyList.Backspace) {
                //если читается файл, выходим в список файлов
                if (fileCode != null) {
                    fileCode = null;
                } else {
                    //иначе возвращаемся в обычный режим
                    var newMode = new TerminalUsualMode(terminal);
                    terminal.ChangeMode(newMode);
                    return;
                }
            }
            
            //если выводится список файлов
            if (fileCode == null) {
                //обрабатываем стрелочки
                //если нажата стрелочка вверх, перемещаем курсор вверх
                if (keyEvent.Scancode == (uint)KeyList.Up) {
                    if (tempChoose > 0) {
                        tempChoose--;
                    } else {
                        tempChoose = textFiles.Count - 1;
                    }
                }
                //если нажата стрелочка вниз, перемещаем курсор вниз
                if(keyEvent.Scancode == (uint)KeyList.Down) {
                    if (tempChoose < textFiles.Count - 1) {
                        tempChoose++;
                    } else {
                        tempChoose = 0;
                    }
                }

                //обрабатываем enter
                if(keyEvent.Scancode == (uint)KeyList.Enter) {
                    fileCode = textFiles[tempChoose];
                }
            }

            //в конце обновляем экран
            UpdateScreen();
        }
    }

    private void UpdateScreen()
    {
        ClearOutput();
        if (fileCode != null) {
            OpenFile(fileCode);
        } else {
            ShowFilesList();
        }
    }

    private void ShowFilesList()
    {
        textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "chooseFile") + "\n";
        textLabel.Text += "-------------------------------------\n";

        for(int i = 0; i < MAX_LINES_COUNT; i++) {
            if (textFiles.Count > i) {
                string tempCode = textFiles[i];
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
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "chooseHelp2") + "\n";
    }

    private void OpenFile(string fileName)
    {
        Dictionary fileData = InterfaceLang.GetPhrasesSection("files", fileCode);
        if (fileData == null) {
            textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "readError1") + "\n";
            textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "readError2") + "\n";
            return;
        } 

        Array lines = fileData["text"] as Array;

        textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "readHeader") + fileData["name"].ToString() + "\n";
        textLabel.Text += "-------------------------------------\n\n";
        
        for(int i = 0; i < MAX_LINES_COUNT + 1; i++) {
            if (lines.Count > i) {
                textLabel.Text += lines[i] + "\n";
            } else {
                textLabel.Text += "\n";
            }
        }

        textLabel.Text += "-------------------------------------\n";
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "readFooter") + "\n";
    }
}