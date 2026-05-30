using Checkers.Engine.Core;
using Checkers.Engine.Models;
using Checkers.Engine.Rules.Variants;
using Checkers.Engine.Scanning;

namespace Checkers.Engine.Tests.Scanning
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
            // Белая стоит снизу на (2, 2)
            Point start = new(2, 2);
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.Man));
            // Черная стоит сзади-слева от неё на (1, 1). Белая бьет НАЗАД.
            _board.PlacePiece(new Point(1, 1), new Piece(PieceSide.Black, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert
            Assert.Single(moves); // Обязательный бой назад
            // Клетка приземления после прыжка назад: (0, 0)
            Assert.Equal(new Point(0, 0), moves[0].To);
            Assert.NotNull(moves[0].Target);
        }

        [Fact]
        public void Russian_King_ShouldFlyAcrossBoard()
        {
            // Arrange
            var rules = new RussianRules();
            // Ставим белую дамку в угол (0, 0)
            Point start = new(0, 0);
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.King));
            // Ставим черную шашку на пути диагонали на (3, 3)
            _board.PlacePiece(new Point(3, 3), new Piece(PieceSide.Black, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert
            // Дамка перелетает через (3,3) и находит все варианты приземления: (4,4), (5,5), (6,6), (7,7)
            Assert.Equal(4, moves.Count);
            Assert.All(moves, m => Assert.NotNull(m.Target));
        }

        #endregion

        #region English Rules Tests (Checkers)

        [Fact]
        public void English_Man_ShouldNotCaptureBackwards()
        {
            // Arrange
            var rules = new EnglishRules();
            Point start = new(2, 2);
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.Man));
            // Черная стоит сзади на (1, 1)
            _board.PlacePiece(new Point(1, 1), new Piece(PieceSide.Black, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert
            // Пешка в Чекерсе не видит врага сзади, только 2 тихих хода вперед: на (3,1) и (3,3)
            Assert.Equal(2, moves.Count);
            Assert.All(moves, m => Assert.Null(m.Target));
        }

        [Fact]
        public void English_King_ShouldNotFly()
        {
            // Arrange
            var rules = new EnglishRules();
            Point start = new(4, 4); // В центре
            _board.PlacePiece(start, new Piece(PieceSide.White, PieceType.King));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert: Дамка в чекерсе ходит только на 1 клетку во все 4 стороны
            Assert.Equal(4, moves.Count);
            Assert.All(moves, m => Assert.Null(m.Target));
        }

        #endregion

        [Fact]
        public void ForcedCapture_GlobalCheck_TwoPieces()
        {
            // Arrange
            var rules = new RussianRules();

            // Пешка А (может бить вперед: (2,2) -> через (3,3) -> на (4,4))
            _board.PlacePiece(new Point(2, 2), new Piece(PieceSide.White, PieceType.Man));
            _board.PlacePiece(new Point(3, 3), new Piece(PieceSide.Black, PieceType.Man));

            // Пешка Б (может только идти вперед с (2,6))
            _board.PlacePiece(new Point(2, 6), new Piece(PieceSide.White, PieceType.Man));

            // Act
            var moves = _scanner.GetMovesForSide(_board, rules, PieceSide.White);

            // Assert: Пешка Б заблокирована, так как на доске есть обязательный бой для Пешки А
            Assert.All(moves, m => Assert.Equal(new Point(2, 2), m.From));
            Assert.All(moves, m => Assert.NotNull(m.Target));
        }
    }
}
