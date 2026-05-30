using Moq;
using Checkers.Engine.Core;
using Checkers.Engine.Models;
using Checkers.Engine.Rules;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Tests.Scanning
{
    public class MoveScanContextTests
    {
        private readonly Chessboard _board;
        private readonly Mock<IRulesStrategy> _rulesMock;
        private readonly Point _start = new(4, 4);
        private readonly Piece _whiteMan;

        public MoveScanContextTests()
        {
            // Используем 8x8 по умолчанию
            _board = new Chessboard(8, 8, useEvenSquares: true);
            _rulesMock = new Mock<IRulesStrategy>();
            _whiteMan = new Piece(PieceSide.White, PieceType.Man);

            // Расставляем стартовую фигуру
            _board.PlacePiece(_start, _whiteMan);
        }

        [Fact]
        public void TryStepForward_MovesCorrectly_AndIncrementsDistance()
        {
            // Arrange
            // Передаем доску, так как она нативно реализует IBoardNavigation
            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(_start);
            ctx.PrepareScanningRay(RayDirection.UpLeft); // Вектор (+1, -1) по физической модели

            // Act
            bool success = ctx.TryStepForward();

            // Assert
            Assert.True(success);
            Assert.Null(ctx.CurrentPiece); // На (5,3) пусто
            Assert.Equal(new Point(5, 3), ctx.CurrentSquare);
            Assert.Equal(1, ctx.Distance);
        }

        [Fact]
        public void TryStepForward_ReturnsFalse_AtBoardEdge()
        {
            // Arrange
            // Использовали нижний левый игровой угол (0, 0).
            var edge = new Point(0, 0);
            _board.PlacePiece(edge, _whiteMan);

            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(edge);
            ctx.PrepareScanningRay(RayDirection.UpLeft); // Шаг за левый край в (1, -1)

            // Act
            bool success = ctx.TryStepForward();

            // Assert
            Assert.False(success);
            Assert.Equal(0, ctx.Distance); // Дистанция не увеличилась
            Assert.Equal(edge, ctx.CurrentSquare); // Остались на месте
        }


        [Fact]
        public void ConfirmCaptureTarget_SetsSquare_AndChangesState()
        {
            // Arrange
            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(_start);
            ctx.PrepareScanningRay(RayDirection.UpLeft); // Вектор (+1, -1)

            // Шагаем на врага, который стоит впереди на (5, 3)
            var enemyPos = new Point(5, 3);
            _board.PlacePiece(enemyPos, new Piece(PieceSide.Black, PieceType.Man));
            ctx.TryStepForward();

            // Act
            ctx.ConfirmCaptureTarget();

            // Assert
            Assert.Equal(ScanState.TargetDetected, ctx.State);
            Assert.Equal(enemyPos, ctx.TempCapturedSquare);
        }

        [Fact]
        public void SaveFoundMove_ClearsSilent_WhenFirstCaptureFound()
        {
            // Arrange
            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(_start);

            // 1. Добавляем тихий ход вверх-влево на (5, 3)
            ctx.PrepareScanningRay(RayDirection.UpLeft);
            ctx.TryStepForward();
            ctx.SaveFoundMove();

            Assert.Single(ctx.FoundMoves);
            Assert.False(ctx.IsCaptureMode);

            // 2. Находим бой на другом луче DownRight (вектор -1, +1)
            // Ставим врага сзади-справа на (3, 5)
            var enemyPos = new Point(3, 5);
            _board.PlacePiece(enemyPos, new Piece(PieceSide.Black, PieceType.Man));

            ctx.PrepareScanningRay(RayDirection.DownRight);
            ctx.TryStepForward(); // (3,5) - враг обнаружен
            ctx.ConfirmCaptureTarget();
            ctx.TryStepForward(); // (2,6) - пустая клетка приземления
            ctx.ConfirmLanding();

            // Act
            ctx.SaveFoundMove();

            // Assert
            Assert.True(ctx.IsCaptureMode);
            Assert.Single(ctx.FoundMoves); // Старый тихий ход стерт из списка
            Assert.Equal(enemyPos, ctx.FoundMoves[0].Target); // В списке остался только чистый бой
        }

        [Fact]
        public void PrepareScanningRay_SwitchesToForced_IfIsCaptureMode()
        {
            // Arrange
            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(_start);

            // Имитируем, что режим боя уже включен (находим один валидный бой на луче)
            _board.PlacePiece(new Point(5, 3), new Piece(PieceSide.Black, PieceType.Man));
            ctx.PrepareScanningRay(RayDirection.UpLeft);
            ctx.TryStepForward();
            ctx.ConfirmCaptureTarget();
            ctx.TryStepForward();
            ctx.ConfirmLanding();
            ctx.SaveFoundMove();

            // Act
            ctx.PrepareScanningRay(RayDirection.UpRight);

            // Assert
            // При подготовке НОВОГО луча состояние должно стать ForcedCaptureOnly,
            // так как в рамках этой сессии сканирования на доске уже найден обязательный бой.
            Assert.Equal(ScanState.ForcedCaptureOnly, ctx.State);
        }
    }
}
