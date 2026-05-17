using System.Diagnostics.CodeAnalysis;
using Сheckers.GameObjects.Rules;
using Сheckers.GameObjects.Scanning;

namespace Сheckers.GameObjects
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

    /// <summary>
    /// Оркестратор игровой партии. Управляет очерёдностью, историей и целостностью игры.
    /// </summary>
    public class GameSession
    {
        private Chessboard _board;
        private TurnExecutor _executor;
        private readonly IRulesStrategy _rules;
        private readonly MoveScanner _scanner;

        private readonly List<Turn> _history = [];
        private Turn _currentTurn;

        private GameStatus _status = GameStatus.InProgress;
        private GameEndReason _endReason = GameEndReason.None;

        // Кеш сканера
        private List<Move>? _cachedMoves = null;

        /// <summary>
        /// Указывает сторону игрока, чей ход ожидается в данный момент.
        /// </summary>
        public PieceSide ActiveSide => _currentTurn.Side;

        /// <summary>
        /// Возвращает true, если партия завершена (победа одной из сторон или ничья).
        /// </summary>
        public bool IsGameOver => _status != GameStatus.InProgress;

        /// <summary>
        /// Инициализирует новую игровую сессию с заданными правилами и сканером ходов.
        /// Автоматически создает доску и подготавливает первый ход для белых.
        /// </summary>
        /// <param name="rules">Стратегия правил (например, RussianRules).</param>
        /// <param name="scanner">Инструмент для поиска доступных ходов на доске.</param>
        public GameSession(IRulesStrategy rules, MoveScanner scanner)
        {
            _rules = rules;
            _scanner = scanner;
            RebuildBoard();
            // Начинаем с белых (или согласно правилам)
            _currentTurn = new Turn(PieceSide.White);
            _history.Add(_currentTurn);
        }

        #region Public API

        /// <summary>
        /// Возвращает список ходов, доступных игроку в текущей ситуации.
        /// </summary>
        public List<Move> GetValidMoves(PieceSide side)
        {
            if (IsGameOver || side != ActiveSide) return [];

            // Перемещения могут быть найдены на предыдущих этапах (запись ведётся в _cachedMoves)
            return _cachedMoves ??= _currentTurn.Steps is [..,var LastStep] ?
                    _scanner.GetMovesForPiece(_board, _rules, LastStep.To) :
                    _scanner.GetMovesForSide(_board, _rules, side);
        }

        /// <summary>
        /// Выполняет один шаг в рамках текущего хода.
        /// Проверяет валидность действия и при необходимости завершает ход.
        /// </summary>
        public void MakeMove(PieceSide side, Move move)
        {
            if (IsGameOver)
                throw new InvalidOperationException("Действие невозможно: игра уже завершена.");
            if (!GetValidMoves(side).Contains(move))
                throw new InvalidOperationException("Указанный ход не входит в список допустимых перемещений.");

            // Выполнение перемещения
            bool chainAllowed = _rules.ProcessStep(_executor, move);
            _currentTurn.Steps.Add(move);

            // Сбрасываем устаревший кэш
            _cachedMoves = null;

            // Если серия по правилам не может быть продолжена или нет доступных ходов для продолжения
            if (chainAllowed is false || GetValidMoves(side) is [])
            {
                _rules.OnFinalize(_executor, _currentTurn.Steps[^1].To);
                _currentTurn.IsCompleted = true;
                PrepareNextTurn();
            }
        }

        /// <summary>
        /// Отменяет последний совершенный шаг в рамках текущего хода.
        /// </summary>
        public void Undo()
        {
            if (IsGameOver) return;

            // Вариант 1: Откат шага внутри текущей серии прыжков
            if (_currentTurn.Steps.Count > 0)
            {
                _currentTurn.Steps.RemoveAt(_currentTurn.Steps.Count - 1);
                RebuildBoard();
            }

            // Вариант 2: Откат к завершенному ходу противника
            // TODO: Внедрить проверку прав доступа (разрешено ли игроку отменять чужой ход)
            /*
            else if (_history.Count > 1)
            {
                // 1. Удаляем текущий (пустой) ход из истории
                _history.RemoveAt(_history.Count - 1);

                // 2. Возвращаемся к последнему завершенному ходу
                _currentTurn = _history[^1];

                // 3. Снимаем флаг завершения, чтобы правила OnFinalize могли отработать снова
                _currentTurn.IsCompleted = false;

                // 4. Пересобираем доску (кэш сбросится внутри RebuildBoard)
                RebuildBoard();
            }
            */
        }

        #endregion

        #region методы для интеграции в Server

        /// <summary>
        /// Формирует актуальную информацию о состоянии сессии для передачи внешним слоям (UI/API).
        /// </summary>
        /// <returns>Объект SessionInfo, содержащий срез данных для отрисовки кадра игры.</returns>
        public SessionInfo GetInfo() => new(
            ActiveSide,
            _status,
            _endReason,
            _currentTurn.Steps.Count > 0,
            _board.GetGridSnapshot());

        /// <summary>
        /// Создает снимок данных, необходимый для сохранения состояния игры в базе данных или файле.
        /// </summary>
        /// <returns>Объект GameSnapshot с полной копией истории ходов.</returns>
        public GameSnapshot GetSnapshot() => new(
            _rules.GetType().Name,
            [.. _history]
        );

        /// <summary>
        /// Создает копию сетки игрового поля для отрисовки
        /// </summary>
        public Piece?[,] GetBoardSnapshot() => _board.GetGridSnapshot();

        /// <summary>
        /// Восстанавливает игровую сессию из истории ходов.
        /// </summary>
        /// <param name="history">Список совершенных ходов.</param>
        /// <param name="rules">Стратегия правил.</param>
        /// <param name="scanner">Сканер ходов.</param>
        /// <exception cref="ArgumentException">Бросается, если история пуста.</exception>
        public static GameSession Restore(List<Turn> history, IRulesStrategy rules, MoveScanner scanner)
        {
            if (history is [])
                throw new ArgumentException("Невозможно восстановить сессию: история ходов пуста.", nameof(history));

            var session = new GameSession(rules, scanner);
            session._history.Clear();
            session._history.AddRange(history);

            var lastTurn = session._history[^1];
            if (lastTurn.IsCompleted) session.PrepareNextTurn();
            else session._currentTurn = lastTurn;

            session.RebuildBoard();

            return session;
        }

        #endregion

        #region Private Logic

        /// <summary>
        /// Синхронизирует физическое состояние доски с историей ходов.
        /// Полностью пересоздает игровое поле и проигрывает все совершенные действия.
        /// </summary>
        [MemberNotNull(nameof(_board), nameof(_executor))]
        private void RebuildBoard()
        {
            var settings = _rules.GetSettings();
            _board = new Chessboard(settings.Rows, settings.Cols, settings.UseEvenSquares);
            _executor = new TurnExecutor(_board);
            _cachedMoves = null;

            // Расстановка фигур согласно начальной позиции правил
            foreach (var (square, side, type) in _rules.GetInitialPositions())
            {
                _board.PlacePiece(square, new(side, type));
            }

            // Воспроизведение цепочки ходов
            foreach (var turn in _history)
            {
                foreach (var move in turn.Steps)
                {
                    _rules.ProcessStep(_executor, move);
                }

                if (turn is { IsCompleted: true, Steps: [.., var lastStep] })
                {
                    _rules.OnFinalize(_executor, lastStep.To);
                }
            }
        }

        /// <summary>
        /// Подготавливает систему к ходу следующего игрока.
        /// </summary>
        private void PrepareNextTurn()
        {
            // Сбрасываем устаревший кэш
            _cachedMoves = null;
            var nextSide = _currentTurn.Side == PieceSide.White ? PieceSide.Black : PieceSide.White;

            _currentTurn = new Turn(nextSide);
            _history.Add(_currentTurn);

            if (GetValidMoves(nextSide) is [])
            {
                // ПРОВЕРКА НА ВЗАИМНЫЙ ПАТ: 
                // Если в истории уже есть как минимум два хода, и предыдущий тоже был без шагов
                if (_history.Count >= 2 && _history[^2].Steps is [])
                {
                    _status = GameStatus.Draw;
                    _endReason = GameEndReason.MutualBlock;
                    return;
                }

                switch (_rules.HandleNoMoves(nextSide, _board))
                {
                    case TurnResult.SwitchSide:
                        PrepareNextTurn();
                        break;

                    case TurnResult.GameFinished:
                        var final = _rules.JudgeTerminalState(nextSide, _board);
                        _status = final.Status;
                        _endReason = final.Reason;
                        break;
                }
            }
        }

        #endregion
    }
}