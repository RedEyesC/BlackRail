namespace GameFramework.Input
{
    public enum InputType
	{
		Button,
		MouseAxis,
		DigitalAxis,
		RemoteButton,
		RemoteAxis,
		AnalogButton,
		AnalogAxis,
		GamepadButton,
		GamepadAnalogButton,
		GamepadAxis
	}

    public enum GamepadIndex
    {
        GamepadOne = 0, GamepadTwo, GamepadThree, GamepadFour
    }

    public struct KeyUsageResult
    {
        public string ControlSchemeName;
        public int ActionIndex;
        public int BindingIndex;

        public static KeyUsageResult None
        {
            get
            {
                return new KeyUsageResult
                {
                    ControlSchemeName = null,
                    ActionIndex = -1,
                    BindingIndex = -1
                };
            }
        }
    }

    public enum DeadZoneType
    {
        CutOff = 0, Remap
    }

    public enum ButtonState
    {
        Pressed, JustPressed, Released, JustReleased
    }

    public enum GamepadButton
    {
        LeftStick,
        RightStick,
        LeftBumper,
        RightBumper,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        Back,
        Start,
        ActionBottom,
        ActionRight,
        ActionLeft,
        ActionTop,
    }

    public enum GamepadAxis
    {
        LeftThumbstickX,
        LeftThumbstickY,
        RightThumbstickX,
        RightThumbstickY,
        DPadX,
        DPadY,
        LeftTrigger,
        RightTrigger
    }
}