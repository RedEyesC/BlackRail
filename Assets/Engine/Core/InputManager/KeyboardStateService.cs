using UnityEngine;
using UnityEngine.Profiling;

namespace GameFramework.Input
{
    public class KeyboardStateService : IInputService
    {
        private Vector3 _lastMousePosition;
        private Vector3 _currentMousePosition;
        int _minMousePositionDelta;

        /// <summary>
        /// How many pixels(at least 1 pixel) the mouse pointer has to move to register it as input.
        /// </summary>
        public int MinMousePositionDelta
        {
            get { return _minMousePositionDelta; }
            set { _minMousePositionDelta = Mathf.Max(value, 1); }
        }

        /// <summary>
        /// Whether to take into account mouse movement. 
        /// In the editor this option is set to false by default to make testing easier.
        /// </summary>
        public bool RegisterMouseMovement { get; set; }

        public bool AnyInput { get; private set; }

        public void Startup()
        {
            _lastMousePosition = Vector3.zero;
            _currentMousePosition = Vector3.zero;
            _minMousePositionDelta = 20;
#if UNITY_EDITOR
            RegisterMouseMovement = false;
#else
            RegisterMouseMovement = true;
#endif
            AnyInput = false;
        }

        public void Shutdown()
        {
        }

        public void OnBeforeUpdate()
        {
            Profiler.BeginSample("KeyboardStateService.OnBeforeUpdate");
            _lastMousePosition = _currentMousePosition;
            _currentMousePosition = UnityEngine.Input.mousePosition;
            AnyInput = false;

            if(UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonDown(1) || 
                UnityEngine.Input.GetMouseButtonDown(2) || KeyUtils.IsAnyKeyDown())
            {
                AnyInput = true;
            }
            else if(RegisterMouseMovement)
            {
                Vector3 delta = _currentMousePosition - _lastMousePosition;
                AnyInput = delta.sqrMagnitude >= _minMousePositionDelta * _minMousePositionDelta;
            }
            Profiler.EndSample();
        }

        public void OnAfterUpdate()
        {
        }
    }
}
