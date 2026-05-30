# 🌐 Спецификация: Международные шашки (`InternationalRules.cs`)

Данный документ описывает логические особенности международных стоклеточных шашек и содержит оригинальную техническую реализацию стратегии правил для игрового движка.

## 📌 Логические особенности правил
*   **Игровое поле:** Увеличенная матрица 10x10 (100 клеток). Расстановка сил занимает по 4 ряда с каждой стороны (всего по 20 шашек у игрока).
*   **Простые шашки:** Тихий ход разрешен только вперед на одну клетку. Ударный ход (прыжок) выполняется **как вперед, так и назад** строго на две клетки через фигуру соперника.
*   **Дамка:** «Летающая» (дальнобойная). Ходит и бьет на любое количество свободных клеток по диагонали в любом направлении.
*   **Отложенное превращение (Флаговая модель):** Если простая шашка в процессе выполнения серии прыжков временно пересекает дамочную линию, но логически обязана продолжить бой дальше, она пролетает это поле, **оставаясь простой фигурой**. Она станет дамкой только в том случае, если вся серия взятий окончательно завершится остановкой на дамочной линии.

> [!IMPORTANT]
> **Ограничение текущей версии (Альфа):** 
> На данном этапе в движке не реализован алгоритм глубинного анализа вариантов (построение дерева ходов). В связи с этим строгое правило международных шашек на **Приоритет большинства** (требование выбирать вектор атаки, приносящий максимальное количество сбитых фигур) пока отсутствует. Сканер выдает все физически возможные прыжки, а выбраковка меньших серий будет добавлена на следующем этапе разработки.

---

## 💻 Техническая реализация правил

Ниже представлен чистый код класса правил, полностью интегрированный со сканером и сессией движка:

```csharp
using Checkers.Engine.Models;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Rules.Variants
{
    public class InternationalRules : IRulesStrategy
    {
        // Настройка доски: 10 рядов, 10 столбцов, играем на темных (четных) клетках
        public BoardSettings GetSettings() => new(10, 10, UseEvenSquares: true);

        // Выполняем стартовую расстановку шашек на стоклеточном поле
        public IEnumerable<StartPosition> GetInitialPositions()
        {
            // Традиционная расстановка: 4 ряда белых (снизу), 4 ряда черных (сверху)
            // Внизу доски находятся первые ряды
            var positions = new List<StartPosition>(40);
            for (int row = 0; row < 4; row++)
                for (int col = (row % 2); col < 10; col += 2)
                    positions.Add(new StartPosition(new Point(row, col), PieceSide.White, PieceType.Man));
            for (int row = 6; row < 10; row++)
                for (int col = (row % 2); col < 10; col += 2)
                    positions.Add(new StartPosition(new Point(row, col), PieceSide.Black, PieceType.Man));
            return positions;
        }

        // Проверяет, доступна ли фигура для взятия
        public bool IsEnemy(Piece actor, Piece target) => actor.Side != target.Side && !target.IsCaptured;

        // Главный судейский метод: оценивает каждую клетку на пути диагонального луча 10x10
        public ScanVerdict EvaluateMove(Piece actor, RayDirection ray, ScanState state, int distance, bool isOccupied)
        {
            bool isForward = ray.IsForwardFor(actor.Side);

            if (actor.Type == PieceType.Man)
            {
                return state switch
                {
                    // Ход только вперед
                    ScanState.Default => new ScanVerdict(distance == 1 && isForward && !isOccupied, distance == 1 && isOccupied),
                    // Бьет во все стороны (как в РФ)
                    ScanState.ForcedCaptureOnly => new ScanVerdict(false, distance == 1 && isOccupied),
                    ScanState.TargetDetected => new ScanVerdict(distance == 2 && !isOccupied, false),
                    _ => new ScanVerdict(false, false)
                };
            }

            // Дамка летает точно так же, как в РФ
            return state switch
            {
                ScanState.Default => new ScanVerdict(!isOccupied, true),
                ScanState.ForcedCaptureOnly => new ScanVerdict(false, true),
                ScanState.TargetDetected or ScanState.CaptureMoveFound => new ScanVerdict(!isOccupied, !isOccupied),
                _ => new ScanVerdict(false, false)
            };
        }

        // Управляет физическим выполнением шага фигуры на доске
        public bool ProcessStep(ITurnExecution actions, Move move)
        {
            actions.ApplyMove(move);

            if (move.IsCapture)
            {
                actions.ApplyCaptureMark(move.Target!.Value);
                // В международных если НЕ остановились на краю — НЕ дамка.
                // Наша сессия вызывает сканер ПОСЛЕ этого метода, 
                // поэтому если бить дальше можно, мы не ставим марк.
                return true;
            }

            if (actions.CanPromote(move.To)) actions.ApplyPromotionMark(move.To);
            return false;
        }

        // Вызывается один раз в конце хода, когда действия игрока зафиксированы как финальные
        public void OnFinalize(ITurnExecution actions, Point lastPostion)
        {
            // Здесь решается "Дамка или нет" для международных:
            // Если остановились на краю — превращаем.
            if(actions.CanPromote(lastPostion))
                actions.ApplyPromotion(lastPostion);
            actions.ApplyFinalEffects();
        }

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
