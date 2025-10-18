-- @enabled true
-- @channels 123456789
--
-- Файл: PlayerEvents.Death.lua
-- Событие: PlayerEvents.Death
--
-- Доступные значения:
-- Player - игрок который умер (LabApi.Features.Wrappers.Players.Player)
-- Attacker - атакующий игрок (LabApi.Features.Wrappers.Players.Player или nil)
-- DamageType - тип урона (DamageType enum)
-- Enum_DamageType - доступ к enum DamageType для сравнения
--
-- Дополнительные свойства Player:
--   Player.Position - позиция игрока (Vector3)
--   Player.Health - текущее здоровье (float)
--   Player.MaxHealth - максимальное здоровье (float)
--
-- Результат нужно записать в переменную reply

if Attacker ~= nil then
    reply = string.format("%s %s убил %s (тип урона: %s)",
        PrintTime(),
        PrintPlayer(Attacker),
        PrintPlayer(Player),
        tostring(DamageType))
else
    reply = string.format("%s %s умер (тип урона: %s)",
        PrintTime(),
        PrintPlayer(Player),
        tostring(DamageType))
end
