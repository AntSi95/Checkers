namespace Checkers.Engine.Models
{
    // TODO: Текущая структура хранения Steps уязвима к утечке данных.
    // Необходимо пересмотреть структуру:
    // Перевести Steps на иммутабельную коллекцию или реализовать способ клонирования данных для методов Snapshots
    /// <summary>
    /// Информация о завершённом или текущем ходе игрока.
    /// Содержит последовательность перемещений (цепочку шагов или прыжков).
    /// </summary>
    public class Turn
    {
        /// <summary> Сторона, совершающая ход. </summary>
        public PieceSide Side { get; init; }

        /// <summary> Список последовательных перемещений в рамках одного хода. </summary>
        public List<Move> Steps { get; } = [];

        /// <summary> Указывает, что ход полностью завершён и зафиксирован. </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр хода для указанной стороны.
        /// </summary>
        /// <param name="side">Сторона игрока (цвет), совершающего ход.</param>
        public Turn(PieceSide side)
        {
            Side = side;
        }

        /// <summary>
        /// Возвращает конечную точку последнего выполненного шага.
        /// Используется для продолжения серии захватов из текущей позиции.
        /// </summary>
        public Point? GetLastPosition() => Steps.LastOrDefault()?.To;
    }
}
