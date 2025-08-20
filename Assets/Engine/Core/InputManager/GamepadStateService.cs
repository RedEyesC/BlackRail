using UnityEngine;
using UnityEngine.Profiling;

namespace GameFramework.Input
{
    public class GamepadStateService : IInputService
    {
        private const int NUMBER_OF_GAMEPADS = 4;
        private const int NUMBER_OF_BUTTONS = 14;
        private const int NUMBER_OF_AXES = 8;
        private const float MIN_AXIS_DELTA = 0.1f;

        private bool[,] _buttonStates;
        private bool[,] _axisStates;
        private float[,] _lastAxisValues;
        private IGamepadStateAdapter _adapter;

        public void SetAdapter(IGamepadStateAdapter adapter)
        {
            if(adapter != _adapter)
            {
                _adapter = adapter;
                Reset();
            }
        }

        public void Startup()
        {
            _buttonStates = new bool[NUMBER_OF_GAMEPADS, NUMBER_OF_BUTTONS];
            _axisStates = new bool[NUMBER_OF_GAMEPADS, NUMBER_OF_AXES];
            _lastAxisValues = new float[NUMBER_OF_GAMEPADS, NUMBER_OF_AXES];
            _adapter = null;
            Reset();
        }

        public void Shutdown()
        {
        }

        public void OnBeforeUpdate()
        {
            Profiler.BeginSample("GamepadStateService.OnBeforeUpdate");

            if(_adapter != null)
            {
                for(int gi = 0; gi < NUMBER_OF_GAMEPADS; gi++)
                {
                    for(int bi = 0; bi < NUMBER_OF_BUTTONS; bi++)
                    {
                        _buttonStates[gi, bi] = _adapter.GetButtonDown((GamepadButton)bi, (GamepadIndex)gi);
                    }

                    for(int ai = 0; ai < NUMBER_OF_AXES; ai++)
                    {
                        float value = _adapter.GetAxis((GamepadAxis)ai, (GamepadIndex)gi);
                        _axisStates[gi, ai] = Mathf.Abs(value - _lastAxisValues[gi, ai]) >= MIN_AXIS_DELTA;
                        _lastAxisValues[gi, ai] = value;
                    }
                }
            }
            
            Profiler.EndSample();
        }

        public void OnAfterUpdate()
        {
        }

        public bool AnyInput(GamepadIndex gamepad)
        {
            if(_adapter == null)
                return false;

            for(int bi = 0; bi < NUMBER_OF_BUTTONS; bi++)
            {
                if(_buttonStates[(int)gamepad, bi])
                    return true;
            }

            for(int ai = 0; ai < NUMBER_OF_AXES; ai++)
            {
                if(_axisStates[(int)gamepad, ai])
                    return true;
            }

            return false;
        }

        private void Reset()
        {
            for(int gi = 0; gi < NUMBER_OF_GAMEPADS; gi++)
            {
                for(int bi = 0; bi < NUMBER_OF_BUTTONS; bi++)
                {
                    _buttonStates[gi, bi] = false;
                }

                for(int ai = 0; ai < NUMBER_OF_AXES; ai++)
                {
                    _axisStates[gi, ai] = false;
                    _lastAxisValues[gi, ai] = 0.0f;
                }
            }
        }
    }
}
