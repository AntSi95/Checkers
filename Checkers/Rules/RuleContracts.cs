using Checkers.Engine.Models;

namespace Checkers.Engine.Rules
{
    // TODO: небольшая оптимизация перивести enum к типу byte (по дефолту это int)
    /// <summary>
    /// Определяет решение правил о дальнейшем развитии игрового процесса.
    /// </summary>
    public enum TurnResult
    {
        /// <summary> Можно продолжать текущий ход (серия). </summary>
        Continue,
        /// <summary> Ход переходит к другому игроку. </summary>
        SwitchSide,
        /// <summary> Игра окончена (победа/поражение/ничья). </summary>
        GameFinished
    }

    /// <summary>
    /// Вердикт правил относительно конкретной клетки на пути сканирования.
    /// </summary>
    /// <param name="IsPossible">Можно ли физически завершить движение в этой клетке.</param>
    /// <param name="CanContinue">Стоит ли сканеру смотреть следующие клетки на этом луче.</param>
    public record struct ScanVerdict(bool IsPossible, bool CanContinue);

    /// <summary>
    /// Настройки геометрии и начального состояния игры.
    /// </summary>
    public record BoardSettings(int Rows, int Cols, bool UseEvenSquares = true);

    /// <summary>
    /// Описание начальной позиции конкретной фигуры.
    /// </summary>
    public record StartPosition(Point Square, PieceSide Side, PieceType Type = PieceType.Man);

    //TODO: подумать о том как его либо убрать, либо расширить, вместе c методами использующим эту запись.
    /// <summary>
    /// Технический вердикт о результате завершённой партии.
    /// </summary>
    /// <param name="Status">Итоговое состояние (победа одной из сторон или ничья).</param>
    /// <param name="Reason">Логическое обоснование финала, определённое правилами.</param>
    public record GameResult(GameStatus Status, GameEndReason Reason);
}
