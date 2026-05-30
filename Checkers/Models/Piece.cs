namespace Checkers.Engine.Models
{

    /// <summary>
    /// Представляет игровую фигуру (шашку или дамку).
    /// Содержит информацию о принадлежности игроку и текущем статусе.
    /// </summary>
    public class Piece
    {
        /// <summary> Сторона (цвет), которой принадлежит данная фигура. </summary>
        public PieceSide Side { get; init; }

        /// <summary> Текущий тип фигуры (простая шашка или дамка). </summary>
        public PieceType Type { get; set; }

        /// <summary> Фигура помечена как съеденная и ждет снятия с доски. </summary>
        public bool IsCaptured { get; set; }

        /// <summary> Фигура достигла края и ждет превращения в дамку. </summary>
        public bool IsPromoted { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр фигуры.
        /// </summary>
        /// <param name="group">Принадлежность к стороне.</param>
        /// <param name="type">Начальный тип фигуры.</param>
        public Piece(PieceSide group, PieceType type)
        {
            Side = group;
            Type = type;
        }

        /// <summary>
        /// Обновляет внутреннее состояние фигуры на основе накопленных флагов.
        /// </summary>
        public void Update()
        {
            if (IsPromoted)
            {
                Type = PieceType.King;
                IsPromoted = false;
            }

            // Здесь в будущем можно добавить логику для столбовых шашек:
            // if (HasCapturedPrisioners) { ... }
        }
    }
}
