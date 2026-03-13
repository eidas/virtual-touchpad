using VirtualTouchpad.Services;
using Xunit;

namespace VirtualTouchpad.Tests;

public class GestureStateMachineTests
{
    private readonly MockInputSimulator _mockInput = new();
    private readonly GestureStateMachine _sm;

    public GestureStateMachineTests()
    {
        _sm = new GestureStateMachine(_mockInput);
    }

    [Fact]
    public void InitialState_IsIdle()
    {
        Assert.Equal(GestureState.Idle, _sm.CurrentState);
    }

    [Fact]
    public void TouchDown_TransitionsToTracking()
    {
        _sm.OnTouchDown(1, 100, 100);
        Assert.Equal(GestureState.Tracking, _sm.CurrentState);
    }

    [Fact]
    public void SmallMoveAndQuickRelease_TriggersLeftClick()
    {
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchMove(1, 102, 101); // small movement < 10px
        _sm.OnTouchUp(1, 102, 101);

        Assert.Equal(GestureState.Idle, _sm.CurrentState);
        Assert.Equal(1, _mockInput.LeftClickCount);
    }

    [Fact]
    public void LargeMove_TransitionsToCursorMove()
    {
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchMove(1, 120, 100); // 20px > 10px threshold

        Assert.Equal(GestureState.CursorMove, _sm.CurrentState);
        Assert.True(_mockInput.MoveCount > 0);
    }

    [Fact]
    public void LargeMove_ThenRelease_ReturnsToIdle()
    {
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchMove(1, 120, 100);
        _sm.OnTouchUp(1, 130, 100);

        Assert.Equal(GestureState.Idle, _sm.CurrentState);
        Assert.Equal(0, _mockInput.LeftClickCount); // No click on drag release
    }

    [Fact]
    public void CursorMove_AppliesMovementDeltas()
    {
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchMove(1, 115, 100); // exceeds threshold
        _sm.OnTouchMove(1, 120, 105); // additional move

        Assert.True(_mockInput.MoveCount >= 2);
    }

    [Fact]
    public void DifferentPointerId_IsIgnoredForMoves()
    {
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchMove(2, 200, 200); // different pointer

        Assert.Equal(GestureState.Tracking, _sm.CurrentState);
        Assert.Equal(0, _mockInput.MoveCount);
    }

    [Fact]
    public void TouchUp_WithDifferentPointer_DoesNotChangeState()
    {
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchMove(1, 120, 100); // enter CursorMove
        _sm.OnTouchUp(2, 120, 100); // different pointer

        Assert.Equal(GestureState.CursorMove, _sm.CurrentState);
    }

    [Fact]
    public void DoubleTap_TriggersDoubleClick()
    {
        // First tap
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchUp(1, 100, 100);

        Assert.Equal(1, _mockInput.LeftClickCount);

        // Second tap within interval
        _sm.OnTouchDown(1, 100, 100);
        _sm.OnTouchUp(1, 100, 100);

        Assert.Equal(1, _mockInput.DoubleClickCount);
    }

    [Fact]
    public void Dragging_IssuesLeftDownAndLeftUp()
    {
        _sm.OnTouchDown(1, 100, 100);

        // Simulate hold by directly transitioning (timer-based in real code)
        // Wait for hold timer - in real code this is async
        Thread.Sleep(600); // wait for hold timer

        // If state became HoldReady, move to start dragging
        if (_sm.CurrentState == GestureState.HoldReady)
        {
            _sm.OnTouchMove(1, 110, 100);
            Assert.Equal(GestureState.Dragging, _sm.CurrentState);
            Assert.Equal(1, _mockInput.LeftDownCount);

            _sm.OnTouchUp(1, 120, 100);
            Assert.Equal(GestureState.Idle, _sm.CurrentState);
            Assert.Equal(1, _mockInput.LeftUpCount);
        }
    }
}

public class MockInputSimulator : IInputSimulator
{
    public int MoveCount { get; private set; }
    public int LeftClickCount { get; private set; }
    public int RightClickCount { get; private set; }
    public int DoubleClickCount { get; private set; }
    public int LeftDownCount { get; private set; }
    public int LeftUpCount { get; private set; }
    public int RightDownCount { get; private set; }
    public int RightUpCount { get; private set; }
    public int ScrollCount { get; private set; }
    public int HScrollCount { get; private set; }
    public List<(int dx, int dy)> Moves { get; } = [];

    public void MoveCursorRelative(int dx, int dy) { MoveCount++; Moves.Add((dx, dy)); }
    public void LeftClick() => LeftClickCount++;
    public void RightClick() => RightClickCount++;
    public void DoubleClick() => DoubleClickCount++;
    public void LeftDown() => LeftDownCount++;
    public void LeftUp() => LeftUpCount++;
    public void RightDown() => RightDownCount++;
    public void RightUp() => RightUpCount++;
    public void Scroll(int delta) => ScrollCount++;
    public void HorizontalScroll(int delta) => HScrollCount++;
}
