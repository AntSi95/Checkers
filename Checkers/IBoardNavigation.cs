using Checkers.Engine.Models;

namespace Checkers.Engine
{
    /// <summary>
    /// Контракт навигации, проверки границ и обхода клеток для механизмов поиска ходов (MoveScanner).
    /// </summary>
    public interface IBoardNavigation
    {

        /// <summary>
        /// Пытается получить фигуру. Возвращает false, если клетка пуста или неигровая.
        /// </summary>
        bool TryGetPiece(Point square, out Piece? piece);

        /// <summary>
        /// Возвращает перечисление всех игровых клеток доски для обхода.
        /// </summary>
        IEnumerable<Point> GetValidSquares();
    }
}
