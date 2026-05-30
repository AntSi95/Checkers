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
using Checkers.Engine.Models;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Rules.Variants
    public class EnglishRules : IRulesStrategy
    {
        // Настройка доски: 8 рядов, 8 столбцов, играем на темных (четных) клетках
        public BoardSettings GetSettings() => new(8, 8, UseEvenSquares: true);

        public IEnumerable<StartPosition> GetInitialPositions()
        {
            // Традиционная расстановка: 3 ряда белых (снизу), 3 ряда черных (сверху)
            // Внизу доски находятся первые ряды
            var positions = new List<StartPosition>(24);

            for (int row = 0; row < 3; row++)
                for (int col = (row % 2); col < 8; col += 2)
                    positions.Add(new StartPosition(new Point(row, col), PieceSide.White, PieceType.Man));
            for (int row = 5; row < 8; row++)
                for (int col = (row % 2); col < 8; col += 2)
                    positions.Add(new StartPosition(new Point(row, col), PieceSide.Black, PieceType.Man));
            return positions;
        }

        // Проверяет, доступна ли фигура для взятия
        public bool IsEnemy(Piece actor, Piece target) => actor.Side != target.Side && !target.IsCaptured;

        // Главный судейский метод: оценивает клетку на векторе диагонального луча
        public ScanVerdict EvaluateMove(Piece actor, RayDirection ray, ScanState state, int distance, bool isOccupied)
        {
            // 1. Главный фильтр Чекерса: простая шашка не видит ничего за спиной.
            if (actor.Type == PieceType.Man && !ray.IsForwardFor(actor.Side))
            {
                return new ScanVerdict(false, false);
            }

            // 2. Универсальная логика короткоходной фигуры (и Man, и King одинаковы)
            return state switch
            {
                // Тихий ход: строго на 1 клетку.
                ScanState.Default => new ScanVerdict(
                    IsPossible: distance == 1 && !isOccupied,
                    CanContinue: distance == 1 && isOccupied // Даем шанс на бой
                ),

                // Поиск боя: скользим до дистанции 1, если там препятствие.
                ScanState.ForcedCaptureOnly => new ScanVerdict(
                    IsPossible: false,
                    CanContinue: distance == 1 && isOccupied
                ),

                // Приземление: строго на дистанции 2 за врагом.
                ScanState.TargetDetected => new ScanVerdict(
                    IsPossible: distance == 2 && !isOccupied,
                    CanContinue: false
                ),

                _ => new ScanVerdict(false, false)
            };
        }

        // Управляет физическим применением шага на доске
        public bool ProcessStep(ITurnExecution actions, Move move)
        {
            actions.ApplyMove(move);

            if (move.IsCapture)
            {
                actions.ApplyCaptureMark(move.Target!.Value);

                // Если шашка достигла края — помечаем, но НЕ превращаем сразу.
                // В Чекерсе ход дамкой на краю СРАЗУ завершается.
                if (actions.CanPromote(move.To))
                {
                    actions.ApplyPromotionMark(move.To);
                    return false; // Специфика: серия прерывается при достижении края
                }
                return true;
            }

            if (actions.CanPromote(move.To)) actions.ApplyPromotionMark(move.To);
            return false;
        }

        // Вызывается один раз в конце хода, когда действия игрока зафиксированы как финальные
        public void OnFinalize(ITurnExecution actions, Point lastPostion) => actions.ApplyFinalEffects();

        // Решает, что делать при полном запирании ходов у текущей стороны
        public TurnResult HandleNoMoves(IBoardInspection board, PieceSide side) => TurnResult.GameFinished;

        // Выносит окончательный технический вердикт о результатах партии
        public GameResult JudgeTerminalState(IBoardInspection board, PieceSide side)
        {
            //TODO: доработать как метод так и GameResult. Сейчас делает слишком мало за слишком большие реурсы.
            var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
            bool hasPieces = board.GetValidSquares().Any(s => { board.TryGetPiece(s, out var p); return p?.Side == side; });

            return new GameResult(winner, hasPieces ? GameEndReason.NoAvailableMoves : GameEndReason.AllPiecesCaptured);
        }
    }
}
```
