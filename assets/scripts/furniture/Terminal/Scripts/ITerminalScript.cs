//интерфейс для внутриигровых скриптов,  
//запускаемых через терминал
public interface ITerminalScript {
    void initiate(Terminal terminal, string parameter);
}