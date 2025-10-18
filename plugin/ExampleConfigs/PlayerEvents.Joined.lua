-- @enabled true
-- @channels 123456789,987654321
--
-- Файл: PlayerEvents.Joined.lua
-- Событие: PlayerEvents.Joined
--
-- Доступные значения:
-- Player - игрок который присоединился (LabApi.Features.Wrappers.Players.Player)
--   Player.UserId - ID игрока (string)
--   Player.DisplayName - имя игрока (string)
--   Player.CurrentRole - текущая роль (RoleTypeId)
--   Player.PreviousRole - предыдущая роль (RoleTypeId)
--
-- Доступные функции:
-- SendLog(message, channels?) - отправить лог в каналы
-- PrintTime() - получить форматированное время
-- PrintPlayer(player, printRole?) - форматировать информацию о игроке
-- IsOneFraction(player1, player2) - проверить что игроки одной фракции
--
-- Результат нужно записать в переменную reply

reply = string.format("%s Игрок `%s` (%s) присоединился к серверу",
    PrintTime(),
    Player.DisplayName,
    Player.UserId)
