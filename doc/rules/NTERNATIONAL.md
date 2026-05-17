# 🌐 Спецификация: Международные шашки (`InternationalRules.cs`)

Данный документ описывает логические особенности международных стоклеточных шашек и содержит оригинальную техническую реализацию стратегии правил для игрового движка.

## 📌 Логические особенности правил
*   **Игровое поле:** Увеличенная матрица 10x10 (100 клеток). Расстановка сил занимает по 4 ряда с каждой стороны (всего по 20 шашек у игрока).
*   **Простые шашки:** Тихий ход разрешен только вперед на одну клетку. Ударный ход (прыжок) выполняется **как вперед, так и назад** строго на две клетки через фигуру соперника.
*   **Дамка:** «Летающая» (дальнобойная). Ходит и бьет на любое количество свободных клеток по диагонали в любом направлении.
*   **Отложенное превращение (Флаговая модель):** Если простая шашка в процессе выполнения серии прыжков временно пересекает дамочную линию, но логически обязана продолжить бой дальше, она пролетает это поле, **оставаясь простой фигурой**. Она станет дамкой только в том случае, если вся серия взятий окончательно завершится остановкой на дамочной линии.

> [!IMPORTANT]
> **Ограничение текущей версии (Альфа):** 
> На данном этапе в движке не реализован алгоритм глубинного анализа вариантов (построение дерева ходов). В связи с этим строгое правило международных шашек на **Приоритет большинства** (требование выбирать вектор атаки, приносящий максимальное количество сбитых фигур) пока отсутствует. Сканер выдает все физически возможные прыжки, а выбраковка меньших серий будет добавлена на следующем этапе разработки.

---

## 💻 Техническая реализация правил

Ниже представлен чистый код класса правил, полностью интегрированный со сканером и сессией движка:

