using Checkers.Engine.Models;
using Checkers.Engine.Rules;

namespace Checkers.Engine.Scanning
{
    /// <summary>
    /// Интерфейс поиска доступных ходов.
    /// </summary>
    public interface IMoveScanner
    {
        /// <summary>
        /// Ищем доступные ходы для продолжения серии боев.
        /// </summary>
        List<Move> GetMovesForPiece(IBoardNavigation board, IRulesStrategy rules, Point square);

        /// <summary>
        /// Ищем всех доступные ходы для выбранной стороны.
        /// </summary>
        List<Move> GetMovesForSide(IBoardNavigation board, IRulesStrategy rules, PieceSide side);
    }
}
