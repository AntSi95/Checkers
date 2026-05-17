# 🇷🇺 Спецификация: Русские шашки (`RussianRules.cs`)

Данный документ описывает логические особенности русских шашек и содержит полную техническую реализацию стратегии правил для игрового движка.

## 📌 Логические особенности правил
*   **Игровое поле:** Классическая доска 8x8. Игра ведется строго по темным клеткам.
*   **Правила хода простых шашек:** Ходят только вперед на одну клетку по диагонали.
*   **Правила боя простых шашек:** Бьют как вперед, так и назад. При прыжке шашка перешагивает через фигуру соперника на следующую за ней пустую клетку.
*   **Специфика Дамки:** Дамка является дальнобойной («летающей»). Она может перемещаться по диагонали на любое количество свободных клеток в любом направлении.
*   **Приоритет взятия:** Если на доске есть возможность боя, игрок обязан бить. Однако, если есть выбор между простым тихим ходом и боем, или выбор между разными направлениями боя, игрок вправе выбрать траекторию с любым количеством сбиваемых фигур (приоритет большинства отсутствует).
*   **Превращение в полете:** Если простая шашка в процессе выполнения серии прыжков временно наступает на дамочное поле, она **мгновенно** становится дамкой и сразу же продолжает текущую серию взятий по правилам дамки (если есть доступные цели).

---

## 💻 Техническая реализация правил

Ниже представлен чистый код класса правил, полностью интегрированный с системами `MoveScanner` и `GameSession`:

