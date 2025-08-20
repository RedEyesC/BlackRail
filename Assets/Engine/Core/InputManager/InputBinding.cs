using UnityEngine;
using System;

namespace GameFramework.Input
{
	public class InputBinding
	{
		public const float AXIS_NEUTRAL = 0.0f;
		public const float AXIS_POSITIVE = 1.0f;
		public const float AXIS_NEGATIVE = -1.0f;
		public const int MAX_MOUSE_AXES = 3;
		public const int MAX_JOYSTICK_AXES = 28;
        public const int MAX_JOYSTICK_BUTTONS = 20;
        public const int MAX_JOYSTICKS = 11;


		private KeyCode _positive;
		private KeyCode _negative;
        private DeadZoneType _deadZoneType;
		private float _deadZone;
		private float _gravity;
		private float _sensitivity = 1.0f;
        private float _scale = 1.0f;
		private bool _snap;
		private bool _invert;
		private InputType _type;
		private int _axis;
		private int _joystick;
		private GamepadButton _gamepadButton;
		private GamepadAxis _gamepadAxis;
		private GamepadIndex _gamepadIndex;

		private string _rawAxisName;
		private float _value;
		private bool _isAxisDirty;
		private bool _isTypeDirty;
		private ButtonState _remoteButtonState;
		private ButtonState _analogButtonState;

		public KeyCode Positive
		{
			get { return _positive; }
			set { _positive = value; }
		}

		public KeyCode Negative
		{
			get { return _negative; }
			set { _negative = value; }
		}

        public DeadZoneType DeadZoneType
        {
            get { return _deadZoneType; }
            set { _deadZoneType = value; }
        }

		public float DeadZone
		{
			get { return _deadZone; }
			set { _deadZone = Mathf.Clamp01(value); }
		}

		public float Gravity
		{
			get { return _gravity; }
			set { _gravity = Mathf.Max(value, 0.0f); }
		}

		public float Sensitivity
		{
			get { return _sensitivity; }
			set { _sensitivity = Math.Max(value, 0.0f); }
		}

        public float Scale
        {
            get { return _scale; }
            set { _scale = Math.Max(value, 0.0f); }
        }

        public bool Snap
		{
			get { return _snap; }
			set { _snap = value; }
		}

		public bool Invert
		{
			get { return _invert; }
			set { _invert = value; }
		}

		public InputType Type
		{
			get { return _type; }
			set
			{
				_type = value;
				_isTypeDirty = true;
			}
		}

		public int Axis
		{
			get { return _axis; }
			set
			{
				_axis = Mathf.Clamp(value, 0, MAX_JOYSTICK_AXES - 1);
				_isAxisDirty = true;
			}
		}

		public int Joystick
		{
			get { return _joystick; }
			set
			{
				_joystick = Mathf.Clamp(value, 0, MAX_JOYSTICKS - 1);
				_isAxisDirty = true;
			}
		}

		public GamepadButton GamepadButton
		{
			get { return _gamepadButton; }
			set { _gamepadButton = value; }
		}

		public GamepadAxis GamepadAxis
		{
			get { return _gamepadAxis; }
			set { _gamepadAxis = value; }
		}

		public GamepadIndex GamepadIndex
		{
			get { return _gamepadIndex; }
			set { _gamepadIndex = value; }
		}

		public bool AnyInput
		{
			get
			{
				switch(_type)
				{
				case InputType.Button:
					return UnityEngine.Input.GetKey(_positive);
				case InputType.AnalogButton:
				case InputType.GamepadAnalogButton:
					return _analogButtonState == ButtonState.Pressed || _analogButtonState == ButtonState.JustPressed;
				case InputType.RemoteButton:
					return _remoteButtonState == ButtonState.Pressed || _remoteButtonState == ButtonState.JustPressed;
				case InputType.GamepadButton:
					return GamepadState.GetButton(_gamepadButton, _gamepadIndex);
				case InputType.GamepadAxis:
					return Mathf.Abs(GamepadState.GetAxisRaw(_gamepadAxis, _gamepadIndex)) >= 1.0f;
				case InputType.DigitalAxis:
				case InputType.RemoteAxis:
					return Mathf.Abs(_value) >= 1.0f;
				default:
					return Mathf.Abs(UnityEngine.Input.GetAxisRaw(_rawAxisName)) >= 1.0f;
				}
			}
		}

		public bool AnyKey
		{
			get
			{
				return UnityEngine.Input.GetKey(_positive) || UnityEngine.Input.GetKey(_negative);
			}
		}

