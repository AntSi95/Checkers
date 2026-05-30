# 🇷🇺 Спецификация: Русские шашки (`RussianRules.cs`)

Данный документ описывает логические особенности русских шашек и содержит полную техническую реализацию стратегии правил для игрового движка.

## 📌 Логические особенности правил
*   **Игровое поле:** Классическая доска 8x8. Игра ведется строго по темным клеткам.
*   **Правила хода простых шашек:** Ходят только вперед на одну клетку по диагонали.
*   **Правила боя простых шашек:** Бьют как вперед, так и назад. При прыжке шашка перешагивает через фигуру соперника на следующую за ней пустую клетку.
*   **Специфика Дамки:** Дамка является дальнобойной («летающей»). Она может перемещаться по диагонали на любое количество свободных клеток в любом направлении.
*   **Приоритет взятия:** Если на доске есть возможность боя, игрок обязан бить. Однако, если есть выбор между простым тихим ходом и боем, или выбор между разными направлениями боя, игрок вправе выбрать траекторию с любым количеством сбиваемых фигур (приоритет большинства отсутствует).
*   **Превращение в полете:** Если простая шашка в процессе выполнения серии прыжков временно наступает на дамочное поле, она **мгновенно** становится дамкой и сразу же продолжает текущую серию взятий по правилам дамки (если есть доступные цели).

---

## 💻 Техническая реализация правил

Ниже представлен чистый код класса правил, полностью интегрированный с системами `MoveScanner` и `GameSession`:

```csharp
using Checkers.Engine.Models;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Rules.Variants
{
    public class RussianRules : IRulesStrategy
    {
        #region методы создания игрового поля

        // Задаем параметры доски: 8 рядов, 8 столбцов, играем на темных (четных) клетках
        public BoardSettings GetSettings() => new(8, 8, UseEvenSquares: true);

        // Выполняем стартовую расстановку шашек на доске
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

        #endregion

        #region методы сканирования

        // Проверяет, является ли фигура целью для атаки
        public bool IsEnemy(Piece actor, Piece target)
        {
            // Турецкий удар: нельзя бить ту же фигуру второй раз за серию
            return actor.Side != target.Side && !target.IsCaptured;
        }

        // Главный судейский метод: оценивает каждую клетку на пути диагонального луча
        public ScanVerdict EvaluateMove(Piece actor, RayDirection ray, ScanState state, int distance, bool isOccupied)
        {
            bool isForward = ray.IsForwardFor(actor.Side);

            if (actor.Type == PieceType.Man)
            {
                return state switch
                {
                    // Тихий ход: 1 клетка вперед. 
                    // Если занято (isOccupied) — возвращаем (false, true), чтобы сканер проверил фигуру на "враждебность".
                    ScanState.Default => new ScanVerdict(
                        IsPossible: distance == 1 && isForward && !isOccupied,
                        CanContinue: isOccupied && distance == 1 // Если пусто — можно было бы идти дальше (но у пешки лимит 1), если занято — стоп для пешки, но шанс для IsEnemy
                    ),

                    // Режим поиска боя: саму пустую клетку подтвердить нельзя, 
                    // но луч продолжаем, пока не встретим цель (на расстоянии 1).
                    ScanState.ForcedCaptureOnly => new ScanVerdict(
                        IsPossible: false,
                        CanContinue: isOccupied && distance == 1
                    ),

                    // Фаза прыжка: приземление строго на дистанции 2 (сразу за врагом).
                    ScanState.TargetDetected => new ScanVerdict(
                        IsPossible: !isOccupied && distance == 2,
                        CanContinue: false // После взятия/не удачной попытки сканировать уже нечего
                    ),

                    _ => new ScanVerdict(false, false)
                };
            }
            else if (actor.Type == PieceType.King)
            {
                return state switch
                {
                    // Тихий ход: разрешен на любую дистанцию, пока путь пуст.
                    ScanState.Default => new ScanVerdict(
                        IsPossible: !isOccupied,
                        CanContinue: true
                    ),

                    // Дамка ищет врага "вдалеке": скользим по пустым клеткам.
                    ScanState.ForcedCaptureOnly => new ScanVerdict(
                        IsPossible: false,
                        CanContinue: true
                    ),

                    // После обнаружения цели (TargetDetected) или совершения прыжка (CaptureMoveFound)
                    // Дамка может приземлиться на любую пустую клетку и лететь дальше.
                    ScanState.TargetDetected or ScanState.CaptureMoveFound => new ScanVerdict(
                        IsPossible: !isOccupied,
                        CanContinue: !isOccupied
                    ),

                    _ => new ScanVerdict(false, false)
                };
            }

            return new ScanVerdict(false, false);
        }

        #endregion

        #region методы реализации хода

        // Управляет физическим выполнением шага и фиксирует его последствия
        public bool ProcessStep(ITurnExecution actions, Move move)
        {
            actions.ApplyMove(move);

            if (move.IsCapture)
            {
                actions.ApplyCaptureMark(move.Target!.Value);

                // В русских шашках шашка ставится дамкой сразу, если наступили на край поля
                if (actions.CanPromote(move.To))
                    actions.ApplyPromotion(move.To);

                return true; // Был бой, сессия должна проверить продолжение
            }

            // Тихий ход: просто проверяем дамку в конце пути
            if (actions.CanPromote(move.To))
                actions.ApplyPromotion(move.To);

            return false; // Тихий ход — серия невозможна
        }

        // Вызывается один раз, когда игрок полностью завершил все действия своего хода
        public void OnFinalize(ITurnExecution actions, Point lastPostion) => actions.ApplyFinalEffects();

        #endregion

        #region методы глобальных игровых правил

        // Определяет поведение системы, если у текущего игрока полностью закончились ходы
        public TurnResult HandleNoMoves(IBoardInspection board, PieceSide side) => TurnResult.GameFinished;

        // Выносит окончательный технический вердикт о результатах завершенной партии
        public GameResult JudgeTerminalState(IBoardInspection board, PieceSide side)
        {
            //TODO: доработать как метод так и GameResult. Сейчас делает слишком мало за слишком большие реурсы.
            var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
            bool hasPieces = board.GetValidSquares().Any(s => { board.TryGetPiece(s, out var p); return p?.Side == side; });

            return new GameResult(winner, hasPieces ? GameEndReason.NoAvailableMoves : GameEndReason.AllPiecesCaptured);
        }

        #endregion
    }
}

```