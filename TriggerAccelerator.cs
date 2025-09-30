using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace controller_ps4
{
    public class TriggerAccelerator
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int KEY_PRESSED = 0x8000;

        private readonly Func<string, bool> _isKeyPressedFunc;
        private readonly double _accelerationFactor;
        private readonly double _maxValue;
        private readonly Dictionary<string, long> _keyPressStartTimes = new();

        public TriggerAccelerator(Func<string, bool> isKeyPressedFunc, double accelerationFactor = 2.0, double maxValue = 255.0)
        {
            _isKeyPressedFunc = isKeyPressedFunc;
            _accelerationFactor = accelerationFactor;
            _maxValue = maxValue;
        }

        public byte CalculateTriggerValue(List<string> keys)
        {
            if (!AreAnyKeysPressed(keys))
            {
                foreach (var key in keys)
                {
                    _keyPressStartTimes.Remove(key);
                }
                return 0;
            }

            string? pressedKey = null;
            foreach (var key in keys)
            {
                if (_isKeyPressedFunc(key))
                {
                    pressedKey = key;
                    break;
                }
            }

            if (pressedKey == null) return 0;

            if (!_keyPressStartTimes.ContainsKey(pressedKey))
            {
                _keyPressStartTimes[pressedKey] = DateTime.UtcNow.Ticks;
            }

            long startTime = _keyPressStartTimes[pressedKey];
            long currentTime = DateTime.UtcNow.Ticks;
            double elapsedSeconds = (currentTime - startTime) / (double)TimeSpan.TicksPerSecond;

            double value = _maxValue * (1 - Math.Exp(-_accelerationFactor * elapsedSeconds));

            return (byte)Math.Clamp(value, 0, _maxValue);
        }

        private bool AreAnyKeysPressed(List<string> keys)
        {
            foreach (var key in keys)
            {
                if (_isKeyPressedFunc(key))
                    return true;
            }
            return false;
        }
    }
}