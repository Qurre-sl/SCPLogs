<p align="center">
   <a href="https://discord.gg/zGUqfJQebn">
      <img src="https://discord.com/api/guilds/779412392651653130/embed.png" alt="Discord"/>
   </a>
   <a href="https://github.com/Qurre-sl/SCPLogs/releases">
      <img src="https://img.shields.io/github/downloads/Qurre-sl/SCPLogs/total?color=%2300b813&style=plastic" alt="Downloads"/>
   </a>
   <a href="https://github.com/Qurre-sl/SCPLogs/releases">
      <img src="https://img.shields.io/github/v/release/Qurre-sl/SCPLogs.svg?style=plastic" alt="Release"/>
   </a>
</p>

<h1 align="center">SCPLogs v3</h1>
<p align="center">
<img src="https://readme-typing-svg.herokuapp.com/?font=Fira+Code&pause=1000&color=3FF781&center=true&vCenter=true&width=435&lines=LabAPI+%2B+Deno;4+%D0%BF%D1%80%D0%BE%D1%82%D0%BE%D0%BA%D0%BE%D0%BB%D0%B0+%D1%81%D0%B2%D1%8F%D0%B7%D0%B8;%D0%9F%D0%BE%D0%BB%D0%BD%D0%B0%D1%8F+%D0%BA%D0%B0%D1%81%D1%82%D0%BE%D0%BC%D0%B8%D0%B7%D0%B0%D1%86%D0%B8%D1%8F+Lua">
</p>

## Описание

Система отправки игровых событий SCP:SL в Discord/Telegram/WebHook с максимальной кастомизацией через Lua скрипты.

**Основные фичи:**
- 🔥 Каждое событие — отдельный `.lua` файл
- ⚡ Автоматическая регистрация всех событий LabAPI
- 🌐 4 протокола связи: HTTP, TCP, UDP, WebSocket
- 🔄 Авто-реконнект при обрыве
- 🎯 Discord, Telegram, WebHook
- 🐳 Docker образы для клиента

---

## 📚 Документация

<p align="center">
  <a href="plugin/ExampleConfigs/README.md">
    <img src="https://img.shields.io/badge/📝_Примеры_Lua_конфигов-00b813?style=for-the-badge" alt="Lua Examples"/>
  </a>
</p>

---

## Архитектура

```
Plugin (C# + LabAPI)  ←→  Client (Deno + TypeScript)  ←→  Discord/Telegram/WebHook
      [Events]            [Socket Server]                  [Bot API]
```

### Plugin
- **Фреймворк:** LabAPI
- **Язык:** C# (.NET Standard 2.1)
- **Задачи:** Собирает события игры, выполняет Lua скрипты, отправляет через сокеты

### Client
- **Runtime:** Deno 2.x
- **Язык:** TypeScript
- **Задачи:** Принимает события от плагина, отправляет в Discord/Telegram

---

## Установка

### 1. Плагин

```bash
# Скачать из релизов
wget https://github.com/Qurre-sl/SCPLogs/releases/latest/download/SCPLogs.dll

# Или скомпилировать
cd plugin
dotnet build -c Release
```

Поместить `SCPLogs.dll` в `AppData/SCP Secret Laboratory/LabAPI/plugins/`

### 2. Конфигурация плагина

После первого запуска создастся `LabAPI/configs/scplogs.yml`:

```yaml
ip: 127.0.0.1
port: 8080
protocol: udp # http | tcp | udp | websocket
clientToken: СМЕНИ_МЕНЯ_НА_РАНДОМНЫЙ_ТОКЕН
badgeOnline: "reply = string.format(\"%s/%s players\", Count, Slots)"
dontSendEvents: []
luaConfig:
  declareTypes:
    - typeName: System.DateTime
      luaName: System_DateTime
```

### 3. Lua события

Создать папку `LabAPI/configs/SCPLogs/` и добавить `.lua` файлы для нужных событий.

**Формат имени:** `{ClassName}.{EventName}.lua`

Примеры в `/plugin/ExampleConfigs/` — скопировать оттуда и настроить.

### 4. Client

```bash
# Установить Deno (если нет)
curl -fsSL https://deno.land/install.sh | sh

cd client
cp env.example .env
nano .env  # настроить токены и каналы
```

**Настройка `.env`:**

