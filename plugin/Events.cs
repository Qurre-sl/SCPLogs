using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Qurre.API;
using Qurre.Events.Structs;
using SCPLogs.Configs;
using MoonSharp.Interpreter;
using SCPLogs.Extensions;

namespace SCPLogs;

internal static class Events
{
    internal static void Load()
    {
        Type iBaseType = typeof(IBaseEvent);
        Assembly assembly = Assembly.GetAssembly(iBaseType);

        JToken config = GetTranslations();

        foreach (Type type in assembly.GetTypes())
            ProcessType(type, iBaseType, config);
    }

    private static void ProcessType(Type type, Type iBaseType, JToken config)
    {
        Lua.Internal.PreRegisterLuaType(type);

        if (type.Namespace != iBaseType.Namespace || !ImplementsInterface(type, iBaseType))
            return;

        FieldInfo? fieldInfo = type.GetField("EventID", BindingFlags.NonPublic | BindingFlags.Static);

        if (fieldInfo?.GetValue(null) is not uint eventId)
        {
            Log.Error($"EventID in '{type.FullName}' not found");
            return;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
            Lua.Internal.RegisterLuaType(property.PropertyType);

        Translate translation = Main.Config.SafeGetValue(type.Name, new Translate(
            properties.Aggregate("Available arguments: ",
                (current, property) => current + $"{{{property.Name}}} ({property.PropertyType}); ").Trim(),
            string.Empty, []
        ), source: config);

        if (!translation.Enabled || string.IsNullOrEmpty(translation.LuaScript) || translation.Channels.Length == 0)
            return;

        Dictionary<string, object> luaEnums = [];

        foreach (PropertyInfo property in properties)
        {
            Type typeOfProperty = property.PropertyType;
            
            if (typeOfProperty is not { IsSealed: true, IsEnum: true })
                continue;
            
            if (typeOfProperty.Namespace?.StartsWith("Qurre.API.Objects") ?? true)
                continue;

            luaEnums["Enum_" + typeOfProperty.Name] = UserData.CreateStatic(typeOfProperty);
        }

        PropertyInfo? propertyAllowed = type.GetProperty("Allowed", BindingFlags.Public | BindingFlags.Instance);
        var sendLog = (string message, string[]? channels = null) => EventsExtensions.SendLog(message, channels ?? translation.Channels);

        Core.InjectAction(eventId, int.MinValue, @event =>
        {
            if (propertyAllowed?.GetValue(@event) is false)
                return;

            Script luaScript = new();
            Lua.Internal.PrepareTable(luaScript.Globals);

            foreach (PropertyInfo property in properties)
                luaScript.Globals[property.Name] = property.GetValue(@event);

            foreach (var luaEnum in luaEnums)
                luaScript.Globals[luaEnum.Key] = luaEnum.Value;

            luaScript.Globals["SendLog"] = sendLog;
            luaScript.Globals["PrintPlayer"] = (object)EventsExtensions.PrintPlayer;

            luaScript.DoString(translation.LuaScript);
            DynValue reply = luaScript.Globals.Get("reply");

            if (reply.IsNil())
                return;

            sendLog(reply.Type == DataType.String ? reply.String : reply.ToString());
            Log.Debug(type.Name);
        });

    }

    private static bool ImplementsInterface(Type type, Type interfaceType)
    {
        return type != interfaceType && interfaceType.IsInterface && interfaceType.IsAssignableFrom(type);
    }

    private static JToken GetTranslations()
    {
        JToken? par = Main.Config.JsonArray["Translations"];

        if (par is not null)
            return par;

        par = JObject.Parse("{ }");
        Main.Config.JsonArray["Translations"] = par;

        return par;
    }
}