		public bool AnyKeyDown
		{
			get
			{
				return UnityEngine.Input.GetKeyDown(_positive) || UnityEngine.Input.GetKeyDown(_negative);
			}
		}

		public bool AnyKeyUp
		{
			get
			{
				return UnityEngine.Input.GetKeyUp(_positive) || UnityEngine.Input.GetKeyUp(_negative);
			}
		}

		public InputBinding()
		{
			_positive = KeyCode.None;
			_negative = KeyCode.None;
            _deadZoneType = DeadZoneType.CutOff;
			_type = InputType.Button;
			_gravity = 1.0f;
			_sensitivity = 1.0f;
            _scale = 1.0f;
		}

		public void Initialize()
		{
			UpdateRawAxisName();
			_value = AXIS_NEUTRAL;
			_isAxisDirty = false;
			_isTypeDirty = false;
			_remoteButtonState = ButtonState.Released;
			_analogButtonState = ButtonState.Released;
		}

		public void Update(float deltaTime)
		{
			if(_isTypeDirty)
			{
				Reset();
				_isTypeDirty = false;
			}

			if(_isAxisDirty)
			{
				UpdateRawAxisName();
				_analogButtonState = ButtonState.Released;
				_isAxisDirty = false;
			}

			bool bothKeysDown = UnityEngine.Input.GetKey(_positive) && UnityEngine.Input.GetKey(_negative);
			if(_type == InputType.DigitalAxis && !bothKeysDown)
			{
				UpdateDigitalAxisValue(deltaTime);
			}
			if(_type == InputType.AnalogButton || _type == InputType.GamepadAnalogButton)
			{
				UpdateAnalogButtonValue();
			}
		}

		private void UpdateDigitalAxisValue(float deltaTime)
		{
			if(UnityEngine.Input.GetKey(_positive))
			{
				if(_value < AXIS_NEUTRAL && _snap)
				{
					_value = AXIS_NEUTRAL;
				}

				_value += _sensitivity * deltaTime;
				if(_value > AXIS_POSITIVE)
				{
					_value = AXIS_POSITIVE;
				}
			}
			else if(UnityEngine.Input.GetKey(_negative))
			{
				if(_value > AXIS_NEUTRAL && _snap)
				{
					_value = AXIS_NEUTRAL;
				}

				_value -= _sensitivity * deltaTime;
				if(_value < AXIS_NEGATIVE)
				{
					_value = AXIS_NEGATIVE;
				}
			}
			else
			{
				if(_value < AXIS_NEUTRAL)
				{
					_value += _gravity * deltaTime;
					if(_value > AXIS_NEUTRAL)
					{
						_value = AXIS_NEUTRAL;
					}
				}
				else if(_value > AXIS_NEUTRAL)
				{
					_value -= _gravity * deltaTime;
					if(_value < AXIS_NEUTRAL)
					{
						_value = AXIS_NEUTRAL;
					}
				}
			}
		}

		private void UpdateAnalogButtonValue()
		{
			float axis = _type == InputType.AnalogButton ?
									UnityEngine.Input.GetAxis(_rawAxisName) :
									GamepadState.GetAxis(_gamepadAxis, _gamepadIndex);

			axis = _invert ? -axis : axis;

			if(axis > _deadZone)
			{
				if(_analogButtonState == ButtonState.Released || _analogButtonState == ButtonState.JustReleased)
					_analogButtonState = ButtonState.JustPressed;
				else if(_analogButtonState == ButtonState.JustPressed)
					_analogButtonState = ButtonState.Pressed;
			}
			else
			{
				if(_analogButtonState == ButtonState.Pressed || _analogButtonState == ButtonState.JustPressed)
					_analogButtonState = ButtonState.JustReleased;
				else if(_analogButtonState == ButtonState.JustReleased)
					_analogButtonState = ButtonState.Released;
			}
		}

		public float? GetAxis()
		{
			float? axis = null;

			if(_type == InputType.DigitalAxis || _type == InputType.RemoteAxis)
			{
                axis = axis * _scale;
				axis = _invert ? -_value : _value;
			}
			else if(_type == InputType.MouseAxis)
			{
				if(_rawAxisName != null)
				{
					axis = UnityEngine.Input.GetAxis(_rawAxisName) * _sensitivity;
					axis = _invert ? -axis : axis;
				}
			}
			else if(_type == InputType.AnalogAxis)
			{
				if(_rawAxisName != null)
				{
					axis = UnityEngine.Input.GetAxis(_rawAxisName);
                    axis = ApplyDeadZone(axis.Value);
					axis = Mathf.Clamp(axis.Value * _sensitivity, -1, 1);
                    axis = axis * _scale;
					axis = _invert ? -axis : axis;
				}
			}
			else if(_type == InputType.GamepadAxis)
			{
				axis = GamepadState.GetAxis(_gamepadAxis, _gamepadIndex);
                axis = ApplyDeadZone(axis.Value);
                axis = Mathf.Clamp(axis.Value * _sensitivity, -1, 1);
                axis = axis * _scale;
                axis = _invert ? -axis : axis;
			}

			if(axis.HasValue && Mathf.Abs(axis.Value) <= 0.0f)
				axis = null;
			
			return axis;
		}

