using System.Linq;
using Godot;
using Godot.Collections;

//В C# у Callable нет Bind (в Godot 3 дополнительные аргументы передавались в Connect через binds).
//Подключение с аргументами идёт через этот объект: он вызывает метод target с сохранёнными аргументами.
//Цель, метод и аргументы доступны снаружи, поэтому такие подключения можно сохранять (NpcSaving).
//Поддерживаются только сигналы без собственных аргументов.
public partial class BoundCall : GodotObject
{
    public Node Target { get; private set; }
    public StringName Method { get; private set; }
    public Array Binds { get; private set; }

    public BoundCall() {}

    private BoundCall(Node target, StringName method, Array binds)
    {
        Target = target;
        Method = method;
        Binds = binds ?? [];
    }

    public void Invoke()
    {
        if (IsInstanceValid(Target))
        {
            Target.Callv(Method, Binds);
        }
    }

    public static void Connect(GodotObject source, StringName signal, Node target, StringName method, Array binds, uint flags = 0)
    {
        var boundCall = new BoundCall(target, method, binds);
        source.Connect(signal, new Callable(boundCall, MethodName.Invoke), flags);
    }

    public static bool IsConnected(GodotObject source, StringName signal, Node target, StringName method) =>
        Find(source, signal, target, method) != null;

    public static void Disconnect(GodotObject source, StringName signal, Node target, StringName method)
    {
        var boundCall = Find(source, signal, target, method);
        if (boundCall == null) return;

        source.Disconnect(signal, new Callable(boundCall, MethodName.Invoke));
        boundCall.Free();
    }

    private static BoundCall Find(GodotObject source, StringName signal, Node target, StringName method) =>
        source.GetSignalConnectionList(signal)
            .Select(connection => connection["callable"].AsCallable().Target)
            .OfType<BoundCall>()
            .FirstOrDefault(boundCall => boundCall.Target == target && boundCall.Method == method);
}