```bash
# === Sender (куда отправлять) ===
SENDER_TYPE=discord # discord | telegram | webhook
SENDER_TIMEOUT_SECONDS=60

# Discord
DISCORD_TOKEN=твой_токен_бота
DISCORD_COMMAND_GUILDS=123456789,987654321
DISCORD_ALLOWED_CHANNELS=123456789,987654321

# === Socket (откуда принимать) ===
SOCKET_TYPE=udp # http | tcp | udp | websocket
SOCKET_HOST=127.0.0.1
SOCKET_PORT=8080
SOCKET_TOKEN=ТАКОЙ_ЖЕ_КАК_В_scplogs.yml
```

**Запуск:**

```bash
deno run --allow-all src/main.ts
```

### 5. Client (Docker) 🐳

**Pull & Run:**

```bash
docker pull ghcr.io/qurre-sl/scplogs:latest

docker run -d \
  --name scplogs \
  --restart unless-stopped \
  -p 8080:8080 \
  -e SENDER_TYPE=discord \
  -e SENDER_TIMEOUT_SECONDS=60 \
  -e DISCORD_TOKEN=your_token \
  -e DISCORD_COMMAND_GUILDS=123,456 \
  -e DISCORD_ALLOWED_CHANNELS=789,012 \
  -e SOCKET_TYPE=udp \
  -e SOCKET_HOST=0.0.0.0 \
  -e SOCKET_PORT=8080 \
  -e SOCKET_TOKEN=your_token \
  ghcr.io/qurre-sl/scplogs:latest
```

**Docker Compose (inline):**

```yaml
version: '3.8'
services:
  scplogs:
    image: ghcr.io/qurre-sl/scplogs:latest
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      SENDER_TYPE: discord
      SENDER_TIMEOUT_SECONDS: 60
      DISCORD_TOKEN: your_token
      DISCORD_COMMAND_GUILDS: "123,456"
      DISCORD_ALLOWED_CHANNELS: "789,012"
      SOCKET_TYPE: udp
      SOCKET_HOST: 0.0.0.0
      SOCKET_PORT: 8080
      SOCKET_TOKEN: your_token
```

**Docker Compose (env_file):**

```yaml
version: '3.8'
services:
  scplogs:
    image: ghcr.io/qurre-sl/scplogs:latest
    restart: unless-stopped
    ports:
      - "8080:8080"
    env_file:
      - .env
```

**Теги:**
- `latest` — stable релиз
- `v3-preview`, `main`, `dev` — ветки разработки
- `1.2.3`, `1.2`, `1` — версии

---

## Lua события

### Формат файла

**Файл:** `LabAPI/configs/SCPLogs/PlayerEvents.Joined.lua`

```lua
-- @enabled true
-- @channels 123456789,987654321
--
-- Доступные значения:
-- Player - игрок (LabApi.Features.Wrappers.Players.Player)
--   Player.DisplayName - никнейм
--   Player.UserId - Steam ID
--   Player.Role - текущая роль (RoleTypeId)
--
-- Функции:
-- SendLog(message, channels?) - отправить лог
-- PrintTime() - Discord timestamp
-- PrintPlayer(player, printRole?) - форматированная инфа об игроке
-- IsOneFraction(player1, player2) - одна ли фракция

reply = string.format("%s Игрок **%s** подключился к серверу",
    PrintTime(),
    Player.DisplayName)
```

### Список событий

Все события из `LabApi.Events.Handlers.*`:

- `PlayerEvents.Joined` / `PlayerEvents.Left`
- `PlayerEvents.Death` / `PlayerEvents.Dying`
- `PlayerEvents.Escaping` / `PlayerEvents.Escaped`
- `ServerEvents.RoundStarted` / `ServerEvents.RoundEnded`
- `WarheadEvents.Starting` / `WarheadEvents.Detonated`
- И т.д. — полный список в исходниках LabAPI

### Где хранятся

```
AppData/SCP Secret Laboratory/LabAPI/configs/SCPLogs/
├── PlayerEvents.Joined.lua
├── PlayerEvents.Death.lua
├── ServerEvents.RoundStarted.lua
└── ...
```

---

## Протоколы

| Протокол  | Описание | Рекомендация |
|-----------|----------|--------------|
| **UDP**   | Датаграммы, быстро | ✅ Рекомендуется |
| **TCP**   | Потоковое соединение | Стабильное |
| **HTTP**  | REST API через Deno.serve | Для дебага |
| **WebSocket** | Двустороннее | Оверкилл |

Все поддерживают авто-реконнект при обрыве связи.

---

## Конфигурация

### scplogs.yml

