using System.Diagnostics;
using Checkers.Engine.Models;

namespace Checkers.Engine.Core
{
    // TODO: Архитектурная оптимизация доступа к доске (Alpha -> Beta):
    // 1. Инкапсуляция: Сделать Chessboard 'internal', чтобы скрыть физику доски от пользователя библиотеки.
    // 2. Унификация действий: Рассмотреть реализацию ITurnActions прямо в Chessboard (или через прокси), 
    //    чтобы Executor не дублировал методы мутации.
    // 3. Безопасность (Read-only): Ввести интерфейс IBoardInspector (только геттеры и проверки), 
    //    который заменит прямую передачу Chessboard в методы IRulesStrategy (HandleNoMoves, JudgeTerminalState). 
    //    Это исключит риск случайного изменения доски во время судейства.
    /// <summary>
    /// Представляет игровое поле для игры в шашки в виде матрицы.
    /// Отвечает за геометрию доски и физическое манипулирование фигурами.
    /// </summary>
    internal sealed class Chessboard : ITurnExecution, IBoardInspection, IBoardNavigation
    {
        /// <summary> Количество рядов на доске. </summary>
        public int Rows { get; }
        /// <summary> Количество столбцов на доске. </summary>
        public int Cols { get; }

        private readonly Piece?[,] _squares;
        private readonly int _party; // чётность игровой диагонали (черные клетки или белые)

        /// <summary>
        /// Возвращает фигуру в указанной клетке. 
        /// Если клетка вне доски или пуста, возвращает null.
        /// </summary>
        public Piece? this[Point square] => IsInBounds(square) ? _squares[square.Row, square.Col] : null;

        /// <summary>
        /// Создает игровое поле с заданными размерами.
        /// </summary>
        /// <param name="rows">Количество рядов.</param>
        /// <param name="cols">Количество столбцов.</param>
        /// <param name="useEvenSquares">Определяет, являются ли игровые клетки четными.</param>
        public Chessboard(int rows, int cols, bool useEvenSquares = true)
        {
            Rows = rows;
            Cols = cols;
            _squares = new Piece[Rows, Cols];
            _party = useEvenSquares ? 0 : 1;
        }

        #region Проверка геометрии

        /// <summary> Проверяет, находится ли точка в пределах массива доски. </summary>
        private bool IsInBounds(Point p) => p.Row >= 0 && p.Row < Rows && p.Col >= 0 && p.Col < Cols;

        /// <summary> Проверяет, является ли клетка игровой (попадание в границы и правильный цвет). </summary>
        public bool IsValidPoint(Point p) => IsInBounds(p) && (p.Row + p.Col) % 2 == _party;

        #endregion

        #region Методы для получения данных

        /// <summary>
        /// Пытается получить фигуру. Возвращает false, если клетка не относится к игровому полю.
        /// </summary>
        public bool TryGetPiece(Point square, out Piece? piece)
        {
            piece = null;
            if (!IsValidPoint(square)) return false;
            piece = _squares[square.Row, square.Col];
            return true;
        }

        /// <summary>
        /// Возвращает перечисление всех игровых клеток доски.
        /// </summary>
        public IEnumerable<Point> GetValidSquares()
        {
            for (int row = 0; row < Rows; row++)
            {
                int startCol = (row + _party) % 2;
                for (int col = startCol; col < Cols; col += 2)
                    yield return new Point(row, col);
            }
        }

        /// <summary>
        /// Создает копию сетки доски для передачи на фронтенд или сохранения.
        /// </summary>
        public Piece?[,] GetGridSnapshot()
        {
            var snapshot = new Piece?[Rows, Cols];
            Array.Copy(_squares, snapshot, _squares.Length);
            return snapshot;
        }

        /// <summary>
        /// Определяет, является ли клетка крайней линией для превращения в дамку для указанной стороны.
        /// </summary>
        /// <param name="square">Проверяемая клетка.</param>
        /// <param name="side">Сторона фигуры.</param>
        public bool IsPromotionEdge(Point square, PieceSide side)
        {
            // Физическая система (0,0)=A1: Белые идут вверх к Rows-1, черные вниз к Row 0
            return side == PieceSide.White ? square.Row == Rows - 1 : square.Row == 0;
        }

        #endregion

        #region Методы изименения состояния

