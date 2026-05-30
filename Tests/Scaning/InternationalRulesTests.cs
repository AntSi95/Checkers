using Checkers.Engine.Core;
using Checkers.Engine.Models;
using Checkers.Engine.Rules.Variants;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Tests.Scanning
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
            var board = new Chessboard(10, 10, useEvenSquares: true);

            var start = new Point(7, 3);
            var end = new Point(9, 5); // Row == 9 — крайняя дамочная линия для белых фигурок
            var target = new Point(8, 4);

            var piece = new Piece(PieceSide.White, PieceType.Man);
            board.PlacePiece(start, piece);
            board.PlacePiece(target, new Piece(PieceSide.Black, PieceType.Man));

            var move = new Move(start, end, target);

            // Act
            bool canContinue = _rules.ProcessStep(board, move);

            // Assert
            Assert.True(canContinue);

            board.TryGetPiece(end, out var resultPiece);
            Assert.Equal(PieceType.Man, resultPiece?.Type); // Фигура всё еще шашка, а не дамка!
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
            var board = new Chessboard(10, 10, useEvenSquares: true);
            var endPoint = new Point(9, 5); // Row == 9 — дамочный край для белых

            // Ставим белую шашку на край
            board.PlacePiece(endPoint, new Piece(PieceSide.White, PieceType.Man));

            // Act
            _rules.OnFinalize(board, endPoint);

            // Assert
            board.TryGetPiece(endPoint, out var resultPiece);
            Assert.Equal(PieceType.King, resultPiece?.Type); // После финализации она обязана стать дамкой
        }
    }
}
