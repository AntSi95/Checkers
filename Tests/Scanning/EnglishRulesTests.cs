using Checkers.Engine.Models;
using Checkers.Engine.Rules.Variants;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Tests.Scanning
{
    public class EnglishRulesTests
    {
        private readonly EnglishRules _rules = new();
        private readonly Piece _whiteMan = new(PieceSide.White, PieceType.Man);
        private readonly Piece _whiteKing = new(PieceSide.White, PieceType.King);

        [Fact]
        public void Man_ShouldNotCaptureBackwards()
        {
            // Ситуация: Белая пешка "видит" врага сзади (DownLeft)
            var ray = RayDirection.DownLeft; // Назад для белых

            // Act
            // В Чекерсе пешка для луча назад возвращает (false, false) сразу
            var verdict = _rules.EvaluateMove(_whiteMan, ray, ScanState.TargetDetected, 2, false);

            // Assert
            Assert.False(verdict.IsPossible);
            Assert.False(verdict.CanContinue);
        }

        [Fact]
        public void King_ShouldNotFly()
        {
            // Ситуация: Дамка (King) пытается пойти тихо на 2 клетки вперед
            // Act
            var verdict = _rules.EvaluateMove(_whiteKing, RayDirection.UpLeft, ScanState.Default, 2, false);

            // Assert: В Чекерсе дамка — это короткоходная фигура (dist 1)
            Assert.False(verdict.IsPossible);
            Assert.False(verdict.CanContinue); // На dist 2 тихий ход дамки невозможен и луч прерван
        }

        [Fact]
        public void Man_ShouldCaptureForward()
        {
            // Ситуация: Белая пешка прыгает вперед (dist 2) на пустую клетку
            var ray = RayDirection.UpLeft; // Вперед для белых

            // Act
            var verdict = _rules.EvaluateMove(_whiteMan, ray, ScanState.TargetDetected, 2, false);

            // Assert: Вперед бить можно, но после прыжка пешка всегда останавливается
            Assert.True(verdict.IsPossible);
            Assert.False(verdict.CanContinue);
        }

        [Theory]
        [InlineData(ScanState.Default)]
        [InlineData(ScanState.ForcedCaptureOnly)]
        public void ForcedCapture_ShouldBlockSilentMoves(ScanState state)
        {
            // Ситуация: Пешка на расстоянии 1, впереди пусто, направление вперед
            bool isOccupied = false;

            // Act
            var verdict = _rules.EvaluateMove(_whiteMan, RayDirection.UpLeft, state, 1, isOccupied);

            // Assert
            if (state == ScanState.Default)
                Assert.True(verdict.IsPossible); // Тихий ход разрешен
            else
                Assert.False(verdict.IsPossible); // В режиме боя тихий ход ЗАПРЕЩЕН
        }

        [Fact]
        public void King_IsShortRange_LikeMan()
        {
            // Проверка, что дамка в Чекерсе не летает через 3 клетки
            var verdict = _rules.EvaluateMove(_whiteKing, RayDirection.UpLeft, ScanState.Default, 3, false);

            Assert.False(verdict.IsPossible);
            Assert.False(verdict.CanContinue);
        }
    }
}