        /// <summary>
        /// Выполняет перемещение фигуры на основе объекта Move.
        /// </summary>
        public void Execute(Move move) => Move(move.From, move.To);

        /// <summary>
        /// Перемещает фигуру между клетками.
        /// </summary>
        /// <param name="from">Исходная клетка.</param>
        /// <param name="to">Целевая клетка.</param>
        /// <exception cref="InvalidOperationException">Если в начальной точке нет фигуры.</exception>
        /// <exception cref="ArgumentException">Если целевая клетка недопустима.</exception>
        public void Move(Point from, Point to)
        {
            var piece = this[from] ?? throw new InvalidOperationException($"Ошибка перемещения: в клетке {from} нет фигуры.");

            if (!IsValidPoint(to))
                throw new ArgumentException($"Точка {to} не является игровой клеткой для этой доски.");

            Debug.Assert(this[to] == null, "Критическая ошибка: попытка перемещения в уже занятую клетку.");

            _squares[to.Row, to.Col] = piece;
            _squares[from.Row, from.Col] = null;
        }

        /// <summary>
        /// Устанавливает фигуру в клетку. Используется для тестов и начальной расстановки.
        /// </summary>
        public void PlacePiece(Point square, Piece piece)
        {
            if (!IsValidPoint(square))
                throw new ArgumentException($"Невозможно разместить фигуру на неигровой клетке {square}.");

            _squares[square.Row, square.Col] = piece;
        }

        /// <summary>
        /// Удаляет фигуру из указанной клетки.
        /// </summary>
        public void Remove(Point square)
        {
            if (IsInBounds(square))
                _squares[square.Row, square.Col] = null;
        }

        /// <summary>
        /// Изменяет тип фигуры (например, превращение в дамку).
        /// </summary>
        public void Transform(Point square, PieceType newType = PieceType.King)
        {
            var piece = this[square] ?? throw new InvalidOperationException($"Ошибка трансформации: в клетке {square} нет фигуры.");
            piece.Type = newType;
        }

        /// <summary>
        /// Помечает фигуру в указанной клетке как взятую (съеденную).
        /// Сама фигура остается на доске до вызова метода Remove.
        /// </summary>
        public void MarkAsCaptured(Point square)
        {
            var piece = this[square] ?? throw new InvalidOperationException($"Ошибка захвата: в клетке {square} нет фигуры.");
            piece.IsCaptured = true;
        }

        /// <summary>
        /// Удаляет с доски все фигуры, которые были помечены как взятые (IsCaptured).
        /// </summary>
        public void RemoveCapturedPieces()
        {
            foreach (var square in GetValidSquares())
            {
                var piece = this[square];
                if (piece != null && piece.IsCaptured)
                {
                    _squares[square.Row, square.Col] = null;
                }
            }
        }

        /// <summary>
        /// Помечает фигуру как готовую к превращению в дамку.
        /// </summary>
        public void MarkAsPromoted(Point square)
        {
            var piece = this[square] ?? throw new InvalidOperationException($"Ошибка повышения: в клетке {square} нет фигуры.");
            piece.IsPromoted = true;
        }

        /// <summary>
        /// Системный метод очистки состояния доски. 
        /// Удаляет битые фигуры и сбрасывает все временные флаги состояния у оставшихся фигур.
        /// </summary>
        public void SettleTurn()
        {
            foreach (var square in GetValidSquares())
            {
                var piece = this[square];
                if (piece == null) continue;

                piece.Update();

                if (piece.IsCaptured)
                    _squares[square.Row, square.Col] = null;
            }
        }

        #endregion

        #region ITurnExecution

        void ITurnExecution.ApplyMove(Move move) => Execute(move);
        void ITurnExecution.ApplyCaptureMark(Point target) => MarkAsCaptured(target);
        void ITurnExecution.ApplyPromotion(Point target) => Transform(target);
        void ITurnExecution.ApplyPromotionMark(Point target) => MarkAsPromoted(target);
        void ITurnExecution.ApplyRemoval() => RemoveCapturedPieces();
        void ITurnExecution.ApplyRemoval(Point target) => Remove(target);
        void ITurnExecution.ApplyFinalEffects() => SettleTurn();
        bool ITurnExecution.CanPromote(Point square)
        {
            TryGetPiece(square, out var piece);
            return piece is not null && IsPromotionEdge(square, piece.Side);
        }

        #endregion
    }
}
