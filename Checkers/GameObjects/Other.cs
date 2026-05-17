namespace Сheckers.GameObjects
{
    /// <summary>
    /// Определяет текущее состояние игрового процесса.
    /// </summary>
    public enum GameStatus
    {
        /// <summary> Игра продолжается. </summary>
        InProgress,
        /// <summary> Игра завершена победой белых. </summary>
        WhiteWin,
        /// <summary> Игра завершена победой черных. </summary>
        BlackWin,
        /// <summary> Игра завершена вничьей. </summary>
        Draw
    }

    /// <summary>
    /// Логическая причина завершения партии, определенная ядром.
    /// </summary>
    public enum GameEndReason
    {
        /// <summary> Причины нет. </summary>
        None,
        /// <summary> У стороны не осталось фигур. </summary>
        AllPiecesCaptured,
        /// <summary> У стороны нет доступных ходов (заперты). </summary>
        NoAvailableMoves,
        /// <summary> Оба игрока заблокированы (взаимный пат). </summary>
        MutualBlock,
        /// <summary> Партия завершена внешним вызовом (сдались, истекло время или решение сервиса). </summary>
        ExternalCommand
    }

    /// <summary>
    /// Определяет сторону (цвет фигур) игрока в партии.
    /// </summary>
    public enum PieceSide
    {
        /// <summary> Белая сторона. </summary>
        White,
        /// <summary> Черная сторона. </summary>
        Black
    }

    /// <summary>
    /// Ранг фигуры, определяющий её возможности перемещения.
    /// </summary>
    public enum PieceType
    {
        /// <summary>Шашка</summary>
        Man,

        /// <summary>Дамка</summary>
        King
    }

    /// <summary>
    /// Координаты клетки на игровой доске.
    /// </summary>
    /// <param name="Row">Индекс строки (горизонталь).</param>
    /// <param name="Col">Индекс столбца (вертикаль).</param>
    public readonly record struct Point( int Row, int Col)
    {
        /// <summary>
        /// Складывает координаты двух точек. 
        /// Используется для вычисления новых позиций на основе векторов направления.
        /// </summary>
        public static Point operator + (Point a, Point b)=>
            new(a.Row + b.Row, a.Col + b.Col);
    }

    /// <summary>
    /// Описание одиночного перемещения фигуры (шага или прыжка).
    /// </summary>
    /// <param name="From">Исходная клетка.</param>
    /// <param name="To">Целевая клетка.</param>
    /// <param name="Target">Клетка со взятой фигурой (null, если ход без боя).</param>
    public record Move(Point From, Point To, Point? Target = null)
    {
        /// <summary>
        /// Указывает, является ли данное перемещение взятием фигуры противника.
        /// </summary>
        public bool IsCapture => Target.HasValue;
    }
}
