# Конфигурация событий SCPLogs

Все .lua файлы должны находиться в `AppData/SCP Secret Laboratory/LabAPI/configs/SCPLogs/`

## Формат .lua файла

```lua
-- @enabled true
-- @channels 123456789,987654321
--
-- Комментарии с описанием доступных значений...

reply = "Ваш текст сообщения"
```

### Параметры в комментариях

- `@enabled true/false` - включено ли событие
- `@channels id1,id2,id3` - список ID каналов Discord/Telegram через запятую

### Доступные глобальные функции

- `SendLog(message, channels?)` - отправить лог в указанные каналы (или дефолтные)
- `PrintTime()` - получить текущее время в формате Discord timestamp
- `PrintPlayer(player, printRole?)` - форматировать информацию об игроке
- `IsOneFraction(player1, player2)` - проверить принадлежность к одной фракции

### Переменная reply

Результат выполнения lua скрипта должен быть записан в переменную `reply`:

- Если `reply` не nil, её содержимое отправится в каналы
- Можно вызвать `SendLog()` напрямую вместо использования `reply`

## Именование файлов

Формат: `{ClassName}.{EventName}.lua`

Примеры из `LabApi.Events.Handlers.PlayerEvents`:

- `public static event LabEventHandler<PlayerJoinedEventArgs>? Joined;` → файл `PlayerEvents.Joined.lua`
- `public static event LabEventHandler<PlayerDeathEventArgs>? Death;` → файл `PlayerEvents.Death.lua`

Примеры из `LabApi.Events.Handlers.ServerEvents`:

- `public static event LabEventHandler? RoundStarted;` → файл `ServerEvents.RoundStarted.lua`

Примеры из `LabApi.Events.Handlers.WarheadEvents`:

- `public static event LabEventHandler<WarheadStartingEventArgs>? Starting;` → файл `WarheadEvents.Starting.lua`

Список всех событий: смотри поля в `LabApi.Events.Handlers.*` классах (PlayerEvents, ServerEvents, WarheadEvents, и
т.д.)

## Примеры

Смотрите примеры конфигов в этой папке:

- `PlayerEvents.Joined.lua` - присоединение игрока (PlayerEvents.Joined)
- `PlayerEvents.Death.lua` - смерть игрока с проверкой убийцы (PlayerEvents.Death)
- `ServerEvents.RoundStarted.lua` - начало раунда без параметров (ServerEvents.RoundStarted)

## Доступные типы из LabAPI

В lua скриптах доступны все свойства EventArgs классов из LabAPI:

- `Player` - обертка игрока с методами и свойствами
- Enum'ы регистрируются автоматически с префиксом `Enum_` (например `Enum_DamageType`)
- Все public свойства EventArgs доступны как глобальные переменные
