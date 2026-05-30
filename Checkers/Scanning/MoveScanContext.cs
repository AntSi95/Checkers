using System.Diagnostics;
using Checkers.Engine.Core;
using Checkers.Engine.Models;
using Checkers.Engine.Rules;

namespace Checkers.Engine.Scanning
{
    /// <summary>
    /// Режим поиска доступных перемещений.
    /// </summary>
    public enum ScanMode
    {
        /// <summary> Искать все типы ходов (тихие и взятия). </summary>
        All,

        /// <summary> Искать исключительно ходы с захватом фигур. </summary>
        CapturesOnly
    }

    /// <summary>
    /// Состояния конечного автомата при сканировании луча диагонали.
    /// </summary>
    public enum ScanState
    {
        /// <summary> Исходное состояние. Допускаются тихие ходы. </summary>
        Default,

        /// <summary> Режим принудительного поиска захвата. Тихие ходы игнорируются. </summary>
        ForcedCaptureOnly,

        /// <summary> На луче обнаружена фигура противника (потенциальная цель). </summary>
        TargetDetected,

        /// <summary> Зафиксирован факт перепрыгивания через цель (взятие возможно). </summary>
        CaptureMoveFound,

        /// <summary> Сканирование луча завершено (препятствие, край доски или предел дальности). </summary>
        Terminated
    }

    /// <summary>
    /// Контекст состояния процесса сканирования ходов.
    /// </summary>
    /// <remarks>
    /// Реализован как <c>class</c> для обеспечения эффективной мутации состояния в глубоких циклах поиска.
    /// Инкапсулирует в себе три уровня ответственности:
    /// 1. Геометрический контроль (текущий луч, дистанция, координаты).
    /// 2. Автомат состояний сканирования (обнаружение целей, подтверждение приземлений).
    /// 3. Агрегация результатов (фильтрация и накопление списка валидных перемещений).
    /// </remarks>
    internal class MoveScanContext
    {
        public Chessboard Board { get; }
        public IRulesStrategy Rules { get; }
        /// <summary>
        /// Список найденных ходов, накапливает в себе ходы при каждой смене проверяемого направления или фигуры
        /// </summary>
        public List<Move> FoundMoves { get; } = [];
        public bool IsCaptureMode { get; private set; }

        public Point StartSquare { get; private set; }
        public Piece? ScanningPiece { get; private set; } = null;

        public RayDirection CurrentRay { get; set; }
        public ScanState State { get; private set; }
        public int Distance { get; set; } = 0;
        /// <summary>
        /// ВНИМАНИЕ: Свойство возвращает StartSquare, пока не вызван StepForward().
        /// Всегда вызывайте StepForward() перед началом анализа новой клетки.
        /// </summary>
        public Point CurrentSquare { get; set; }
        public Piece? CurrentPiece { get; private set; } = null;

        /// <summary>
        /// Координата фигуры которую, можно взять.
        /// </summary>
        public Point? TempCapturedSquare { get; private set; }

        private Point _step;


        public MoveScanContext(Chessboard board, IRulesStrategy rules, ScanMode mode)
        {
            Board = board;
            Rules = rules;
            IsCaptureMode = (mode == ScanMode.CapturesOnly);
        }

        /// <summary>
        /// Переключает контекст на сканирование новой фигуры.
        /// </summary>
        /// <param name="square">Координаты клетки с фигурой.</param>
        /// <returns><c>true</c>, если фигура успешно найдена; иначе <c>false</c>.</returns>
        public bool TrySwitchScanningPiece(Point square)
        {
            if (!Board.TryGetPiece(square, out Piece? piece) || piece is null)
            {
                ScanningPiece = null;
                return false;
            }
            StartSquare = square;
            ScanningPiece = piece;
            ResetRay();
            return true;
        }

        /// <summary>
        /// Подготавливает параметры для сканирования нового луча (диагонали).
        /// Сбрасывает дистанцию и временные координаты цели.
        /// </summary>
        public void PrepareScanningRay(RayDirection ray)
        {
            //избыточно так как все вызовы контролируют правильность ввода
            //if (ray == RayDirection.None)
            //    throw new ArgumentException("Необходимо указать конкретный луч.");

            ResetRay();

            CurrentRay = ray;
            _step = ray.GetVector();
            CurrentSquare = StartSquare;
        }

