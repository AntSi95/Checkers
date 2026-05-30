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
}
