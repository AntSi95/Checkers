using Moq;
using Checkers.GameObjects.Scanning;
using Checkers.GameObjects;
using Checkers.GameObjects.Rules;

namespace Checkers.Tests.Scanning
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
            _board = new Chessboard(8, 8);
            _rulesMock = new Mock<IRulesStrategy>();
            _whiteMan = new Piece(PieceSide.White, PieceType.Man);

            // Расставляем стартовую фигуру
            _board.PlacePiece(_start, _whiteMan);
        }

        [Fact]
        public void TryStepForward_MovesCorrectly_AndIncrementsDistance()
        {
            // Arrange
            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(_start);
            ctx.PrepareScanningRay(RayDirection.UpLeft); // Вектор (-1, -1)

            // Act
            bool success = ctx.TryStepForward();

            // Assert
            Assert.True(success);
            Assert.Null(ctx.CurrentPiece); // На (3,3) пусто
            Assert.Equal(new Point(3, 3), ctx.CurrentSquare);
            Assert.Equal(1, ctx.Distance);
        }

        [Fact]
        public void TryStepForward_ReturnsFalse_AtBoardEdge()
        {
            // Arrange
            Point edge = new Point(0, 0);
            _board.PlacePiece(edge, _whiteMan);

            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(edge);
            ctx.PrepareScanningRay(RayDirection.UpLeft); // Шаг за край в (-1, -1)

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
            ctx.PrepareScanningRay(RayDirection.UpLeft);

            // Шагаем на врага
            var enemyPos = new Point(3, 3);
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

            // 1. Добавляем тихий ход
            ctx.PrepareScanningRay(RayDirection.UpLeft);
            ctx.TryStepForward(); // (3,3)
            ctx.SaveFoundMove();

            Assert.Single(ctx.FoundMoves);
            Assert.False(ctx.IsCaptureMode);

            // 2. Находим бой на другом луче
            var enemyPos = new Point(5, 5);
            _board.PlacePiece(enemyPos, new Piece(PieceSide.Black, PieceType.Man));

            ctx.PrepareScanningRay(RayDirection.DownRight);
            ctx.TryStepForward(); // (5,5) - враг
            ctx.ConfirmCaptureTarget();
            ctx.TryStepForward(); // (6,6) - приземление
            ctx.ConfirmLanding();

            // Act
            ctx.SaveFoundMove();

            // Assert
            Assert.True(ctx.IsCaptureMode);
            Assert.Single(ctx.FoundMoves); // Тихий ход удален из списка
            Assert.Equal(enemyPos, ctx.FoundMoves[0].Target); // Остался только бой
        }

        [Fact]
        public void PrepareScanningRay_SwitchesToForced_IfIsCaptureMode()
        {
            // Arrange
            var ctx = new MoveScanContext(_board, _rulesMock.Object, ScanMode.All);
            ctx.TrySwitchScanningPiece(_start);

            // Имитируем, что режим боя уже включен (через свойство или первый бой)
            // Для теста просто найдем один бой
            _board.PlacePiece(new Point(3, 3), new Piece(PieceSide.Black, PieceType.Man));
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
            // так как в этой сессии уже найден бой.
            Assert.Equal(ScanState.ForcedCaptureOnly, ctx.State);
        }
    }
}
