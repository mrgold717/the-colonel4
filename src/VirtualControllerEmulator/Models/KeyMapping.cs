namespace VirtualControllerEmulator.Models;

public enum InputType
{
    Key,
    MouseButton,
    MouseWheel,
    MouseMovement
}

public class KeyMapping
{
    public int InputKey { get; set; }
    public InputType InputType { get; set; } = InputType.Key;
    public string ControllerButton { get; set; } = string.Empty;
    public bool IsModifierRequired { get; set; }
    public int ModifierKey { get; set; }
    public bool TurboEnabled { get; set; }
    public int TurboRate { get; set; } = 10;

    public KeyMapping() { }

    public KeyMapping(int inputKey, InputType inputType, string controllerButton)
    {
        InputKey = inputKey;
        InputType = inputType;
        ControllerButton = controllerButton;
    }
}
