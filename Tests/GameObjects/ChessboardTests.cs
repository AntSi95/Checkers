using Checkers.Engine.Core;
using Checkers.Engine.Models;

namespace Checkers.Engine.Tests.GameObjects
{
    public class ChessboardTests
    {
        [Fact]
        public void PlacePiece_ShouldSetPiece_OnValidEvenSquare()
        {
            // Arrange: Игровые клетки чётные (4+4=8)
            var board = new Chessboard(8, 8, useEvenSquares: true);
            var piece = new Piece(PieceSide.White, PieceType.Man);
            var validPoint = new Point(4, 4);

            // Act
            board.PlacePiece(validPoint, piece);

            // Assert
            Assert.Equal(piece, board[validPoint]);
        }

        [Fact]
        public void PlacePiece_ShouldThrow_OnInvalidOddSquare()
        {
            // Arrange: Доска чётная, точка нечётная (5+4=9)
            var board = new Chessboard(8, 8, useEvenSquares: true);
            var piece = new Piece(PieceSide.White, PieceType.Man);
            var invalidPoint = new Point(5, 4);

            // Act & Assert: Метод должен выбросить ArgumentException
            Assert.Throws<ArgumentException>(() => board.PlacePiece(invalidPoint, piece));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(8, 0)]
        [InlineData(0, 8)]
        public void Indexer_ShouldReturnNull_ForPointsOutsideBounds(int row, int col)
        {
            // Arrange
            var board = new Chessboard(8, 8);
            var outPoint = new Point(row, col);

            // Act & Assert: Индексатор просто возвращает null для выхода за границы
            Assert.Null(board[outPoint]);
        }

        [Fact]
        public void Constructor_ShouldSetCorrectParty()
        {
            // Проверяем "шахматную" разметку игровых клеток
            var boardEven = new Chessboard(8, 8, useEvenSquares: true);
            var boardOdd = new Chessboard(8, 8, useEvenSquares: false);
            var piece = new Piece(PieceSide.White, PieceType.Man);

            // (0,0) — чётная, (0,1) — нечётная
            var p00 = new Point(0, 0);
            var p01 = new Point(0, 1);

            // Act
            boardEven.PlacePiece(p00, piece);
            boardOdd.PlacePiece(p01, piece);

            // Assert
            Assert.NotNull(boardEven[p00]);
            Assert.NotNull(boardOdd[p01]);

            // Проверяем инверсию: попытка поставить на чужую диагональ
            Assert.Throws<ArgumentException>(() => boardEven.PlacePiece(p01, piece));
            Assert.Throws<ArgumentException>(() => boardOdd.PlacePiece(p00, piece));
        }

        [Fact]
        public void Move_ShouldRelocatePiece_Correctly()
        {
            // Arrange
            var board = new Chessboard(8, 8);
            var piece = new Piece(PieceSide.White, PieceType.Man);
            var from = new Point(5, 5);
            var to = new Point(4, 4);
            board.PlacePiece(from, piece);

            // Act
            board.Move(from, to);

            // Assert
            Assert.Null(board[from]);
            Assert.Equal(piece, board[to]);
        }
    }
}
