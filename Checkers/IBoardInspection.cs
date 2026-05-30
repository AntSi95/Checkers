using Checkers.Engine.Models;

namespace Checkers.Engine
{
    // TODO: стоит доработать, что бы не отдавать по факту доску целиком.
    // Cейчас он дублирует IBoardNavigation хоть и нужен для других задачь.
    // В этих же рамках стоит полностью пересмотреть роль Chessboard что бы он не лопнул от методов.
    /// <summary>
    /// Контракт судейства для стратегий правил (Rules).
    /// Предоставляет безопасный доступ только для обхода игровых полей.
    /// </summary>
    public interface IBoardInspection
    {
        /// <summary>
        /// Возвращает перечисление всех игровых клеток доски для проверки статуса фигур.
        /// </summary>
        IEnumerable<Point> GetValidSquares();

        /// <summary>
        /// Пытается получить фигуру. Возвращает false, если клетка пуста или неигровая.
        /// </summary>
        bool TryGetPiece(Point square, out Piece? piece);
    }
}
