using Checkers.Engine.Models;

namespace Checkers.Engine.Core
{
    /// <summary>
    /// Предоставляет набор атомарных команд для изменения состояния доски.
    /// Используется правилами (Rules) для выполнения этапов хода.
    /// </summary>
    public interface ITurnActions
    {
        /// <summary> Выполняет физическое перемещение фигуры. </summary>
        void ApplyMove(Move move);

        /// <summary> Помечает фигуру в указанной точке как взятую (битую). </summary>
        void ApplyCaptureMark(Point target);

        /// <summary> Мгновенно изменяет ранг фигуры (повышает до дамки). </summary>
        void ApplyPromotion(Point pieceLocation);

        /// <summary> Помечает фигуру как готовую к повышению в конце хода. </summary>
        void ApplyPromotionMark(Point target);

        /// <summary> Окончательно удаляет все помеченные битые фигуры с доски. </summary>
        void ApplyRemoval();

        /// <summary> Удаляет фигуру с доски. </summary>
        void ApplyRemoval(Point target);

        /// <summary> Применяет все отложенные эффекты (снятие фигур, материализация дамок). </summary>
        void ApplyFinalEffects();

        /// <summary> Применяет все отложенные эффекты (снятие фигур, материализация дамок). </summary>
        bool CanPromote(Point square);

        /* ПАМЯТКА: Возможно стоит добавить:
         * void ApplyEnd();            - Для явной финализации статуса в Turn
         * bool CanCaptureMore();      - Если Executor будет делегировать проверку серии Scanner-у
         */
    }

    /// <summary>
    /// Исполнитель команд, транслирующий действия правил в изменения на доске.
    /// </summary>
    internal class TurnExecutor : ITurnActions
    {
        private readonly Chessboard _board;

        public TurnExecutor(Chessboard board) => _board = board;

        public void ApplyMove(Move move) => _board.Execute(move);

        public void ApplyCaptureMark(Point target) => _board.MarkAsCaptured(target);

        public void ApplyPromotion(Point target) => _board.Transform(target);

        public void ApplyPromotionMark(Point target) => _board.MarkAsPromoted(target);

        public void ApplyRemoval() => _board.RemoveCapturedPieces();

        public void ApplyRemoval(Point target) => _board.Remove(target);

        public void ApplyFinalEffects() => _board.SettleTurn();

        public bool CanPromote(Point square)
        {
            _board.TryGetPiece(square,out var piece);
            return piece is not null && _board.IsPromotionEdge(square, piece.Side);
        }
    }
}