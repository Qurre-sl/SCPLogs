using System;
using System.Reflection;
using LabApi.Features.Console;
using MoonSharp.Interpreter;

namespace SCPLogs.Lua;

internal static class Internal
{
    internal static void PreRegisterLuaType(Type type)
    {
        if (type.Namespace is null)
            return;

        if (!(type.Namespace == "LabApi.Features" ||
              type.Namespace.StartsWith("LabApi.Features.Wrappers") ||
              type.Namespace.StartsWith("LabApi.Events.Arguments")))
            return;

        RegisterLuaType(type);

        if (type is not { IsSealed: true, IsPublic: true })
            return;

        RegisterEnums(type);

        string prefix;

        if (type.IsAbstract)
            prefix = "API";
        else if (type.IsEnum)
            prefix = "API_Enum";
        else
            return;

        string name = prefix + "_" + type.Name;
        Globals.SetGlobalVariable(name, UserData.CreateStatic(type));
        Logger.Debug($"Registered in Lua global space: {name}, Type: {type.FullName}");
    }

    internal static void RegisterLuaType(Type type)
    {
        try
        {
            PropertyInfo? propertyBase = type.GetProperty("Base",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyBase is not null)
                Globals.RegisterType(propertyBase.PropertyType);

            Globals.RegisterType(type);
        }
        catch (Exception ex)
        {
            Logger.Warn(type + "\n" + ex.Message);
        }
    }

    internal static void PrepareTable(Table table)
    {
        foreach (var variable in Globals.GetGlobalVariables())
        {
            table[variable.Key] = variable.Value;
        }
    }

    private static void RegisterEnums(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (property.PropertyType is not { IsSealed: true, IsEnum: true })
                continue;

            Globals.RegisterType(property.PropertyType);
        }
    }
}