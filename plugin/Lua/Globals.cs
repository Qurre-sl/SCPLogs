using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace SCPLogs.Lua;

public static class Globals
{
    private static readonly HashSet<Type> CachedTypes = [];
    private static readonly Dictionary<string, object> Table = [];

    public static void RegisterType<T>()
    {
        RegisterType(typeof(T));
    }

    public static void RegisterType(Type type)
    {
        if (!CachedTypes.Add(type))
            return;

        UserData.RegisterType(type);
    }

    public static void SetGlobalVariable(string name, object value)
    {
        Table[name] = value;
    }

    public static IReadOnlyDictionary<string, object> GetGlobalVariables()
    {
        return Table;
    }
}