		///<summary>
		///	Returns raw input with no sensitivity or smoothing applyed.
		/// </summary>
		public float? GetAxisRaw()
		{
			float? axis = null;

			if(_type == InputType.DigitalAxis)
			{
				if(UnityEngine.Input.GetKey(_positive))
				{
					axis = _invert ? -AXIS_POSITIVE : AXIS_POSITIVE;
				}
				else if(UnityEngine.Input.GetKey(_negative))
				{
					axis = _invert ? -AXIS_NEGATIVE : AXIS_NEGATIVE;
				}
			}
			else if(_type == InputType.MouseAxis || _type == InputType.AnalogAxis)
			{
				if(_rawAxisName != null)
				{
					axis = UnityEngine.Input.GetAxisRaw(_rawAxisName);
					axis = _invert ? -axis : axis;
				}
			}
			else if(_type == InputType.GamepadAxis)
			{
				axis =	GamepadState.GetAxisRaw(_gamepadAxis, _gamepadIndex);
				axis = _invert ? -axis : axis;
			}

			if(axis.HasValue && Mathf.Abs(axis.Value) <= 0.0f)
				axis = null;

			return axis;
		}

		public bool? GetButton()
		{
			bool? value = null;

			if(_type == InputType.Button)
			{
				value = UnityEngine.Input.GetKey(_positive);
			}
			else if(_type == InputType.GamepadButton)
			{
				value = GamepadState.GetButton(_gamepadButton, _gamepadIndex);
			}
			else if(_type == InputType.RemoteButton)
			{
				value = _remoteButtonState == ButtonState.Pressed || _remoteButtonState == ButtonState.JustPressed;
			}
			else if(_type == InputType.AnalogButton || _type == InputType.GamepadAnalogButton)
			{
				value = _analogButtonState == ButtonState.Pressed || _analogButtonState == ButtonState.JustPressed;
			}

			if(value.HasValue && !value.Value)
				value = null;

			return value;
		}

		public bool? GetButtonDown()
		{
			bool? value = null;

			if(_type == InputType.Button)
			{
				value = UnityEngine.Input.GetKeyDown(_positive);
			}
			else if(_type == InputType.GamepadButton)
			{
				value = GamepadState.GetButtonDown(_gamepadButton, _gamepadIndex);
			}
			else if(_type == InputType.RemoteButton)
			{
				value = _remoteButtonState == ButtonState.JustPressed;
			}
			else if(_type == InputType.AnalogButton || _type == InputType.GamepadAnalogButton)
			{
				value = _analogButtonState == ButtonState.JustPressed;
			}

			if(value.HasValue && !value.Value)
				value = null;

			return value;
		}

		public bool? GetButtonUp()
		{
			bool? value = null;

			if(_type == InputType.Button)
			{
				value = UnityEngine.Input.GetKeyUp(_positive);
			}
			else if(_type == InputType.GamepadButton)
			{
				value = GamepadState.GetButtonUp(_gamepadButton, _gamepadIndex);
			}
			else if(_type == InputType.RemoteButton)
			{
				value = _remoteButtonState == ButtonState.JustReleased;
			}
			else if(_type == InputType.AnalogButton || _type == InputType.GamepadAnalogButton)
			{
				value = _analogButtonState == ButtonState.JustReleased;
			}

			if(value.HasValue && !value.Value)
				value = null;

			return value;
		}

		/// <summary>
		/// If the input type is set to "RemoteAxis" the axis value will be changed, else nothing will happen.
		/// </summary>
		public void SetRemoteAxisValue(float value)
		{
			if(_type == InputType.RemoteAxis)
			{
				_value = value;
			}
		}

		/// <summary>
		/// If the input type is set to "RemoteButton" the axis state will be changed, else nothing will happen.
		/// </summary>
		public void SetRemoteButtonState(ButtonState state)
		{
			if(_type == InputType.RemoteButton)
			{
				_remoteButtonState = state;
			}
		}

