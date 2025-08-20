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
        private ControlScheme _playerScheme;

        private static Dictionary<Type, IInputService> _services;
        private static Dictionary<string, ControlScheme> _schemeLookup;
        private static Dictionary<string, Dictionary<string, InputAction>> _actionLookup;

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
            _actionLookup = new Dictionary<string, Dictionary<string, InputAction>>();

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

        public static InputAction CreateDigitalAxis(string controlSchemeName, string axisName, KeyCode positive, KeyCode negative, float gravity, float sensitivity)
        {
            return CreateDigitalAxis(controlSchemeName, axisName, positive, negative, KeyCode.None, KeyCode.None, gravity, sensitivity);
        }

        public static InputAction CreateDigitalAxis(string controlSchemeName, string axisName, KeyCode positive, KeyCode negative,
                                                        KeyCode altPositive, KeyCode altNegative, float gravity, float sensitivity)
        {
            ControlScheme scheme = GetControlScheme(controlSchemeName);
            if (scheme == null)
            {
                Debug.LogError(string.Format("A control scheme named \'{0}\' does not exist", controlSchemeName));
                return null;
            }
            if (_actionLookup[controlSchemeName].ContainsKey(axisName))
            {
                Debug.LogError(string.Format("The control scheme named {0} already contains an action named {1}", controlSchemeName, axisName));
                return null;
            }

            InputAction action = scheme.CreateNewAction(axisName);
            InputBinding primary = action.CreateNewBinding();
            primary.Type = InputType.DigitalAxis;
            primary.Positive = positive;
            primary.Negative = negative;
            primary.Gravity = gravity;
            primary.Sensitivity = sensitivity;

            InputBinding secondary = action.CreateNewBinding(primary);
            secondary.Positive = altPositive;
            secondary.Negative = altNegative;

            action.Initialize();
            _actionLookup[controlSchemeName][axisName] = action;

            return action;
        }


        public static ControlScheme GetControlScheme(string name)
        {
            ControlScheme scheme = null;
            if (_schemeLookup.TryGetValue(name, out scheme))
                return scheme;

            return null;
        }

    }
}
