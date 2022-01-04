using Godot;
using Godot.Collections;

//режим чтения
//делится на два подрежима - выбор файла и чтение файла
//выбор файла ограничен 9 текстовыми файлами
public class TerminalReadMode: TerminalMode {
    const int MAX_LINES_COUNT = 10;
    private const int MAX_LINE_LENGTH = 36;
    private Array<string> textFiles = new Array<string>();
    int tempChoose = 0;
    string fileCode = null;
    string fileName = null;
    
    Array lines = new Array();
    int tempPage = 0;
    int pagesMax = 0;

    public TerminalReadMode(Terminal terminal, string fileName = null): 
    base(terminal) { 
        //добавляем в список только текстовые файлы
        if (terminal.files != null)
        {
            foreach (string tempCode in terminal.files)
            {
                Dictionary fileData = InterfaceLang.GetPhrasesSection("files", tempCode);
                textFiles.Add(tempCode);

                if (fileName == null) continue;
                string tempName = fileData["name"].ToString();
                if (tempName != fileName) continue;
                this.fileCode = tempCode;
                this.fileName = tempName;
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
                    lines.Clear();
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
                if(keyEvent.Scancode == (uint)KeyList.Enter && textFiles.Count > 0) {
                    fileCode = textFiles[tempChoose];
                }
            } else {
                if (keyEvent.Scancode == (uint)KeyList.Right) {
                    TurnPage(true);
                }
                if (keyEvent.Scancode == (uint)KeyList.Left) {
                    TurnPage(false);
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
            if (lines.Count == 0) {
                OpenFile(fileCode);
            } else {
                UpdateFileText();
            }
        } else {
            ShowFilesList();
        }
    }

    private void ShowFilesList()
    {
        textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "chooseFile") + "\n";
        textLabel.Text += "------------------------------------\n";

        for(int i = 0; i < MAX_LINES_COUNT - 1; i++) {
            if (textFiles.Count > i) {
                string tempCode = textFiles[i];
                Dictionary fileData = InterfaceLang.GetPhrasesSection("files", tempCode);
                string fileName = fileData["name"].ToString();
                if (i == tempChoose) {
                    textLabel.Text += " >" + fileName + "\n";
                } else {
                    textLabel.Text += "  " + fileName + "\n";
                }
            } else {
                textLabel.Text += "\n";
            }
        }        

        textLabel.Text += "------------------------------------\n";
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "chooseHelp") + "\n";
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "readFooter");
    }

    private void OpenFile(string fileName)
    {
        Dictionary fileData = InterfaceLang.GetPhrasesSection("files", fileCode);
        if (fileData == null) {
            textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "readError1") + "\n";
            textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "readError2") + "\n";
            return;
        } 
        this.fileName = fileData["name"].ToString();

        Array textLines = fileData["text"] as Array;
        lines = Global.ClumpLineLength(textLines, MAX_LINE_LENGTH);
        
        //считаем данные для постранички
        if (lines.Count > MAX_LINES_COUNT) {
            pagesMax = Mathf.CeilToInt(lines.Count / MAX_LINES_COUNT);
            tempPage = 0;
        } else {
            pagesMax = 0;
        }

        UpdateFileText();
    }

    private void UpdateFileText()
    {
        textLabel.Text = InterfaceLang.GetPhrase("terminal", "phrases", "readHeader") + fileName + "\n";
        textLabel.Text += "------------------------------------\n";
        
        int firstLine = tempPage * MAX_LINES_COUNT;
        int lastLine = firstLine + MAX_LINES_COUNT;

        for(int i = firstLine; i < lastLine; i++) {
            if (lines.Count > i) {
                textLabel.Text += lines[i] + "\n";
            } else {
                textLabel.Text += "\n";
            }
        }

        textLabel.Text += "------------------------------------\n";
        if (pagesMax > 0) {
            textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "readPage1")
                              + (tempPage + 1) + 
                              InterfaceLang.GetPhrase("terminal", "phrases", "readPage2")
                              + (pagesMax + 1) + " (";
            
            textLabel.Text += (tempPage > 0) ? "<" : " ";
            textLabel.Text += "-";
            textLabel.Text += (tempPage < pagesMax) ? ">" : " ";
            textLabel.Text += ")\n";
        } else {
            textLabel.Text += "\n";
        }
        
        textLabel.Text += InterfaceLang.GetPhrase("terminal", "phrases", "readFooter");
    }

    private void TurnPage(bool forward)
    {
        if (forward) {
            if (tempPage < pagesMax) {
                tempPage++;
            }
        } else {
            if (tempPage > 0) {
                tempPage--;
            }
        }
    }
}