		public void Copy(InputBinding source)
		{
			_positive = source._positive;
			_negative = source._negative;
            _deadZoneType = source._deadZoneType;
			_deadZone = source._deadZone;
			_gravity = source._gravity;
			_sensitivity = source._sensitivity;
            _scale = source._scale;
			_snap = source._snap;
			_invert = source._invert;
			_type = source._type;
			_axis = source._axis;
			_joystick = source._joystick;
			_gamepadAxis = source._gamepadAxis;
			_gamepadButton = source._gamepadButton;
			_gamepadIndex = source._gamepadIndex;
		}

		public void Reset()
		{
			_value = AXIS_NEUTRAL;
			_remoteButtonState = ButtonState.Released;
			_analogButtonState = ButtonState.Released;
		}

        private float ApplyDeadZone(float axis)
        {
            if(Mathf.Abs(axis) <= _deadZone)
            {
                axis = AXIS_NEUTRAL;
            }
            else if(_deadZoneType == DeadZoneType.Remap)
            {
                axis = (axis - Mathf.Sign(axis) * _deadZone) / (1.0f - _deadZone);
            }

            return axis;
        }

		private void UpdateRawAxisName()
		{
			if(_type == InputType.MouseAxis)
			{
				if(_axis < 0 || _axis >= MAX_MOUSE_AXES)
				{
					string message = string.Format("Desired mouse axis is out of range. Mouse axis will be clamped to {0}.",
												   Mathf.Clamp(_axis, 0, MAX_MOUSE_AXES - 1));
					Debug.LogWarning(message);
				}

				_rawAxisName = string.Concat("mouse_axis_", Mathf.Clamp(_axis, 0, MAX_MOUSE_AXES - 1));
			}
			else if(_type == InputType.AnalogAxis || _type == InputType.AnalogButton)
			{
				if(_joystick < 0 || _joystick >= MAX_JOYSTICKS)
				{
					string message = string.Format("Desired joystick is out of range. Joystick has been clamped to {0}.",
												   Mathf.Clamp(_joystick, 0, MAX_JOYSTICKS - 1));
					Debug.LogWarning(message);
				}
				if(_axis >= MAX_JOYSTICK_AXES)
				{
					string message = string.Format("Desired joystick axis is out of range. Joystick axis will be clamped to {0}.",
												   Mathf.Clamp(_axis, 0, MAX_JOYSTICK_AXES - 1));
					Debug.LogWarning(message);
				}

				_rawAxisName = string.Concat("joy_", Mathf.Clamp(_joystick, 0, MAX_JOYSTICKS - 1),
									 "_axis_", Mathf.Clamp(_axis, 0, MAX_JOYSTICK_AXES - 1));
			}
			else
			{
				_rawAxisName = string.Empty;
			}
		}

		public static KeyCode StringToKey(string value)
		{
			return StringToEnum(value, KeyCode.None);
		}

        public static DeadZoneType StringToDeadZoneType(string value)
		{
			return StringToEnum(value, DeadZoneType.CutOff);
		}

		public static InputType StringToInputType(string value)
		{
			return StringToEnum(value, InputType.Button);
		}

		public static GamepadButton StringToGamepadButton(string value)
		{
			return StringToEnum(value, GamepadButton.ActionBottom);
		}

		public static GamepadAxis StringToGamepadAxis(string value)
		{
			return StringToEnum(value, GamepadAxis.LeftThumbstickX);
		}

		public static GamepadIndex StringToGamepadIndex(string value)
		{
			return StringToEnum(value, GamepadIndex.GamepadOne);
		}

		private static T StringToEnum<T>(string value, T defValue)
		{
			if(string.IsNullOrEmpty(value))
			{
				return defValue;
			}
			try
			{
				return (T)Enum.Parse(typeof(T), value, true);
			}
			catch
			{
				return defValue;
			}
		}

		public static InputBinding Duplicate(InputBinding source)
		{
            InputBinding duplicate = new InputBinding
            {
                _positive = source._positive,
                _negative = source._negative,
                _deadZoneType = source._deadZoneType,
                _deadZone = source._deadZone,
                _gravity = source._gravity,
                _sensitivity = source._sensitivity,
                _scale = source._scale,
				_snap = source._snap,
				_invert = source._invert,
				_type = source._type,
				_axis = source._axis,
				_joystick = source._joystick,
				_gamepadAxis = source._gamepadAxis,
				_gamepadButton = source._gamepadButton,
				_gamepadIndex = source._gamepadIndex,
			};

			return duplicate;
		}
	}
}