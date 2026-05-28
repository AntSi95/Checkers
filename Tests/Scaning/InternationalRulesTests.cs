using Checkers.GameObjects;
using Checkers.GameObjects.Rules.Checkers.Core.Rules;
using Checkers.GameObjects.Scanning;

namespace Checkers.Tests.Scaning
{
    public class InternationalRulesTests
    {
        private readonly InternationalRules _rules = new();
        private readonly Piece _whiteMan = new(PieceSide.White, PieceType.Man);

        [Fact]
        public void Board_ShouldBe10x10()
        {
            // Act
            var settings = _rules.GetSettings();

            // Assert
            Assert.Equal(10, settings.Rows);
            Assert.Equal(10, settings.Cols);
        }

        [Fact]
        public void Man_ShouldOnlyMoveForward_ButCaptureAnywhere()
        {
            // 1. Тихий ход назад (Down) для белых
            var silentBack = _rules.EvaluateMove(_whiteMan, RayDirection.DownLeft, ScanState.Default, 1, false);
            Assert.False(silentBack.IsPossible, "Простая шашка не может ходить назад тихо");

            // 2. Бой назад (TargetDetected на dist 2)
            var captureBack = _rules.EvaluateMove(_whiteMan, RayDirection.DownLeft, ScanState.TargetDetected, 2, false);
            Assert.True(captureBack.IsPossible, "В международных шашках простая шашка ОБЯЗАНА бить назад");
        }

        [Fact]
        public void Man_ShouldNotPromoteInMiddleOfCaptureSeries()
        {
            // Arrange
            // Нам нужен настоящий Executor или его Mock, так как метод CanPromote полезет в доску
            var board = new Chessboard(10, 10);
            var executor = new TurnExecutor(board);

            // Ставим белую шашку на пред-дамочную линию
            var start = new Point(2, 2);
            var end = new Point(0, 4); // Допустим, это край для белых
            var target = new Point(1, 3);

            board.PlacePiece(start, new Piece(PieceSide.White, PieceType.Man));
            board.PlacePiece(target, new Piece(PieceSide.Black, PieceType.Man));

            var move = new Move(start, end, target);

            // Act
            // В международных шашках ProcessStep возвращает true, если был бой.
            // Но он НЕ должен менять тип фигуры на King немедленно.
            bool canContinue = _rules.ProcessStep(executor, move);

            // Assert
            Assert.True(canContinue);
            Assert.Equal(PieceType.Man, board[end]?.Type); // Фигура всё еще шашка, а не дамка!
        }

        [Fact]
        public void King_ShouldFly_LikeInRussianRules()
        {
            var whiteKing = new Piece(PieceSide.White, PieceType.King);

            // Дамка ищет цель на дистанции 4
            var verdict = _rules.EvaluateMove(whiteKing, RayDirection.UpRight, ScanState.ForcedCaptureOnly, 4, false);

            // Assert: Дамка может пролетать пустые клетки в поиске жертвы
            Assert.False(verdict.IsPossible);
            Assert.True(verdict.CanContinue);
        }

        [Fact]
        public void OnFinalize_ShouldPromoteMan_OnlyAtEndOfTurn()
        {
            // Arrange
            var board = new Chessboard(10, 10);
            var executor = new TurnExecutor(board);
            var endPoint = new Point(0, 4); // Край поля для белых

            // Ставим белую шашку на край
            board.PlacePiece(endPoint, new Piece(PieceSide.White, PieceType.Man));

            // Act
            // Вызываем финализацию, передавая точку, где "замерла" фигура
            _rules.OnFinalize(executor, endPoint);

            // Assert
            // Теперь, после финализации, она должна стать дамкой
            Assert.Equal(PieceType.King, board[endPoint]?.Type);
        }
    }
}
