using System;
using System.Collections.Generic;
using HarmonyLib;
using LabApi.Features.Console;
using MoonSharp.Interpreter;

namespace SCPLogs.Configs;

public class LuaConfig
{
    public List<DeclareType> DeclareTypes { get; } = [new(typeof(DateTime).FullName, "System_DateTime")];
    
    public readonly struct DeclareType(string? typeName, string luaName)
    {
        public string? TypeName { get; } = typeName;
        public string LuaName { get; } = luaName;
    }

    internal static void Load(LuaConfig config)
    {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (DeclareType declareType in config.DeclareTypes)
        {
            if (declareType.TypeName is null)
                continue;
            
            Type? type = AccessTools.TypeByName(declareType.TypeName);
            
            if (type is null)
                continue;
            
            Lua.Internal.RegisterLuaType(type);
            
            if (string.IsNullOrEmpty(declareType.LuaName))
                continue;

            try
            {
                Lua.Globals.SetGlobalVariable(declareType.LuaName, UserData.CreateStatic(type));
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to declare class \"{declareType.TypeName}\" in global Lua-space: \n{ex.Message}");
            }
        }
    }
}