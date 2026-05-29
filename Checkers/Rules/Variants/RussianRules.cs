using Checkers.Engine.Core;
using Checkers.Engine.Models;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Rules.Variants
{
    /// <summary>
    /// Стратегия правил Русских шашек.
    /// </summary>
    /// <remarks>
    /// Ключевые особенности:
    /// 1. Игра ведется на доске 8x8, используются темные клетки.
    /// 2. Взятие (бой) является обязательным. При наличии нескольких вариантов выбор произволен.
    /// 3. Простая шашка бьет назад.
    /// 4. Дамка "дальнобойная": ходит и бьет на любое количество клеток по диагонали.
    /// 5. Правило немедленного превращения: если простая шашка достигает дамочного поля в процессе 
    ///    серии прыжков, она немедленно становится дамкой и продолжает бой как дамка (если есть цели).
    /// 6. Турецкий удар: взятая фигура снимается с доски только после завершения всего хода. 
    ///    Перепрыгивать через одну и ту же фигуру дважды за ход запрещено.
    /// </remarks>
    public class RussianRules : IRulesStrategy
    {
        #region методы создания игрового поля

        /// <inheritdoc />
        public BoardSettings GetSettings() => new(8, 8, UseEvenSquares: true);

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool IsEnemy(Piece actor, Piece target)
        {
            // Турецкий удар: нельзя бить ту же фигуру второй раз за серию
            return actor.Side != target.Side && !target.IsCaptured;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool ProcessStep(ITurnActions actions, Move move)
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

        /// <inheritdoc />
        public void OnFinalize(ITurnActions actions, Point lastPostion) => actions.ApplyFinalEffects();

        #endregion

        #region методы глобальных игровых правил

        /// <inheritdoc />
        public TurnResult HandleNoMoves(PieceSide side, Chessboard board) => TurnResult.GameFinished;

        /// <inheritdoc />
        public GameResult JudgeTerminalState(PieceSide side, Chessboard board)
        {
            var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
            bool hasPieces = board.GetValidSquares().Any(p => board[p]?.Side == side);

            return new GameResult(winner, hasPieces ? GameEndReason.NoAvailableMoves : GameEndReason.AllPiecesCaptured);
        }

        #endregion
    }
}