```csharp
using checkers.GameObjects;
using checkers.GameObjects.Rules;
using checkers.GameObjects.Scanning;

namespace checkers.Core.Rules;

public class RussianRules : IRulesStrategy
{
    #region Методы создания игрового поля

    // Задаем параметры доски: 8 рядов, 8 столбцов, играем на темных (четных) клетках
    public BoardSettings GetSettings() => new BoardSettings(8, 8, UseEvenSquares: true);

    // Выполняем стартовую расстановку шашек на доске
    public IEnumerable<StartPosition> GetInitialPositions()
    {
        // Традиционная расстановка: 3 ряда черных (сверху), 3 ряда белых (снизу)
        var positions = new List<StartPosition>();
        for (int r = 0; r < 8; r++)
        {
            for (int c = (r % 2 == 0 ? 1 : 0); c < 8; c += 2)
            {
                // Первые три ряда (0, 1, 2) заполняем черными фигурами
                if (r < 3) positions.Add(new StartPosition(new Point(r, c), PieceSide.Black, PieceType.Man));
                
                // Последние три ряда (5, 6, 7) заполняем белыми фигурами
                if (r > 4) positions.Add(new StartPosition(new Point(r, c), PieceSide.White, PieceType.Man));
            }
        }
        return positions;
    }

    #endregion

    #region Методы сканирования

    // Проверяет, является ли фигура целью для атаки
    public bool IsEnemy(Piece actor, Piece target)
    {
        // Фигура должна принадлежать оппоненту.
        // Эффект "Турецкого удара": нельзя бить ту же фигуру второй раз за серию (у нее IsCaptured == true)
        return actor.Side != target.Side && !target.IsCaptured;
    }

    // Главный судейский метод: оценивает каждую клетку на пути диагонального луча
    public ScanVerdict EvaluateMove(Piece actor, RayDirection ray, ScanState state, int distance, bool isOccupied)
    {
        bool isForward = ray.IsForwardFor(actor.Side);

        // --- ВЕТКА СУДЕЙСТВА ДЛЯ ПРОСТОЙ ШАШКИ (Пешки) ---
        if (actor.Type == PieceType.Man)
        {
            return state switch
            {
                // Фаза Default: Обычный поиск ходов (ситуация на доске спокойная)
                // - Тихий ход: разрешен строго на 1 клетку вперед и только если она пуста (!isOccupied).
                // - Если клетка занята (isOccupied) на расстоянии 1: возвращаем (IsPossible: false, CanContinue: true).
                //   Это заставляет сканер остановиться для простой шашки, но передает клетку методу IsEnemy для проверки на бой.
                ScanState.Default => new ScanVerdict(
                    IsPossible: distance == 1 && isForward && !isOccupied,
                    CanContinue: isOccupied && distance == 1
                ),

                // Фаза ForcedCaptureOnly: В сессии обнаружен обязательный бой.
                // Саму пустую клетку подтвердить как ход нельзя (IsPossible: false).
                // Но луч продолжаем сканировать строго до расстояния 1, чтобы дать сканеру шанс наткнуться на фигуру врага.
                ScanState.ForcedCaptureOnly => new ScanVerdict(
                    IsPossible: false,
                    CanContinue: isOccupied && distance == 1
                ),

                // Фаза TargetDetected: На предыдущем шаге луча сканер зафиксировал врага.
                // Приземление для простой шашки возможно строго на дистанции 2 (сразу за врагом) и клетка должна быть пуста.
                // После этого сканировать этот луч для пешки больше бессмысленно (CanContinue: false).
                ScanState.TargetDetected => new ScanVerdict(
                    IsPossible: !isOccupied && distance == 2,
                    CanContinue: false
                ),

                _ => new ScanVerdict(false, false)
            };
        }
        // --- ВЕТКА СУДЕЙСТВА ДЛЯ ДАМКИ (Короля) ---
        else if (actor.Type == PieceType.King)
        {
            return state switch
            {
                // Фаза Default: Обычный поиск ходов.
                // Дамка может ходить на любую дистанцию, пока траектория луча абсолютно пуста (!isOccupied).
                ScanState.Default => new ScanVerdict(
                    IsPossible: !isOccupied,
                    CanContinue: true
                ),

                // Фаза ForcedCaptureOnly: Дамка ищет врага "вдалеке".
                // Сами пустые клетки игнорируются, но сканер беспрепятственно скользит по лучу дальше (CanContinue: true).
                ScanState.ForcedCaptureOnly => new ScanVerdict(
                    IsPossible: false,
                    CanContinue: true
                ),

                // Фазы TargetDetected (враг обнаружен) или CaptureMoveFound (прыжок уже подтвержден).
                // Дальнобойная дамка имеет право приземлиться на ЛЮБУЮ пустую клетку позади сбитой фигуры 
                // и продолжать скользить дальше по лучу, выбирая удобную точку для остановки.
                ScanState.TargetDetected or ScanState.CaptureMoveFound => new ScanVerdict(
                    IsPossible: !isOccupied,
                    CanContinue: !isOccupied
                ),

                _ => new ScanVerdict(false, false)
            };
        }

        return new ScanVerdict(false, false);
    }

    #endregion

    #region Методы реализации хода

    // Управляет физическим выполнением шага и фиксирует его последствия
    public bool ProcessStep(ITurnActions actions, Move move)
    {
        // Физически передвигаем фигуру на целевую клетку
        actions.ApplyMove(move);

        // Сценарий А: Шаг является прыжком-взятием
        if (move.IsCapture)
        {
            // Вешаем на срубленную фигуру маркер захвата (отложенное удаление)
            actions.ApplyCaptureMark(move.Target!.Value);

            // Механика «превращения в полете»: если шашка наступила на край доски в середине серии прыжков,
            // она мгновенно повышается до дамки и сможет продолжить бой в этой же сессии.
            if (actions.CanPromote(move.To))
                actions.ApplyPromotion(move.To);

            return true; // Возвращаем true — был бой, сессия обязана запустить сканер для проверки продолжения серии
        }

        // Сценарий Б: Обычный тихий ход. 
        // Просто проверяем, достигла ли шашка края доски в конце своего пути, чтобы стать дамкой.
        if (actions.CanPromote(move.To))
            actions.ApplyPromotion(move.To);

        return false; // Тихий ход — продолжение серии прыжков невозможно
    }

    // Вызывается один раз, когда игрок полностью завершил все действия своего хода
    public void OnFinalize(ITurnActions actions, Point lastPosition)
    {
        // Запускаем отложенный конвейер: физически очищаем доску от всех сбитых за ход фигур
        actions.ApplyFinalEffects();
    }

    #endregion

    #region Методы глобальных игровых правил

    // Определяет поведение системы, если у текущего игрока полностью закончились ходы
    public TurnResult HandleNoMoves(PieceSide side, Chessboard board) => TurnResult.GameFinished;

    // Выносит окончательный технический вердикт о результатах завершенной партии
    public GameResult JudgeTerminalState(PieceSide side, Chessboard board)
    {
        // Заблокированный игрок признается проигравшим, право победы отдается оппоненту
        var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
        
        // Анализируем причину финала: проверяем, остались ли у проигравшей стороны фигуры на доске
        bool hasPieces = board.GetValidSquares().Any(p => board[p]?.Side == side);

        // Если фигуры остались, но ходить некуда — это пат (NoAvailableMoves). Если фигур нет — тотальное уничтожение (AllPiecesCaptured).
        return new GameResult(winner, hasPieces ? GameEndReason.NoAvailableMoves : GameEndReason.AllPiecesCaptured);
    }

    #endregion
}
```