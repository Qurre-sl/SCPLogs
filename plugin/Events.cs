using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LabApi.Features.Console;
using LabApi.Loader.Features.Paths;
using MoonSharp.Interpreter;
using SCPLogs.Extensions;
using SCPLogs.Lua;

namespace SCPLogs;

internal static class Events
{
    private static readonly Dictionary<string, LuaEventConfig> EventConfigs = new();
    private static readonly Dictionary<EventInfo, Delegate> RegisteredEvents = new();
    private static DirectoryInfo? _configsDirectory;

    internal static void Load()
    {
        _configsDirectory = PathManager.Configs.CreateSubdirectory("SCPLogs");
        LoadLuaConfigs();
        RegisterAllEvents();
    }

    internal static void Unload()
    {
        foreach (var (eventInfo, handler) in RegisteredEvents)
            try
            {
                eventInfo.RemoveEventHandler(null, handler);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to unregister event {eventInfo.Name}: {ex.Message}");
            }

        RegisteredEvents.Clear();
    }

    private static void LoadLuaConfigs()
    {
        if (_configsDirectory == null || !_configsDirectory.Exists)
        {
            Logger.Warn($"Config directory does not exist: {_configsDirectory?.FullName}");
            _configsDirectory?.Create();
            return;
        }

        foreach (var file in _configsDirectory.GetFiles("*.lua"))
            try
            {
                var eventName = Path.GetFileNameWithoutExtension(file.Name);
                var luaScript = File.ReadAllText(file.FullName);

                var config = ParseLuaConfig(luaScript);
                config.EventName = eventName;
                config.LuaScript = luaScript;

                EventConfigs[eventName] = config;
                Logger.Debug($"Loaded Lua config for event: {eventName}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load Lua config from {file.Name}: {ex.Message}");
            }
    }

    private static LuaEventConfig ParseLuaConfig(string luaScript)
    {
        var config = new LuaEventConfig { Enabled = true, Channels = [] };

        var lines = luaScript.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("--")) break;

            if (trimmed.Contains("@enabled"))
            {
                config.Enabled = trimmed.Contains("true", StringComparison.OrdinalIgnoreCase);
            }
            else if (trimmed.Contains("@channels"))
            {
                var channelsStr = trimmed.Substring(trimmed.IndexOf("@channels") + 9).Trim();
                config.Channels = channelsStr.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c))
                    .ToArray();
            }
        }

        return config;
    }

    private static void RegisterAllEvents()
    {
        var labApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "LabApi");

        if (labApiAssembly == null)
        {
            Logger.Error("LabApi assembly not found");
            return;
        }

        var eventHandlerTypes = labApiAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace == "LabApi.Events.Handlers" && t.IsClass && t.IsAbstract &&
                        t.IsSealed);

        foreach (var eventHandlerType in eventHandlerTypes)
        {
            var events = eventHandlerType.GetEvents(BindingFlags.Public | BindingFlags.Static);
            var classPrefix = eventHandlerType.Name;

            foreach (var eventInfo in events)
            {
                if (eventInfo.EventHandlerType == null)
                    continue;

                if (!eventInfo.EventHandlerType.Name.StartsWith("LabEventHandler"))
                    continue;

                var eventName = $"{classPrefix}.{eventInfo.Name}";

                RegisterEvent(eventInfo, eventName);
            }
        }
    }

    private static void RegisterEvent(EventInfo eventInfo, string eventName)
    {
        if (!EventConfigs.TryGetValue(eventName, out var config) || !config.Enabled || config.Channels.Length == 0)
            return;

        if (Main.Instance.Config?.DontSendEvents?.Contains(eventName) == true)
        {
            Logger.Debug($"Event {eventName} is in DontSendEvents list, skipping");
            return;
        }

        try
        {
            var eventHandlerType = eventInfo.EventHandlerType;
            var invokeMethod = eventHandlerType?.GetMethod("Invoke");

            if (invokeMethod == null)
                return;

            var parameters = invokeMethod.GetParameters();

            var handler = parameters.Length switch
            {
                0 => new Action(() => ExecuteLuaEvent(eventName, config, new { })),
                1 => CreateTypedHandler(parameters[0].ParameterType, eventName, config),
                _ => null
            };

            if (handler == null)
                return;

            eventInfo.AddEventHandler(null, handler);
            RegisteredEvents[eventInfo] = handler;

            Logger.Debug($"Registered event: {eventName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to register event {eventName}: {ex.Message}");
        }
    }

    private static Delegate? CreateTypedHandler(Type argsType, string eventName, LuaEventConfig config)
    {
        var actionType = typeof(Action<>).MakeGenericType(argsType);

        var method =
            typeof(Events).GetMethod(nameof(ExecuteLuaEventGeneric), BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
            return null;

        var genericMethod = method.MakeGenericMethod(argsType);

        object[] args = [eventName, config];
        var handler = Delegate.CreateDelegate(actionType, args, genericMethod);

        return handler;
    }

    private static void ExecuteLuaEventGeneric<T>(string eventName, LuaEventConfig config, T eventArgs)
    {
        ExecuteLuaEvent(eventName, config, eventArgs);
    }

    private static void ExecuteLuaEvent(string eventName, LuaEventConfig config, object eventArgs)
    {
        try
        {
            Script luaScript = new();
            Internal.PrepareTable(luaScript.Globals);

            var argsType = eventArgs.GetType();

            Internal.PreRegisterLuaType(argsType);

            var properties = argsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
                try
                {
                    Internal.RegisterLuaType(property.PropertyType);
                    luaScript.Globals[property.Name] = property.GetValue(eventArgs);

                    if (property.PropertyType is { IsSealed: true, IsEnum: true })
                        luaScript.Globals["Enum_" + property.PropertyType.Name] =
                            UserData.CreateStatic(property.PropertyType);
                }
                catch
                {
                    // Skip
                }

            var sendLog = (string message, string[]? channels = null) =>
                EventsExtensions.SendLog(message, channels ?? config.Channels);

            luaScript.Globals["SendLog"] = sendLog;
            luaScript.Globals["PrintTime"] = (object)EventsExtensions.GetTime;
            luaScript.Globals["PrintPlayer"] = (object)EventsExtensions.PrintPlayer;
            luaScript.Globals["IsOneFraction"] = (object)EventsExtensions.IsOneFraction;

            luaScript.DoString(config.LuaScript);
            var reply = luaScript.Globals.Get("reply");

            if (!reply.IsNil())
            {
                var message = reply.Type == DataType.String ? reply.String : reply.ToString();
                sendLog(message);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error executing Lua script for {eventName}: {ex.Message}");
        }
    }

    private class LuaEventConfig
    {
        public string EventName { get; set; } = string.Empty;
        public string LuaScript { get; set; } = string.Empty;
        public string[] Channels { get; set; } = [];
        public bool Enabled { get; set; }
    }
}