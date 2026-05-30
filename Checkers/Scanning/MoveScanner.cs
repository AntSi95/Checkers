using Checkers.Engine.Models;
using Checkers.Engine.Rules;

namespace Checkers.Engine.Scanning
{
    /// <summary>
    /// Механизм поиска доступных ходов, производящий последовательное сканирование клеток/диагоналей/фигур.
    /// </summary>
    public class MoveScanner
    {
        /// <summary>
        /// Ищем доступные ходы для продолжения серии боев.
        /// </summary>
        public List<Move> GetMovesForPiece(IBoardNavigation board, IRulesStrategy rules, Point square)
        {
            var context = new MoveScanContext(board, rules, ScanMode.CapturesOnly);

            if (context.TrySwitchScanningPiece(square))
                ScanAllDirections(context);

            return context.FoundMoves;
        }

        /// <summary>
        /// Ищем всех доступные ходы для выбранной стороны.
        /// </summary>
        public List<Move> GetMovesForSide(IBoardNavigation board, IRulesStrategy rules, PieceSide side)
        {
            var context = new MoveScanContext(board, rules, ScanMode.All);

            foreach (var square in board.GetValidSquares())
            {
                // Находим фигуру для сканирования и проверяем, принадлежит ли она игроку
                if (context.TrySwitchScanningPiece(square) && context.ScanningPiece?.Side == side)
                {
                    ScanAllDirections(context);
                }
            }
            return context.FoundMoves;
        }

        private void ScanAllDirections(MoveScanContext context)
        {
            foreach (RayDirection ray in RayDirectionExtensions.AllDiagonals)
            {
                context.PrepareScanningRay(ray);
                ScanRay(context);
            }
        }

        private void ScanRay(MoveScanContext context)
        {
            // Цикл шагает по лучу, пока он не терминирован
            while (context.State != ScanState.Terminated && context.TryStepForward())
            {
                AnalyzeSquare(context);
            }

            // Если вышли из цикла по причине края доски (TryStepForward == false)
            if (context.State != ScanState.Terminated)
                context.ConfirmTermination();
        }

        private void AnalyzeSquare(MoveScanContext context)
        {
            bool isOccupied = (context.CurrentPiece != null);

            // 1. Оцениваем клетку через правила
            var verdict = context.Rules.EvaluateMove(
                context.ScanningPiece!,
                context.CurrentRay,
                context.State,
                context.Distance,
                isOccupied
            );

            // 2. Если правила разрешают здесь остановиться - сохраняем ход
            if (verdict.IsPossible)
            {
                // Если мы уже "перепрыгнули" врага и встали на пустую клетку - подтверждаем приземление
                if (context.State == ScanState.TargetDetected && !isOccupied)
                    context.ConfirmLanding();

                context.SaveFoundMove();
            }

            // 3. Если правила велели прервать луч (препятствие или предел дальности)
            if (!verdict.CanContinue)
            {
                context.ConfirmTermination();
                return;
            }

            // 4. Если клетка занята, определяем её роль для автомата состояний
            if (isOccupied)
            {
                if (context.Rules.IsEnemy(context.ScanningPiece!, context.CurrentPiece!))
                    context.ConfirmCaptureTarget();
                else
                    context.ConfirmTermination(); // Фигура, которую нельзя взять блокирует путь
            }
        }
    }
}
