using Checkers.GameObjects.Scanning;

namespace Checkers.GameObjects.Rules
{
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

    /// <summary>
    /// Технический вердикт о результате завершённой партии.
    /// </summary>
    /// <param name="Status">Итоговое состояние (победа одной из сторон или ничья).</param>
    /// <param name="Reason">Логическое обоснование финала, определённое правилами.</param>
    public record GameResult(GameStatus Status, GameEndReason Reason);

    /// <summary>
    /// Стратегия правил игры, определяющая логику движения, захвата и финализации хода.
    /// </summary>
    public interface IRulesStrategy
    {
        // --- Инициализация ---

        /// <summary>
        /// Возвращает конфигурационные настройки доски (размеры, игровая диагональ), 
        /// специфичные для данного набора правил.
        /// </summary>
        /// <returns>Объект BoardSettings с параметрами геометрии.</returns>
        BoardSettings GetSettings();

        /// <summary>
        /// Формирует коллекцию начальных позиций и типов фигур для старта партии.
        /// </summary>
        /// <returns>Перечисление структур StartPosition для расстановки на доске.</returns>
        IEnumerable<StartPosition> GetInitialPositions();

        // --- Логика сканирования (Ответы для Scanner) ---

        /// <summary>
        /// Проверяет, является ли одна фигура враждебной по отношению к другой.
        /// Используется для определения потенциальных целей для захвата.
        /// </summary>
        bool IsEnemy(Piece actor, Piece target);

        /// <summary>
        /// Оценивает возможность перемещения или прыжка на конкретную клетку луча.
        /// </summary>
        /// <param name="actor">Фигура, совершающая ход.</param>
        /// <param name="ray">Направление луча диагонали.</param>
        /// <param name="state">Текущее состояние луча (накопленные данные сканирования).</param>
        /// <param name="distance">Расстояние от исходной фигуры до текущей точки.</param>
        /// <param name="isOccupied">Флаг, указывающий, занята ли клетка фигурой.</param>
        /// <remarks>
        /// ВАЖНО ДЛЯ РЕАЛИЗАЦИИ:
        /// Сканер обрабатывает клетку в два этапа:
        /// 1. Вызов EvaluateMove: Правила решают, можно ли ПРИЗЕМЛИТЬСЯ здесь (IsPossible) 
        ///    и нужно ли СМОТРЕТЬ ДАЛЬШЕ по лучу (CanContinue).
        /// 2. Если клетка занята и CanContinue == true, сканер проверяет IsEnemy(). 
        ///    При обнаружении врага статус меняется на TargetDetected.
        /// 
        /// Специфика состояний (ScanState) для правил:
        /// - Default: Ищем тихие ходы. Если встретили фигуру, обычно возвращаем (Possible: false, Continue: true), 
        ///   чтобы Scanner мог опознать в ней врага на втором этапе.
        /// - ForcedCaptureOnly: Пропускаем пустые клетки (Possible: false), пока не найдем цель.
        /// - TargetDetected: На предыдущем шаге обнаружен враг. Теперь IsPossible будет true 
        ///   только если текущая клетка ПУСТА (завершение прыжка). 
        /// - CaptureMoveFound: Прыжок через врага уже подтвержден. Для простых шашек здесь обычно 
        ///   CanContinue = false, а для дамок — true (позволяет выбирать клетку приземления дальше по лучу).
        /// </remarks>
        ScanVerdict EvaluateMove(Piece actor, RayDirection ray, ScanState state, int distance, bool isOccupied);

        // --- Логика исполнения (Ответы для Executor/Session) ---

        /// <summary>
        /// Выполняет логику шага: движение, пометку битых и превращения.
        /// </summary>
        /// <returns>
        /// True — дальнейшее сканирование возможно (например если правила позволяют соверша серию взятий). 
        /// False — дальнейшее сканирование не требуется (например если совершен тихий ход).
        /// </returns>
        bool ProcessStep(ITurnActions actions, Move move);

        /// <summary>
        /// Выполняет финальные действия в конце всего хода (массовое снятие фигур, отложенные дамки).
        /// </summary>
        void OnFinalize(ITurnActions actions, Point lastPostion);

        /// <summary>
        /// Решает, что делать, если у текущего игрока нет доступных ходов.
        /// </summary>
        /// <param name="side">Сторона, у которой нет ходов.</param>
        /// <param name="board">Текущее состояние доски.</param>
        TurnResult HandleNoMoves(PieceSide side, Chessboard board);

        /// <summary>
        /// Выносит окончательный технический вердикт о результате игры.
        /// </summary>
        GameResult JudgeTerminalState(PieceSide side, Chessboard board);
    }
}