| Параметр | Тип | Описание |
|----------|-----|----------|
| `ip` | string | IP клиента (127.0.0.1 если на том же хосте) |
| `port` | uint | Порт клиента (должен совпадать с `.env`) |
| `protocol` | enum | `http` / `tcp` / `udp` / `websocket` |
| `clientToken` | string | Токен авторизации (должен совпадать с `.env`) |
| `badgeOnline` | string | Lua для статуса бота (переменные: `Count`, `Slots`) |
| `dontSendEvents` | string[] | Список событий которые НЕ отправлять |
| `luaConfig.declareTypes` | array | Дополнительные типы для Lua |

### Lua файл

Специальные комментарии в начале файла:

```lua
-- @enabled true|false - включить/выключить событие
-- @channels 123,456 - ID каналов Discord/Telegram (через запятую)
```

---

## Фишки

### 1. Динамическая регистрация событий

Плагин автоматически подхватывает **все** события из LabAPI. Не нужно ждать обновления плагина при добавлении новых событий в LabAPI.

### 2. Кеширование типов Lua

Типы регистрируются один раз при загрузке → производительность.

### 3. Независимость сокетов

Client использует dynamic imports — если выбран UDP, не загружаются зависимости для HTTP/TCP/WebSocket.

### 4. Фильтрация событий

Через `dontSendEvents` можно исключить события из отправки (например, спам-события).

### 5. Отдельные Lua на событие

Вместо одного огромного конфига — каждое событие в отдельном файле. Легко управлять и шарить между серверами.

---

## Примеры Lua

### Простой

```lua
-- @enabled true
-- @channels 123456789

reply = PrintTime() .. " Раунд начался!"
```

### С проверкой

```lua
-- @enabled true
-- @channels 123456789

if Attacker ~= nil then
    reply = string.format("%s %s убил %s",
        PrintTime(),
        PrintPlayer(Attacker),
        PrintPlayer(Player))
else
    reply = string.format("%s %s умер",
        PrintTime(),
        PrintPlayer(Player))
end
```

### С отправкой в разные каналы

```lua
-- @enabled true
-- @channels 123456789

if Player.Role == RoleTypeId.Scp096 then
    SendLog("SCP-096 сбежал!", {"111111111"})
end

reply = PrintTime() .. " Игрок сбежал: " .. PrintPlayer(Player)
```

---

## Troubleshooting

### Плагин не загружается

1. Проверить что LabAPI установлен и работает
2. Проверить что нет конфликтов версий .NET
3. Посмотреть логи в `AppData/SCP Secret Laboratory/LabAPI/logs/`

### Client не подключается

1. Проверить совпадение `clientToken` в `.yml` и `.env`
2. Проверить совпадение портов
3. Проверить что выбран одинаковый протокол
4. Убедиться что файрволл не блокирует порт

### События не отправляются

1. Проверить что `.lua` файлы в правильной папке
2. Проверить что `@enabled true`
3. Проверить что `@channels` указаны правильные ID
4. Проверить что событие не в списке `dontSendEvents`

---

## Разработка

### Добавить новое событие

1. Создать `LabAPI/configs/SCPLogs/{EventClass}.{EventName}.lua`
2. Настроить `@enabled` и `@channels`
3. Написать логику в Lua
4. Перезагрузить плагин (`reload plugins` в RA)

### Структура проекта

```
SCPLogs/
├── plugin/                  # C# плагин
│   ├── Main.cs             # Точка входа
│   ├── Events.cs           # Система событий
│   ├── Configs/            # Классы конфигов
│   ├── Sockets/            # HTTP/TCP/UDP/WebSocket клиенты
│   ├── Lua/                # Lua интеграция (MoonSharp)
│   └── Extensions/         # Хелперы
├── client/                  # Deno клиент
│   └── src/
│       ├── main.ts         # Точка входа
│       ├── sockets/        # Серверы протоколов
│       └── senders/        # Discord/Telegram/WebHook
└── LabAPI/                  # Исходники LabAPI (submodule)
```

---

## FAQ

**Q: Чем отличается от v2?**
A: v3 использует LabAPI вместо Qurre, отдельные Lua файлы вместо монолитного конфига, Deno вместо Node.js

**Q: Поддерживается ли Qurre?**
A: Нет, только LabAPI. Для Qurre используй v2.

**Q: Можно ли использовать несколько клиентов на один плагин?**
A: Нет, one-to-one. Но можно отправлять в разные каналы через `SendLog()`.

**Q: Как обновить плагин?**
A: Заменить `.dll`, конфиги сохранятся. Lua файлы — ручная миграция.

---

## Лицензия

MIT License - делай че хочешь.

---

<p align="center"><img src="https://count.getloli.com/get/@SCPLogs"></p>
