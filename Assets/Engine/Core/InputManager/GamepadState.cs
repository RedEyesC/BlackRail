using UnityEngine;

namespace GameFramework.Input
{
    public static class GamepadState
	{
		private static bool m_hasWarningBeenDisplayed = false;
        private static IGamepadStateAdapter m_adapter = null;

		public static IGamepadStateAdapter Adapter
        {
            get { return m_adapter; }
            set
            {
                if(value != m_adapter)
                {
                    m_adapter = value;

                    GamepadStateService service = InputManager.GetService<GamepadStateService>();
                    if(service != null)
                    {
                        service.SetAdapter(m_adapter);
                    }
                }
            }
        }

        public static bool IsGamepadSupported
        {
            get { return Adapter != null; }
        }

        public static bool AnyInput()
        {
            PrintMissingAdapterWarningIfNecessary();
            
            GamepadStateService service = InputManager.GetService<GamepadStateService>();
            return service != null ? service.AnyInput(GamepadIndex.GamepadOne) : false;
        }

        public static bool AnyInput(GamepadIndex gamepad)
        {
            PrintMissingAdapterWarningIfNecessary();
            
            GamepadStateService service = InputManager.GetService<GamepadStateService>();
            return service != null ? service.AnyInput(gamepad) : false;
        }

        public static bool IsConnected(GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			return Adapter != null ? Adapter.IsConnected(gamepad) : false;
		}

		public static float GetAxis(GamepadAxis axis, GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			return Adapter != null ? Adapter.GetAxis(axis, gamepad) : 0;
		}

		public static float GetAxisRaw(GamepadAxis axis, GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			return Adapter != null ? Adapter.GetAxisRaw(axis, gamepad) : 0;
		}

		public static bool GetButton(GamepadButton button, GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			return Adapter != null ? Adapter.GetButton(button, gamepad) : false;
		}

		public static bool GetButtonDown(GamepadButton button, GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			return Adapter != null ? Adapter.GetButtonDown(button, gamepad) : false;
		}

		public static bool GetButtonUp(GamepadButton button, GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			return Adapter != null ? Adapter.GetButtonUp(button, gamepad) : false;
		}

		public static void SetVibration(GamepadVibration vibration, GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			Adapter.SetVibration(vibration, gamepad);
		}

		public static GamepadVibration GetVibration(GamepadIndex gamepad)
		{
			PrintMissingAdapterWarningIfNecessary();
			return Adapter != null ? Adapter.GetVibration(gamepad) : GamepadVibration.None;
		}

		private static void PrintMissingAdapterWarningIfNecessary()
		{
			if(Adapter == null && !m_hasWarningBeenDisplayed)
			{
				Debug.LogWarning("No gamepad adapter has been assigned.");
				m_hasWarningBeenDisplayed = true;
			}
		}
	}
}
