namespace Сheckers.GameObjects.Scanning
{
    /// <summary>
    /// Определяет четыре диагональных направления движения по игровой доске.
    /// </summary>
    public enum RayDirection
    {
        /// <summary> Направление не определено. </summary>
        None,

        /// <summary> Вверх-влево (уменьшение ряда, уменьшение столбца). </summary>
        UpLeft,

        /// <summary> Вверх-вправо (уменьшение ряда, увеличение столбца). </summary>
        UpRight,

        /// <summary> Вниз-влево (увеличение ряда, уменьшение столбца). </summary>
        DownLeft,

        /// <summary> Вниз-вправо (увеличение ряда, увеличение столбца). </summary>
        DownRight
    }

    /// <summary>
    /// Предоставляет вспомогательные методы для работы с направлениями лучей.
    /// </summary>
    public static class RayDirectionExtensions
    {
        // Кешируем векторы, чтобы не создавать новые объекты Point постоянно
        private static readonly Point _upLeft = new(-1, -1);
        private static readonly Point _upRight = new(-1, 1);
        private static readonly Point _downLeft = new(1, -1);
        private static readonly Point _downRight = new(1, 1);
        private static readonly Point _zero = new(0, 0);

        /// <summary>
        /// Преобразует направление луча в вектор смещения (Point).
        /// </summary>
        /// <param name="direction">Направление луча.</param>
        /// <returns>Точка-вектор с компонентами -1, 0 или 1.</returns>
        public static Point GetVector(this RayDirection direction) => direction switch
        {
            RayDirection.UpLeft => _upLeft,
            RayDirection.UpRight => _upRight,
            RayDirection.DownLeft => _downLeft,
            RayDirection.DownRight => _downRight,
            _ => _zero
        };

        /// <summary>
        /// Массив всех игровых направлений (диагоналей). 
        /// Используется для оптимизации циклов сканирования, чтобы избежать Enum.GetValues.
        /// </summary>
        public static readonly RayDirection[] AllDiagonals =
        [
            RayDirection.UpLeft,
            RayDirection.UpRight,
            RayDirection.DownLeft,
            RayDirection.DownRight
        ];

        /// <summary>
        /// Определяет, является ли направление "движением вперед" для конкретной стороны.
        /// </summary>
        /// <param name="direction">Направление луча.</param>
        /// <param name="side">Сторона (цвет) фигуры.</param>
        /// <remarks>
        /// Для белых "вперед" — это уменьшение индекса строки (вверх).
        /// Для черных "вперед" — это увеличение индекса строки (вниз).
        /// </remarks>
        public static bool IsForwardFor(this RayDirection direction, PieceSide side) => side switch
        {
            // Для белых вперед — это всё, что UP
            PieceSide.White => direction == RayDirection.UpLeft || direction == RayDirection.UpRight,
            // Для черных вперед — это всё, что DOWN
            PieceSide.Black => direction == RayDirection.DownLeft || direction == RayDirection.DownRight,
            _ => false
        };
    }
}
