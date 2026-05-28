using Checkers.GameObjects.Scanning;

namespace Checkers.GameObjects.Rules
{

    /// <summary>
    /// Правила Английских шашек (Checkers).
    /// </summary>
    /// <remarks>
    /// 1. Доска 8x8. Простые не бьют назад.
    /// 2. Дамка (King) ходит и бьет только на соседние клетки (не летает).
    /// 3. При достижении края в серии боя ход всегда завершается (превращение отложенное).
    /// </remarks>
    public class EnglishRules : IRulesStrategy
    {
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

        /// <inheritdoc />
        public bool IsEnemy(Piece actor, Piece target) => actor.Side != target.Side && !target.IsCaptured;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool ProcessStep(ITurnActions actions, Move move)
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

        /// <inheritdoc />
        public void OnFinalize(ITurnActions actions, Point lastPostion) => actions.ApplyFinalEffects();

        /// <inheritdoc />
        public TurnResult HandleNoMoves(PieceSide side, Chessboard board) => TurnResult.GameFinished;

        /// <inheritdoc />
        public GameResult JudgeTerminalState(PieceSide side, Chessboard board)
        {
            var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
            return new GameResult(winner, GameEndReason.NoAvailableMoves);
        }
    }
}
