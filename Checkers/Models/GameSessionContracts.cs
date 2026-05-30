namespace Checkers.Engine.Models
{
    /// <summary>
    /// Полный срез данных о текущем состоянии игровой сессии для UI или API.
    /// </summary>
    /// <param name="ActiveSide">Сторона, чей сейчас ход.</param>
    /// <param name="Status">Общий статус игры (Идёт, Победа белых/черных, Ничья).</param>
    /// <param name="Reason">Техническая причина текущего статуса.</param>
    /// <param name="IsTurnInProgress">Флаг, указывающий, что игрок уже начал серию прыжков, но не завершил ход.</param>
    /// <param name="Grid">Двумерный массив (снимок) доски с фигурами.</param>
    public record SessionInfo(
        PieceSide ActiveSide,
        GameStatus Status,
        GameEndReason Reason,
        bool IsTurnInProgress,
        Piece?[,] Grid
    );

    /// <summary>
    /// Представляет полный снимок состояния игры для сохранения и последующего восстановления.
    /// Содержит минимально необходимый набор данных для воссоздания сессии.
    /// </summary>
    /// <param name="RuleSetId">Уникальный идентификатор набора правил (название класса стратегии).</param>
    /// <param name="History">Полная хронология ходов, включая незавершенные действия.</param>
    public record GameSnapshot(
        string RuleSetId,
        List<Turn> History
    );
}
