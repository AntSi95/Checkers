using Checkers.Engine.Models;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Rules.Variants
{
    /// <summary>
    /// Стратегия правил Международных шашек (100-клеточные).
    /// </summary>
    /// <remarks>
    /// Ключевые особенности:
    /// 1. Доска 10x10, по 20 фигур у каждой стороны (занимают по 4 ряда).
    /// 2. Простая шашка ходит ТОЛЬКО вперед, но бьет и вперед, и назад.
    /// 3. Обязателен выбор варианта боя с максимальным количеством взятых фигур (Приоритет большинства).
    /// 4. Дамка "дальнобойная", как в русских шашках.
    /// 5. Правило "остановки" для дамки: шашка становится дамкой только если завершает ход 
    ///    на дамочном поле. Если она проходит его "транзитом" во время боя, она остается простой.
    /// 6. Турецкий удар: битые фигуры снимаются только после завершения всего хода.
    /// </remarks>
    public class InternationalRules : IRulesStrategy
    {
        /// <inheritdoc />
        public BoardSettings GetSettings() => new(10, 10, UseEvenSquares: true);

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool IsEnemy(Piece actor, Piece target) => actor.Side != target.Side && !target.IsCaptured;

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void OnFinalize(ITurnExecution actions, Point lastPostion)
        {
            // Здесь решается "Дамка или нет" для международных:
            // Если остановились на краю — превращаем.
            if(actions.CanPromote(lastPostion))
                actions.ApplyPromotion(lastPostion);
            actions.ApplyFinalEffects();
        }

        /// <inheritdoc />
        public TurnResult HandleNoMoves(IBoardInspection board, PieceSide side) => TurnResult.GameFinished;

        /// <inheritdoc />
        public GameResult JudgeTerminalState(IBoardInspection board, PieceSide side)
        {
            var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
            bool hasPieces = board.GetValidSquares().Any(s => { board.TryGetPiece(s, out var p); return p?.Side == side; });

            return new GameResult(winner, hasPieces ? GameEndReason.NoAvailableMoves : GameEndReason.AllPiecesCaptured);
        }
    }
}