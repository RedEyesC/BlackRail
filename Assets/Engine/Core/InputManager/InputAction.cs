using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameFramework.Input
{
	public class InputAction
	{
		public const int MAX_BINDINGS = 16;

		private string _name;
		private string _description;
		private List<InputBinding> _bindings;

		public ReadOnlyCollection<InputBinding> Bindings
		{
			get { return _bindings.AsReadOnly(); }
		}

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				if(Application.isPlaying)
				{
					Debug.LogWarning("You should not change the name of an input action at runtime");
				}
			}
		}

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		public bool AnyInput
		{
			get
			{
				foreach(var binding in _bindings)
				{
					if(binding.AnyInput)
						return true;
				}

				return false;
			}
		}

		public InputAction() :
			this("New Action") { }
		
		public InputAction(string name)
		{
			_name = name;
			_description = string.Empty;
			_bindings = new List<InputBinding>();
		}
		
		public void Initialize()
		{
			foreach(var binding in _bindings)
			{
				binding.Initialize();
			}
		}
		
		public void Update(float deltaTime)
		{
			foreach(var binding in _bindings)
			{
				binding.Update(deltaTime);
			}
		}
		
		public float GetAxis()
		{
			float? value = null;
			foreach(var binding in _bindings)
			{
				value = binding.GetAxis();
				if(value.HasValue)
					break;
			}

			return value ?? InputBinding.AXIS_NEUTRAL;
		}

		///<summary>
		///	Returns raw input with no sensitivity or smoothing applyed.
		/// </summary>
		public float GetAxisRaw()
		{
			float? value = null;
			foreach(var binding in _bindings)
			{
				value = binding.GetAxisRaw();
				if(value.HasValue)
					break;
			}

			return value ?? InputBinding.AXIS_NEUTRAL;
		}
		
		public bool GetButton()
		{
			bool? value = null;
			foreach(var binding in _bindings)
			{
				value = binding.GetButton();
				if(value.HasValue)
					break;
			}

			return value ?? false;
		}
		
		public bool GetButtonDown()
		{
			bool? value = null;
			foreach(var binding in _bindings)
			{
				value = binding.GetButtonDown();
				if(value.HasValue)
					break;
			}

			return value ?? false;
		}
		
		public bool GetButtonUp()
		{
			bool? value = null;
			foreach(var binding in _bindings)
			{
				value = binding.GetButtonUp();
				if(value.HasValue)
					break;
			}

			return value ?? false;
		}

		public InputBinding GetBinding(int index)
		{
			if(index >= 0 && index < _bindings.Count)
				return _bindings[index];

			return null;
		}

		public InputBinding CreateNewBinding()
		{
			if(_bindings.Count < MAX_BINDINGS)
			{
				InputBinding binding = new InputBinding();
				_bindings.Add(binding);

				return binding;
			}

			return null;
		}

		public InputBinding CreateNewBinding(InputBinding source)
		{
			if(_bindings.Count < MAX_BINDINGS)
			{
				InputBinding binding = InputBinding.Duplicate(source);
				_bindings.Add(binding);

				return binding;
			}

			return null;
		}

		public InputBinding InsertNewBinding(int index)
		{
			if(_bindings.Count < MAX_BINDINGS)
			{
				InputBinding binding = new InputBinding();
				_bindings.Insert(index, binding);

				return binding;
			}

			return null;
		}

		public InputBinding InsertNewBinding(int index, InputBinding source)
		{
			if(_bindings.Count < MAX_BINDINGS)
			{
				InputBinding binding = InputBinding.Duplicate(source);
				_bindings.Insert(index, binding);

				return binding;
			}

			return null;
		}

		public void DeleteBinding(int index)
		{
			if(index >= 0 && index < _bindings.Count)
				_bindings.RemoveAt(index);
		}

		public void SwapBindings(int fromIndex, int toIndex)
		{
			if(fromIndex >= 0 && fromIndex < _bindings.Count && toIndex >= 0 && toIndex < _bindings.Count)
			{
				var temp = _bindings[toIndex];
				_bindings[toIndex] = _bindings[fromIndex];
				_bindings[fromIndex] = temp;
			}
		}

		public void Copy(InputAction source)
		{
			_name = source._name;
			_description = source._description;

			_bindings.Clear();
			foreach(var binding in source._bindings)
			{
				_bindings.Add(InputBinding.Duplicate(binding));
			}
		}

		public void Reset()
		{
			foreach(var binding in _bindings)
			{
				binding.Reset();
			}
		}

		public static InputAction Duplicate(InputAction source)
		{
			return Duplicate(source._name, source);
		}

		public static InputAction Duplicate(string name, InputAction source)
		{
			InputAction duplicate = new InputAction();
			duplicate._name = name;
			duplicate._description = source._description;
			duplicate._bindings = new List<InputBinding>();
			foreach(var binding in source._bindings)
			{
				duplicate._bindings.Add(InputBinding.Duplicate(binding));
			}

			return duplicate;
		}
    }
}