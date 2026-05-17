# 🇬🇧 Спецификация: English Checkers / Draughts (`EnglishRules.cs`)

Данный документ описывает логические особенности английских шашек (Чекерс) и содержит оригинальную техническую реализацию стратегии правил для игрового движка.

## 📌 Логические особенности правил
*   **Игровое поле:** Стандартная доска 8x8. Игра ведется по темным клеткам.
*   **Простые шашки:** Ходят **и бьют только вперед** строго на одну клетку по диагонали. Прыжки назад для простых фигур запрещены.
*   **Дамка (King):** Является короткоходной. Ходит и бьет как вперед, так и назад, но строго на одну пустую клетку при тихом ходе и ровно на две клетки (через одну фигуру) при взятии. Дальнобойность отсутствует.
*   **Приоритет взятия:** Бой обязателен, но приоритет большинства отсутствует (выбор траектории с максимальным числом сбиваемых фигур не требуется).
*   **Особое правило дамки:** Если простая шашка в процессе серии прыжков достигает противоположного края доски, она превращается в дамку, но её ход **немедленно останавливается**. Продолжать серию прыжков в текущем ходу, даже будучи дамкой, запрещено.

---

## 💻 Техническая реализация правил

Ниже представлен чистый код класса правил, полностью интегрированный со сканером и сессией движка:

```csharp
using checkers.GameObjects;
using checkers.GameObjects.Rules;
using checkers.GameObjects.Scanning;

namespace checkers.Core.Rules;

public class EnglishRules : IRulesStrategy
{
    #region Методы создания игрового поля

    // Настройка доски: 8 рядов, 8 столбцов, играем на темных (четных) клетках
    public BoardSettings GetSettings() => new BoardSettings(8, 8, UseEvenSquares: true);

    // Выполняем стартовую расстановку шашек на доске
    public IEnumerable<StartPosition> GetInitialPositions()
    {
        // Стандартная расстановка 8x8 (по 3 ряда с каждой стороны)
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                // Фильтруем только темные игровые клетки
                if ((r + c) % 2 != 1) continue;
                
                // Верхние три ряда (0, 1, 2) заполняем черными шашками
                if (r < 3) yield return new StartPosition(new Point(r, c), PieceSide.Black, PieceType.Man);
                
                // Нижние три ряда (5, 6, 7) заполняем белыми шашками
                if (r > 4) yield return new StartPosition(new Point(r, c), PieceSide.White, PieceType.Man);
            }
    }

    #endregion

    #region Методы сканирования

    // Проверяет, доступна ли фигура для взятия
    public bool IsEnemy(Piece actor, Piece target)
    {
        // Фигура должна принадлежать оппоненту. 
        // Нельзя бить одну фигуру дважды за серию (у нее IsCaptured == true)
        return actor.Side != target.Side && !target.IsCaptured;
    }

    // Главный судейский метод: оценивает клетку на векторе диагонального луча
    public ScanVerdict EvaluateMove(Piece actor, RayDirection ray, ScanState state, int distance, bool isOccupied)
    {
        // 1. Главный фильтр Чекерса: простая шашка (Man) абсолютно слепа к векторам за спиной
        if (actor.Type == PieceType.Man && !ray.IsForwardFor(actor.Side))
        {
            return new ScanVerdict(IsPossible: false, CanContinue: false);
        }

        // 2. Универсальная логика короткоходной фигуры: после фильтра выше 
        // поведение пешки и дамки на луче становится математически идентичным
        return state switch
        {
            // Фаза Default: Обычный поиск тихих ходов и потенциального боя.
            // - Тихий ход: строго 1 клетка удаления от фигуры и она должна быть пустой.
            // - Если клетка на расстоянии 1 занята (isOccupied): разрешаем сканеру продолжить, 
            //   чтобы система вызвала IsEnemy и зафиксировала цель.
            ScanState.Default => new ScanVerdict(
                IsPossible: distance == 1 && !isOccupied,
                CanContinue: distance == 1 && isOccupied
            ),

            // Фаза ForcedCaptureOnly: В сессии есть обязательный бой.
            // Пустые клетки игнорируются, но скользим до дистанции 1, если там стоит фигура.
            ScanState.ForcedCaptureOnly => new ScanVerdict(
                IsPossible: false,
                CanContinue: distance == 1 && isOccupied
            ),

            // Фаза TargetDetected: На предыдущем шаге (дистанция 1) был обнаружен враг.
            // Точка приземления для короткоходной фигуры обязана быть на расстоянии строго 2 клетки и быть пустой.
            // Дальше сканировать этот луч бессмысленно (CanContinue: false).
            ScanState.TargetDetected => new ScanVerdict(
                IsPossible: distance == 2 && !isOccupied,
                CanContinue: false
            ),

            _ => new ScanVerdict(IsPossible: false, CanContinue: false)
        };
    }

    #endregion

    #region Методы реализации хода

    // Управляет физическим применением шага на доске
    public bool ProcessStep(ITurnActions actions, Move move)
    {
        // Выполняем физический сдвиг фигуры
        actions.ApplyMove(move);

        // Сценарий А: Выполнен прыжок с рубочной целью
        if (move.IsCapture)
        {
            // Вешаем на срубленную фигуру маркер отложенного удаления
            actions.ApplyCaptureMark(move.Target!.Value);

            // Специфика Чекерса: если шашка в результате прыжка достигла края доски,
            // мы вешаем отложенный флаг коронации (ApplyPromotionMark), но ОБЯЗАТЕЛЬНО
            // возвращаем false. Ход принудительно завершается, серия прыжков прерывается.
            if (actions.CanPromote(move.To))
            {
                actions.ApplyPromotionMark(move.To);
                return false; 
            }
            return true; // Был бой не на краю, серия прыжков может продолжиться
        }

        // Сценарий Б: Выполнен обычный тихий ход.
        // Если дошли до дамочного поля в конце пути — вешаем маркер коронации.
        if (actions.CanPromote(move.To)) 
            actions.ApplyPromotionMark(move.To);
            
        return false; // Тихий ход — серия невозможна
    }

    // Вызывается один раз, когда действия игрока зафиксированы как финальные
    public void OnFinalize(ITurnActions actions, Point lastPosition)
    {
        // Запускаем отложенный конвейер изменений: материализуем дамки 
        // и физически удаляем все помеченные сбитые фигуры с доски.
        actions.ApplyFinalEffects();
    }

    #endregion

    #region Методы глобальных игровых правил

    // Решает, что делать при полном запирании ходов у текущей стороны
    public TurnResult HandleNoMoves(PieceSide side, Chessboard board) => TurnResult.GameFinished;

    // Выносит окончательный технический вердикт о результатах партии
    public GameResult JudgeTerminalState(PieceSide side, Chessboard board)
    {
        // В рамках текущей альфа-версии Чекерса заблокированный игрок признается проигравшим
        var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
        return new GameResult(winner, GameEndReason.NoAvailableMoves);
    }

    #endregion
}
```
