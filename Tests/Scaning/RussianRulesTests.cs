using Checkers.Engine.Models;
using Checkers.Engine.Rules.Variants;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Tests.Scaning
{
    public class RussianRulesTests
    {
        // Не забудь, что теперь RussianRules требует Scanner в конструкторе (если ты не отвязал его окончательно)
        private readonly RussianRules _rules = new();
        private readonly Piece _whiteMan = new(PieceSide.White, PieceType.Man);

        [Theory]
        [InlineData(RayDirection.UpLeft, true)]   // Вперед для белых — можно
        [InlineData(RayDirection.DownLeft, false)] // Назад для белых — нельзя
        public void Man_ShouldOnlyMoveForward_InDefaultState(RayDirection ray, bool expectedIsPossible)
        {
            // Act: Проверяем тихий ход на дистанции 1 (клетка пуста)
            var verdict = _rules.EvaluateMove(_whiteMan, ray, ScanState.Default, 1, false);

            // Assert
            Assert.Equal(expectedIsPossible, verdict.IsPossible);

            // Важно: если ход возможен (пусто), пешка не должна лететь дальше
            if (verdict.IsPossible)
                Assert.False(verdict.CanContinue);
        }

        [Fact]
        public void Man_ShouldCaptureBackwards()
        {
            // Act: Пешка в состоянии после обнаружения врага (TargetDetected) 
            // на дистанции 2 (клетка приземления), направление назад
            var verdict = _rules.EvaluateMove(_whiteMan, RayDirection.DownLeft, ScanState.TargetDetected, 2, false);

            // Assert: Бить назад в РФ правилах МОЖНО
            Assert.True(verdict.IsPossible);
            Assert.False(verdict.CanContinue); // За пешкой луч всегда прерывается
        }

        [Fact]
        public void Man_ShouldAllowScannerToSeeEnemyAtDistance1()
        {
            // Тест нашей логики (isOccupied && distance == 1)
            // Когда пешка видит фигуру впереди в обычном режиме
            var verdict = _rules.EvaluateMove(_whiteMan, RayDirection.UpLeft, ScanState.Default, 1, true);

            // Assert
            Assert.False(verdict.IsPossible); // Встать на голову нельзя
            Assert.True(verdict.CanContinue);  // НО нужно разрешить сканеру проверить IsEnemy!
        }

        [Fact]
        public void King_ShouldFlyAcrossBoard()
        {
            var whiteKing = new Piece(PieceSide.White, PieceType.King);

            // Дамка в тихом режиме на дистанции 5
            var verdict = _rules.EvaluateMove(whiteKing, RayDirection.UpLeft, ScanState.Default, 5, false);

            // Assert: Дамка может лететь далеко
            Assert.True(verdict.IsPossible);
            Assert.True(verdict.CanContinue);
        }

        [Fact]
        public void IsEnemy_ShouldReturnFalse_WhenTargetAlreadyCaptured()
        {
            // Arrange
            var whiteMan = new Piece(PieceSide.White, PieceType.Man);
            var blackMan = new Piece(PieceSide.Black, PieceType.Man);

            // Помечаем черную фигуру как уже "сбитую" (Турецкий удар)
            blackMan.IsCaptured = true;

            // Act
            bool result = _rules.IsEnemy(whiteMan, blackMan);

            // Assert
            // Несмотря на то, что цвета разные, результат должен быть false,
            // так как нельзя бить одну и ту же фигуру дважды за серию.
            Assert.False(result, "Правила не должны позволять захват уже помеченной фигуры (Турецкий удар)");
        }

        [Fact]
        public void IsEnemy_ShouldReturnTrue_WhenTargetIsNormalEnemy()
        {
            // Arrange
            var whiteMan = new Piece(PieceSide.White, PieceType.Man);
            var blackMan = new Piece(PieceSide.Black, PieceType.Man);

            // Act
            bool result = _rules.IsEnemy(whiteMan, blackMan);

            // Assert
            Assert.True(result, "Нормальный враг должен определяться как валидная цель");
        }

        [Fact]
        public void IsEnemy_ShouldReturnFalse_WhenTargetIsFriend()
        {
            // Arrange
            var whiteMan1 = new Piece(PieceSide.White, PieceType.Man);
            var whiteMan2 = new Piece(PieceSide.White, PieceType.Man);

            // Act
            bool result = _rules.IsEnemy(whiteMan1, whiteMan2);

            // Assert
            Assert.False(result, "Своя фигура не может быть целью для захвата");
        }
    }
}