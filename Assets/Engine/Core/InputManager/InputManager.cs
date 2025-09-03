using GameFramework.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Input
{
    internal class InputManager : GameModule
    {
        public new int priority = 6;

        private bool _ignoreTimescale = true;

        private static ControlScheme _playerScheme;
        private static Dictionary<Type, IInputService> _services;
        private static Dictionary<string, ControlScheme> _schemeLookup;

        public override void Destroy()
        {

            ;
            foreach (var entry in _services)
                entry.Value.Shutdown();

            _services.Clear();
        }

        public override void Start()
        {
            _services = new Dictionary<Type, IInputService>();
            _schemeLookup = new Dictionary<string, ControlScheme>();

            AddDefaultServices();
        }


        private void AddDefaultServices()
        {
            AddService<KeyboardStateService>();
            AddService<GamepadStateService>();
        }


        public override void Update(float nowTime, float elapseSeconds)
        {

            foreach (var item in _services.Values)
                item.OnBeforeUpdate();

            if (_playerScheme != null)
            {
                float deltaTime = _ignoreTimescale ? Time.unscaledDeltaTime : Time.deltaTime;
                _playerScheme.Update(deltaTime);

            }


            foreach (var item in _services.Values)
                item.OnAfterUpdate();
        }


        public static void SetPlayerScheme(string controlSchemeName)
        {
            ControlScheme scheme = GetControlScheme(controlSchemeName);

            _playerScheme = scheme;
        }


        private static T AddService<T>() where T : class, IInputService, new()
        {
            if (!_services.ContainsKey(typeof(T)))
            {
                T service = new T();
                service.Startup();

                _services[typeof(T)] = service;
            }

            return _services[typeof(T)] as T;
        }


        public static T GetService<T>() where T : class, IInputService
        {
            IInputService service = null;
            if (_services.TryGetValue(typeof(T), out service))
                return (T)service;

            return null;
        }

        public static InputAction CreateButton(string controlSchemeName, string buttonName, KeyCode primaryKey)
        {

            ControlScheme scheme = GetControlScheme(controlSchemeName);
            InputAction action = scheme.CreateNewAction(buttonName);
            InputBinding primary = action.CreateNewBinding();
            primary.Type = InputType.Button;
            primary.Positive = primaryKey;

            action.Initialize();

            return action;
        }


        public static InputAction CreateDigitalAxis(string controlSchemeName, string axisName, KeyCode positive, KeyCode negative, float gravity, float sensitivity, bool snap)
        {
            ControlScheme scheme = GetControlScheme(controlSchemeName);
            InputAction action = scheme.CreateNewAction(axisName);
            InputBinding primary = action.CreateNewBinding();
            primary.Type = InputType.DigitalAxis;
            primary.Positive = positive;
            primary.Negative = negative;
            primary.Gravity = gravity;
            primary.Sensitivity = sensitivity;
            primary.Snap = snap;

            action.Initialize();

            return action;
        }


        public static ControlScheme GetControlScheme(string name)
        {
            ControlScheme scheme = null;
            if (!_schemeLookup.TryGetValue(name, out scheme))
            {
                scheme = new ControlScheme(name);
                _schemeLookup.Add(name, scheme);
            }

            return scheme;

        }


        public static float GetAxis(string controlSchemeName, string name)
        {
            InputAction action = GetAction(controlSchemeName, name);
            if (action != null)
            {
                return action.GetAxis();
            }
            else
            {
                Debug.LogError(string.Format("An axis named \'{0}\' does not exist in the active input configuration for player {1}", name, controlSchemeName));
                return 0.0f;
            }
        }

        public static bool GetButton(string controlSchemeName, string name)
        {
            InputAction action = GetAction(controlSchemeName, name);
            if (action != null)
            {
                return action.GetButton();
            }
            else
            {
                Debug.LogError(string.Format("An button named \'{0}\' does not exist in the active input configuration for player {1}", name, controlSchemeName));
                return false;
            }
        }

        public static bool GetButtonDown(string controlSchemeName, string name)
        {
            InputAction action = GetAction(controlSchemeName, name);
            if (action != null)
            {
                return action.GetButtonDown();
            }
            else
            {
                Debug.LogError(string.Format("An button named \'{0}\' does not exist in the active input configuration for player {1}", name, controlSchemeName));
                return false;
            }
        }

        public static bool GetButtonUp(string controlSchemeName, string name)
        {
            InputAction action = GetAction(controlSchemeName, name);
            if (action != null)
            {
                return action.GetButtonUp();
            }
            else
            {
                Debug.LogError(string.Format("An button named \'{0}\' does not exist in the active input configuration for player {1}", name, controlSchemeName));
                return false;
            }
        }

        public static InputAction GetAction(string controlSchemeName, string actionName)
        {
            ControlScheme scheme = null;
            if (!_schemeLookup.TryGetValue(controlSchemeName, out scheme))
            {
                return null;
            }

            return scheme.GetAction(actionName);
        }

    }
}