```csharp
using checkers.GameObjects;
using checkers.GameObjects.Rules;
using checkers.GameObjects.Scanning;

namespace checkers.Core.Rules;

public class InternationalRules : IRulesStrategy
{
    #region Методы создания игрового поля

    // Настройка доски: 10 рядов, 10 столбцов, играем на темных (четных) клетках
    public BoardSettings GetSettings() => new BoardSettings(10, 10, UseEvenSquares: true);

    // Выполняем стартовую расстановку шашек на стоклеточном поле
    public IEnumerable<StartPosition> GetInitialPositions()
    {
        // 4 ряда фигур на доске 10x10 (всего по 20 шашек у каждой стороны)
        for (int r = 0; r < 10; r++)
            for (int c = 0; c < 10; c++)
            {
                // Отсекаем неигровые белые клетки
                if ((r + c) % 2 != 1) continue;
                
                // Верхние четыре ряда (0, 1, 2, 3) заполняем черными шашками
                if (r < 4) yield return new StartPosition(new Point(r, c), PieceSide.Black, PieceType.Man);
                
                // Нижние четыре ряда (6, 7, 8, 9) заполняем белыми шашками
                if (r > 5) yield return new StartPosition(new Point(r, c), PieceSide.White, PieceType.Man);
            }
    }

    #endregion

    #region Методы сканирования

    // Проверяет, доступна ли фигура для взятия
    public bool IsEnemy(Piece actor, Piece target)
    {
        // Фигура должна принадлежать оппоненту и не быть срубленной ранее в этом же ходу
        return actor.Side != target.Side && !target.IsCaptured;
    }

    // Главный судейский метод: оценивает каждую клетку на пути диагонального луча 10x10
    public ScanVerdict EvaluateMove(Piece actor, RayDirection ray, ScanState state, int distance, bool isOccupied)
    {
        bool isForward = ray.IsForwardFor(actor.Side);

        // --- ВЕТКА СУДЕЙСТВА ДЛЯ ПРОСТОЙ ШАШКИ (Пешки) ---
        if (actor.Type == PieceType.Man)
        {
            return state switch
            {
                // Фаза Default: Обычный поиск тихих ходов.
                // Разрешено перемещение строго на 1 клетку и только вперед.
                // Если клетка на расстоянии 1 занята фигурой, разрешаем сканеру продолжить луч (CanContinue: true),
                // чтобы на следующем этапе система проверила её на враждебность методом IsEnemy.
                ScanState.Default => new ScanVerdict(
                    IsPossible: distance == 1 && isForward && !isOccupied, 
                    CanContinue: distance == 1 && isOccupied
                ),
                
                // Фаза ForcedCaptureOnly: В сессии обнаружен обязательный бой.
                // Простые шашки в международных правилах бьют во все стороны (включая вектор назад).
                // Саму пустую клетку подтвердить нельзя, но луч продолжаем до дистанции 1, если там препятствие.
                ScanState.ForcedCaptureOnly => new ScanVerdict(
                    IsPossible: false, 
                    CanContinue: distance == 1 && isOccupied
                ),
                
                // Фаза TargetDetected: Сканер перешагнул через вражескую фигуру.
                // Приземление для пешки возможно строго на расстоянии 2 (сразу за врагом) в пустую клетку.
                ScanState.TargetDetected => new ScanVerdict(
                    IsPossible: distance == 2 && !isOccupied, 
                    false
                ),
                
                _ => new ScanVerdict(false, false)
            };
        }

        // --- ВЕТКА СУДЕЙСТВА ДЛЯ ДАМКИ (Короля) ---
        // Летающая дамка на поле 10x10 работает математически идентично правилам русских шашек
        return state switch
        {
            // Свободное скольжение по лучу на любую дистанцию, пока клетки пусты
            ScanState.Default => new ScanVerdict(!isOccupied, true),
            
            // Пропуск пустых клеток при поиске удаленной цели
            ScanState.ForcedCaptureOnly => new ScanVerdict(false, true),
            
            // Выбор любой клетки приземления позади врага и продолжение скольжения по лучу
            ScanState.TargetDetected or ScanState.CaptureMoveFound => new ScanVerdict(!isOccupied, !isOccupied),
            
            _ => new ScanVerdict(false, false)
        };
    }

    #endregion

    #region Методы реализации хода

    // Управляет физическим выполнением шага фигуры на доске
    public bool ProcessStep(ITurnActions actions, Move move)
    {
        // Сдвигаем фигуру на новую позицию
        actions.ApplyMove(move);

        // Сценарий А: Выполнен прыжок-взятие
        if (move.IsCapture)
        {
            // Маркируем фигуру противника как съеденную (отложенное удаление)
            actions.ApplyCaptureMark(move.Target!.Value);
            
            // В международных шашках, если фигура НЕ остановилась на краю — она НЕ становится дамкой.
            // Наша сессия вызывает сканер ходов ПОСЛЕ этого метода. Если сканер найдет новые цели
            // для взятия, фигура продолжит бой как простая шашка, поэтому маркер повышения здесь не ставится.
            return true; // Был бой, сообщаем сессии о потенциальном продолжении серии прыжков
        }

        // Сценарий Б: Обычный тихий ход.
        // Если простая шашка завершила свой единственный шаг на краю доски — вешаем отложенную метку коронации.
        if (actions.CanPromote(move.To)) 
            actions.ApplyPromotionMark(move.To);
            
        return false; // Тихий ход — серия невозможна
    }

    // Вызывается один раз, когда игрок полностью завершил все действия своего хода
    public void OnFinalize(ITurnActions actions, Point lastPosition)
    {
        // Реализация флаговой отложенной модели для международных шашек:
        // Если вся серия прыжков (или тихий ход) окончательно остановилась на дамочной клетке,
        // мы вызываем физическое превращение фигуры в дамку.
        if (actions.CanPromote(lastPosition))
            actions.ApplyPromotion(lastPosition);
            
        // Запускаем физическую зачистку доски от сбитых за ход фигур
        actions.ApplyFinalEffects();
    }

    #endregion

    #region Методы глобальных игровых правил

    // Решает, что делать при полном запирании ходов у текущей стороны
    public TurnResult HandleNoMoves(PieceSide side, Chessboard board) => TurnResult.GameFinished;

    // Выносит окончательный технический вердикт о результатах партии
    public GameResult JudgeTerminalState(PieceSide side, Chessboard board)
    {
        // Заблокированная сторона признается проигравшей, оппонент побеждает
        var winner = (side == PieceSide.White) ? GameStatus.BlackWin : GameStatus.WhiteWin;
        return new GameResult(winner, GameEndReason.NoAvailableMoves);
    }

    #endregion
}
```
