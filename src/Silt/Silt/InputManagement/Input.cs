using System.Numerics;
using Silk.NET.Input;

namespace Silt.InputManagement;

/// <summary>
/// Manages input from keyboard and mouse devices.
/// </summary>
public static class Input
{
    private static readonly HashSet<Key> _pressedKeys = [];
    private static readonly HashSet<MouseButton> _pressedMouseButtons = [];

    /// <summary>
    /// Current position of the mouse cursor.
    /// </summary>
    public static Vector2 MousePosition { get; private set; }
    
    /// <summary>
    /// Mouse position delta since the last frame.
    /// </summary>
    public static Vector2 MousePositionDelta { get; private set; }

    /// <summary>
    /// Mouse scroll wheel offset since the last frame.
    /// </summary>
    public static Vector2 MouseScrollDelta { get; private set; }


    public static void Initialize(IInputContext inputContext)
    {
        foreach (IKeyboard keyboard in inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }

        foreach (IMouse mouse in inputContext.Mice)
        {
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseScroll;
        }
    }


    /// <summary>
    /// Updates the input state. Should be called once per frame.
    /// </summary>
    public static void Update()
    {
        MousePositionDelta = Vector2.Zero;
        MouseScrollDelta = Vector2.Zero;
    }


    /// <summary>
    /// Checks if a specific key is currently being held down.
    /// </summary>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    public static bool IsKeyDown(Key key) => _pressedKeys.Contains(key);


    /// <summary>
    /// Checks if a specific mouse button is currently being held down.
    /// </summary>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    public static bool IsMouseButtonDown(MouseButton button) => _pressedMouseButtons.Contains(button);


    private static void OnKeyDown(IKeyboard keyboard, Key key, int arg3) => _pressedKeys.Add(key);


    private static void OnKeyUp(IKeyboard keyboard, Key key, int arg3) => _pressedKeys.Remove(key);


    private static void OnMouseDown(IMouse mouse, MouseButton button) => _pressedMouseButtons.Add(button);


    private static void OnMouseUp(IMouse mouse, MouseButton button) => _pressedMouseButtons.Remove(button);


    private static void OnMouseMove(IMouse mouse, Vector2 position)
    {
        MousePosition = position;
        MousePositionDelta += position;
    }


    private static void OnMouseScroll(IMouse mouse, ScrollWheel scroll) => MouseScrollDelta += new Vector2(scroll.X, scroll.Y);
}