        /// <summary>
        /// Пытается совершить шаг вперед по вектору текущего луча.
        /// Обновляет текущую клетку, дистанцию и обнаруженную фигуру.
        /// </summary>
        /// <returns><c>true</c>, если шаг совершен успешно; <c>false</c>, если достигнут край доски.</returns>
        public bool TryStepForward()
        {
            var nextSquare = CurrentSquare + _step;

            if (!Board.TryGetPiece(nextSquare, out Piece? piece))
                return false;

            CurrentPiece = piece;
            CurrentSquare = nextSquare;
            Distance++;
            return true;
        }

        /// <summary>
        /// Фиксирует обнаружение вражеской фигуры на пути луча. 
        /// Переводит автомат состояний в режим ожидания клетки для приземления.
        /// </summary>
        public void ConfirmCaptureTarget()
        {
            Debug.Assert(State == ScanState.Default || State == ScanState.ForcedCaptureOnly,
                "Нельзя захватить цель, если луч уже в режиме прыжка или завершен.");
            TempCapturedSquare = CurrentSquare;
            State = ScanState.TargetDetected;
        }

        /// <summary>
        /// Подтверждает возможность завершения прыжка в текущей пустой клетке.
        /// Переводит автомат в состояние найденного взятия.
        /// </summary>
        public void ConfirmLanding()
        {
            Debug.Assert(State == ScanState.TargetDetected,
                "Нельзя подтвердить приземление, не обнаружив цель (врага).");
            State = ScanState.CaptureMoveFound;
        }

        /// <summary>
        /// Подтверждает завершение работы с текущим лучом.
        /// Переводит автомат в состояние конца проверки луча.
        /// </summary>
        public void ConfirmTermination()
        {
            State = ScanState.Terminated;
        }

        /// <summary>
        /// Создает объект <see cref="Move"/> на основе текущего состояния луча и сохраняет его в общий список.
        /// Реализует логику приоритета боя: удаляет тихие ходы при обнаружении первого взятия.
        /// </summary>
        public void SaveFoundMove()
        {
            bool isCapture = TempCapturedSquare.HasValue;

            // Игнорируем тихий ход, если в этой сессии уже были бои
            if (IsCaptureMode && !isCapture)
                return;

            // Нашли первый бой в сессии — тотальная чистка записанных тихих ходов. В списке сейчас храняться только тихие ходы.
            if (isCapture && !IsCaptureMode)
            {
                IsCaptureMode = true;
                FoundMoves.Clear();
            }

            //Добавляем ход (либо это тихий к тихим, либо бой к боям)
            FoundMoves.Add(new Move(StartSquare, CurrentSquare, TempCapturedSquare));
        }

        /// <summary>
        /// Сбрасывает параметры итератора луча (дистанцию, текущую клетку, цель) перед началом нового поиска.
        /// </summary>
        private void ResetRay()
        {
            CurrentRay = RayDirection.None;
            _step = default;
            Distance = 0;
            TempCapturedSquare = null;

            State = IsCaptureMode ? ScanState.ForcedCaptureOnly : ScanState.Default;
        }

        // TODO [Optimization]: Активировать и интегрировать этот метод при переходе к глубокому сканированию, при необходимости
        ///// <summary>
        ///// Полностью очищает контекст и готовит его к новой независимой сессии поиска ходов.
        ///// </summary>
        ///// <remarks>
        ///// Позволяет повторно использовать существующий экземпляр контекста для другой фигуры,
        ///// минимизируя нагрузку на сборщик мусора (GC) в глубоких циклах поиска.
        ///// </remarks>
        //public void RestartScanning(Point startSquare, ScanMode mode)
        //{
        //    FoundMoves.Clear();
        //    ResetRay();

        //    if (!TrySwitchScanningPiece(startSquare))
        //        throw new ArgumentException("Стартовая клетка не содержит валидной фигуры.");
        //}
    }
}
