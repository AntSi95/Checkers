using Сheckers.GameObjects.Scanning;

namespace Сheckers.GameObjects.Rules
{
    namespace Checkers.Core.Rules
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
                // 4 ряда фигур на доске 10x10
                for (int r = 0; r < 10; r++)
                    for (int c = 0; c < 10; c++)
                    {
                        if ((r + c) % 2 != 1) continue;
                        if (r < 4) yield return new StartPosition(new Point(r, c), PieceSide.Black, PieceType.Man);
                        if (r > 5) yield return new StartPosition(new Point(r, c), PieceSide.White, PieceType.Man);
                    }
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
            public bool ProcessStep(ITurnActions actions, Move move)
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
            public void OnFinalize(ITurnActions actions, Point lastPostion)
            {
                // Здесь решается "Дамка или нет" для международных:
                // Если остановились на краю — превращаем.
                if(actions.CanPromote(lastPostion))
                    actions.ApplyPromotion(lastPostion);
                actions.ApplyFinalEffects();
            }

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
}
