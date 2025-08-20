using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameFramework.Input
{

	public class ControlScheme
	{

		private string _name;

		private string _description;

		private bool _isExpanded;

		private string _uniqueID;

		private List<InputAction> _actions;

		public ReadOnlyCollection<InputAction> Actions
		{
			get { return _actions.AsReadOnly(); }
		}

		public bool IsExpanded
		{
			get { return _isExpanded; }
			set { _isExpanded = value; }
		}

		public string UniqueID
		{
			get { return _uniqueID; }
			set { _uniqueID = value; }
		}

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				if(Application.isPlaying)
				{
					Debug.LogWarning("You should not change the name of a control scheme at runtime");
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
				foreach(var action in _actions)
				{
					if(action.AnyInput)
						return true;
				}

				return false;
			}
		}
		
		public ControlScheme() :
			this("New Scheme") { }
		
		public ControlScheme(string name)
		{
			_actions = new List<InputAction>();
			_name = name;
			_description = "";
			_isExpanded = false;
			_uniqueID = GenerateUniqueID();
		}

		public void Initialize()
		{
			foreach(var action in _actions)
			{
				action.Initialize();
			}
		}

		public void Update(float deltaTime)
		{
			foreach(var action in _actions)
			{
				action.Update(deltaTime);
			}
		}

		public void Reset()
		{
			foreach(var action in _actions)
			{
				action.Reset();
			}
		}

		public InputAction GetAction(int index)
		{
			if(index >= 0 && index < _actions.Count)
				return _actions[index];

			return null;
		}

		public InputAction GetAction(string name)
		{
			return _actions.Find(obj => obj.Name == name);
		}

		public InputAction CreateNewAction(string name)
		{
			InputAction action = new InputAction(name);
			_actions.Add(action);

			return action;
		}

		public InputAction CreateNewAction(string name, InputAction source)
		{
			InputAction action = InputAction.Duplicate(name, source);
			_actions.Add(action);

			return action;
		}

		public InputAction InsertNewAction(int index, string name)
		{
			InputAction action = new InputAction(name);
			_actions.Insert(index, action);

			return action;
		}

		public InputAction InsertNewAction(int index, string name, InputAction source)
		{
			InputAction action = InputAction.Duplicate(name, source);
			_actions.Insert(index, action);

			return action;
		}

		public void DeleteAction(InputAction action)
		{
			_actions.Remove(action);
		}

		public void DeleteAction(int index)
		{
			if(index >= 0 && index < _actions.Count)
				_actions.RemoveAt(index);
		}

		public void DeleteAction(string name)
		{
			_actions.RemoveAll(obj => obj.Name == name);
		}

		public void SwapActions(int fromIndex, int toIndex)
		{
			if(fromIndex >= 0 && fromIndex < _actions.Count && toIndex >= 0 && toIndex < _actions.Count)
			{
				var temp = _actions[toIndex];
				_actions[toIndex] = _actions[fromIndex];
				_actions[fromIndex] = temp;
			}
		}

        public void ChangeJoystick(int joystick)
        {
            if(joystick >= 0 && joystick < InputBinding.MAX_JOYSTICKS)
            {
                foreach(var action in _actions)
                {
                    foreach(var binding in action.Bindings)
                    {
                        binding.Joystick = joystick;
                    }
                }
            }
            else
            {
                Debug.LogFormat("Cannnot replace control scheme joystick. Joystick {0} is out of range.", joystick);
            }
        }

        public void ChangeGamepad(GamepadIndex gamepad)
        {
            foreach(var action in _actions)
            {
                foreach(var binding in action.Bindings)
                {
                    binding.GamepadIndex = gamepad;
                }
            }
        }

		public Dictionary<string, InputAction> GetActionLookupTable()
		{
			Dictionary<string, InputAction> table = new Dictionary<string, InputAction>();
			foreach(InputAction action in _actions)
			{
				table[action.Name] = action;
			}

			return table;
		}

        public bool IsKeyUsedInAnyAction(KeyCode key, out KeyUsageResult usage)
        {
            usage = new KeyUsageResult();

            for(int ai = 0; ai < _actions.Count; ai++)
            {
                for(int bi = 0; bi < _actions[ai].Bindings.Count; bi++)
                {
                    InputBinding binding = _actions[ai].Bindings[bi];
                    bool isTheRightType = binding.Type == InputType.Button || binding.Type == InputType.DigitalAxis;
                    bool isUsingTheKey = binding.Positive == key || binding.Negative == key;

                    if(isTheRightType && isUsingTheKey)
                    {
                        usage = new KeyUsageResult
                        {
                            ControlSchemeName = _name,
                            ActionIndex = ai,
                            BindingIndex = bi
                        };

                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsKeyUsedInAnyAction(KeyCode key)
        {
            for(int ai = 0; ai < _actions.Count; ai++)
            {
                for(int bi = 0; bi < _actions[ai].Bindings.Count; bi++)
                {
                    InputBinding binding = _actions[ai].Bindings[bi];
                    bool isTheRightType = binding.Type == InputType.Button || binding.Type == InputType.DigitalAxis;
                    bool isUsingTheKey = binding.Positive == key || binding.Negative == key;

                    if(isTheRightType && isUsingTheKey)
                        return true;
                }
            }

            return false;
        }

		public static ControlScheme Duplicate(ControlScheme source)
		{
			return Duplicate(source.Name, source);
		}

		public static ControlScheme Duplicate(string name, ControlScheme source)
		{
			ControlScheme duplicate = new ControlScheme();
			duplicate._name = name;
			duplicate._description = source._description;
			duplicate._uniqueID = GenerateUniqueID(); 
			duplicate._actions = new List<InputAction>();
			foreach(var action in source._actions)
			{
				duplicate._actions.Add(InputAction.Duplicate(action));
			}
			
			return duplicate;
		}

		public static string GenerateUniqueID()
		{
			return Guid.NewGuid().ToString("N");
		}
    }
}