namespace GameFramework.Input
{
	public struct GamepadVibration
	{
		public float LeftMotor;
		public float RightMotor;
		public float LeftTrigger;
		public float RightTrigger;

        public static GamepadVibration None
        {
            get
            {
                return new GamepadVibration(0.0f, 0.0f, 0.0f, 0.0f);
            }
        }

		public GamepadVibration(float leftMotor, float rightMotor, float leftTrigger, float rightTrigger)
		{
			LeftMotor = leftMotor;
			RightMotor = rightMotor;
			LeftTrigger = leftTrigger;
			RightTrigger = rightTrigger;
		}
	}

	public interface IGamepadStateAdapter
	{
		bool IsConnected(GamepadIndex gamepad);
		float GetAxis(GamepadAxis axis, GamepadIndex gamepad);
		float GetAxisRaw(GamepadAxis axis, GamepadIndex gamepad);
		bool GetButton(GamepadButton button, GamepadIndex gamepad);
		bool GetButtonDown(GamepadButton button, GamepadIndex gamepad);
		bool GetButtonUp(GamepadButton button, GamepadIndex gamepad);
		void SetVibration(GamepadVibration vibration, GamepadIndex gamepad);
		GamepadVibration GetVibration(GamepadIndex gamepad);
	}
}