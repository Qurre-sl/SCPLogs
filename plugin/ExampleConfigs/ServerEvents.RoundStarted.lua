-- @enabled true
-- @channels 123456789,987654321
--
-- Файл: ServerEvents.RoundStarted.lua
-- Событие: ServerEvents.RoundStarted (событие без параметров)
--
-- Доступные функции:
-- SendLog(message, channels?) - отправить лог в каналы
-- PrintTime() - получить форматированное время
--
-- Результат нужно записать в переменную reply

reply = PrintTime() .. " :video_game: Раунд начался!"
