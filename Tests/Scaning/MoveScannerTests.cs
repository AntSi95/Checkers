using Checkers.Engine.Core;
using Checkers.Engine.Models;
using Checkers.Engine.Rules.Variants;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Tests.Scaning
{
    public class MoveScannerTests
    {
        private readonly MoveScanner _scanner = new();
        private readonly Chessboard _board = new(8, 8, useEvenSquares: true);

        #region Russian Rules Tests (Русские шашки)

        [Fact]
        public void Russian_Man_ShouldCaptureBackwards_AndForceIt()
        {
            // Arrange
            var rules = new RussianRules();
            Point start = new(5, 5);
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.Man));
            _board.PlacePiece(new Point(6, 4), new Piece(PieceSide.Black, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert
            Assert.Single(moves); // Обязательный бой назад
            Assert.Equal(new Point(7, 3), moves[0].To);
            Assert.NotNull(moves[0].Target);
        }

        [Fact]
        public void Russian_King_ShouldFlyAcrossBoard()
        {
            // Arrange
            var rules = new RussianRules();
            Point start = new(7, 7);
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.King));
            _board.PlacePiece(new Point(4, 4), new Piece(PieceSide.Black, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert
            // Дамка находит все варианты приземления на векторе (3,3), (2,2), (1,1), (0,0)
            Assert.Equal(4, moves.Count);
            Assert.All(moves, m => Assert.NotNull(m.Target));
        }

        #endregion

        #region English Rules Tests (Checkers)

        [Fact]
        public void English_Man_ShouldNotCaptureBackwards()
        {
            // Arrange
            var rules = new EnglishRules(); // Чекерсу сканнер не нужен
            Point start = new(5, 5);
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.Man));
            _board.PlacePiece(new Point(6, 4), new Piece(PieceSide.Black, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert
            // Пешка не видит врага сзади, только тихие ходы вперед
            Assert.Equal(2, moves.Count);
            Assert.All(moves, m => Assert.Null(m.Target));
        }

        [Fact]
        public void English_King_ShouldNotFly()
        {
            // Arrange
            var rules = new EnglishRules();
            Point start = new(5, 5); // Поставим в центр для наглядности
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.King));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert: Дамка в чекерсе ходит на 1 клетку во все 4 стороны
            Assert.Equal(4, moves.Count);
            Assert.All(moves, m => Assert.Null(m.Target));
        }

        #endregion

        [Fact]
        public void ForcedCapture_GlobalCheck_TwoPieces()
        {
            // Arrange: Приоритет боя для всей стороны
            var rules = new RussianRules();

            // Пешка А (может бить)
            _board.PlacePiece(new Point(5, 5), new Piece(PieceSide.White, PieceType.Man));
            _board.PlacePiece(new Point(4, 4), new Piece(PieceSide.Black, PieceType.Man));

            // Пешка Б (может только идти)
            _board.PlacePiece(new Point(5, 1), new Piece(PieceSide.White, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert: Пешка Б заблокирована, так как на доске есть бой
            Assert.All(moves, m => Assert.Equal(new Point(5, 5), m.From));
            Assert.All(moves, m => Assert.NotNull(m.Target));
        }
    }
}
