using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using SharpDX.DirectInput;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace controller_ps4
{
    public class DS4VirtualInputBridge : IDisposable
    {
        private enum VirtualKeyCodes
        {
            Escape = 0x01,      // Échap
            Z = 0x11,           // Z
            A = 0x10,           // A
            S = 0x1F,           // S
            D = 0x20,           // D
            Space = 0x39,// Espace
            LeftCtrl = 0x1D,    // Ctrl gauche
            RightCtrl = 0x9D,   // Ctrl droit
            LeftShift = 0x2A,   // Maj gauche
            RightShift = 0x36,  // Maj droit
            Q = 0x1E,           // Q (en AZERTY0x20, c'est "A"0x20, mais le code physique reste 0x51)
            E = 0x12,           // E
            R = 0x13,           // R
            F = 0x21,           // F
            W = 0x2C,           // W (anciennement "Z" en QWERTY)
            X = 0x2D,           // X
            C = 0x2E,           // C
            V = 0x2F,           // V
            Tab = 0x0f,         // Tab
            CapsLock = 0x4A,    // Verr. Maj
            Enter = 0x1C,       // Entrée
            Backspace = 0x0E,   // Retour arrière
            LeftAlt = 0x38,     // Alt gauche
            RightAlt = 0xB8,    // AltGr
            Up = 0xC8,          // Flèche haut
            Down = 0xD0,        // Flèche bas
            Left = 0xCb,        // Flèche gauche
            Right = 0xCD,       // Flèche droite
            Num0 = 0x52,        // 0
            Num1 = 0x4f,        // 1
            Num2 = 0x50,        // 2
            Num3 = 0x51,        // 3
            Num4 = 0x4B,        // 4
            Num5 = 0x4C,        // 5
            Num6 = 0x4D,        // 6
            Num7 = 0x47,        // 7
            Num8 = 0x48,        // 8
            Num9 = 0x49,        // 9
            Circumflex = 0x4A,  // ^ (sur AZERTY)
            ParenRight = 0x4A,  // )
            Equal = 0x4A,       // =
            Dollar = 0x4A,      // $
            Asterisk = 0x4A,    // *
            Mu = 0x4A,          // µ
            M = 0x4A,           // M
            UGrave = 0x4A,      // ù
            Comma = 0x4A,       // 0x20,
            Semicolon = 0x4A,   // ;
            Exclamation = 0x4A, // !
            PrintScreen = 0x4A, // Impr. écran
            Delete = 0x4A,      // Suppr
            Insert = 0xD2,      // Insérer
            Home = 0x4A,        // Début
            End = 0x4A,         // Fin
            PageUp = 0x4A,      // Page précédente
            PageDown = 0x4A,    // Page suivante
            F1 = 0x3B,          // F1
            F2 = 0x3C,          // F2
            F3 = 0x3D,          // F3
            F4 = 0x3E,          // F4
            F5 = 0x3F,          // F5
            F6 = 0x40,          // F6
            F7 = 0x41,          // F7
            F8 = 0x42,          // F8
            F9 = 0x43,          // F9
            F10 = 0x44,         // F10
            F11 = 0x57,         // F11
            F12 = 0x58,         // F12
        }

        private bool _isRunning;
        private Keyboard? _keyboard;
        private Mouse? _mouse;
        private readonly ViGEmClient _client;
        private readonly IDualShock4Controller _virtualController;

        public DS4VirtualInputBridge()
        {
            _client = new ViGEmClient();
            _virtualController = _client.CreateDualShock4Controller();
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            InitializeInputDevices();
            _virtualController.Connect();
            ResetControllerAxes();

            _isRunning = true;
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                var inputState = ReadInputState();
                UpdateVirtualController(inputState);

                if (inputState.Options)
                    _isRunning = false;

                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }

            _virtualController.Disconnect();
        }

        private void ResetControllerAxes()
        {
            _virtualController.SetAxisValue(DualShock4Axis.LeftThumbX, 128);
            _virtualController.SetAxisValue(DualShock4Axis.LeftThumbY, 128);
            _virtualController.SetAxisValue(DualShock4Axis.RightThumbX, 128);
            _virtualController.SetAxisValue(DualShock4Axis.RightThumbY, 128);
            _virtualController.SetSliderValue(DualShock4Slider.LeftTrigger, 0);
            _virtualController.SetSliderValue(DualShock4Slider.RightTrigger, 0);
        }

        private void InitializeInputDevices()
        {
            var directInput = new DirectInput();
            _keyboard = new Keyboard(directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();

            _mouse = new Mouse(directInput);
            _mouse.Properties.BufferSize = 128;
            _mouse.Acquire();
        }

        private InputState ReadInputState()
        {
            if (_keyboard != null && _mouse != null)
            {
                _keyboard.Poll();
                var keyboardState = _keyboard.GetCurrentState();

                _mouse.Poll();
                var mouseState = _mouse.GetCurrentState();

                var wheelDelta = mouseState.Z;
                bool trianglePressed = wheelDelta != 0;

                bool leftClick = mouseState.Buttons[1];
                bool rightClick = mouseState.Buttons[0];

                bool mousebtn4 = mouseState.Buttons[3];
                bool mousebtn5 = mouseState.Buttons[4];

                int leftX = 0;
                int leftY = 0;

                if (keyboardState.IsPressed((Key)VirtualKeyCodes.Q)) leftX -= 1;
                if (keyboardState.IsPressed((Key)VirtualKeyCodes.D)) leftX += 1;
                if (keyboardState.IsPressed((Key)VirtualKeyCodes.Z)) leftY -= 1;
                if (keyboardState.IsPressed((Key)VirtualKeyCodes.S)) leftY += 1;

                double length = Math.Sqrt(leftX * leftX + leftY * leftY);
                if (length > 0)
                {
                    leftX = (int)(leftX / length * 127);
                    leftY = (int)(leftY / length * 127);
                }

                return new InputState
                {
                    DpadUp = keyboardState.IsPressed((Key)VirtualKeyCodes.Up),
                    DpadDown = keyboardState.IsPressed((Key)VirtualKeyCodes.Down),
                    DpadLeft = keyboardState.IsPressed((Key)VirtualKeyCodes.Left),
                    DpadRight = keyboardState.IsPressed((Key)VirtualKeyCodes.Right),

                    Square = keyboardState.IsPressed((Key)VirtualKeyCodes.R),
                    Cross = keyboardState.IsPressed((Key)VirtualKeyCodes.Space),
                    Circle = keyboardState.IsPressed((Key)VirtualKeyCodes.F),
                    Triangle = trianglePressed,

                    L1 = keyboardState.IsPressed((Key)VirtualKeyCodes.LeftCtrl),
                    R1 = mousebtn5,

                    L1R1 = keyboardState.IsPressed((Key)VirtualKeyCodes.A),

                    Share = keyboardState.IsPressed((Key)VirtualKeyCodes.PrintScreen),
                    Options = keyboardState.IsPressed((Key)VirtualKeyCodes.Backspace),
                    PS = keyboardState.IsPressed((Key)VirtualKeyCodes.Escape),
                    Touchpad = keyboardState.IsPressed((Key)VirtualKeyCodes.Tab),

                    L3 = keyboardState.IsPressed((Key)VirtualKeyCodes.LeftShift),
                    R3 = mousebtn4,

                    LeftX = (byte)(128 + leftX),
                    LeftY = (byte)(128 + leftY),
                    RightX = (byte)(128 + Math.Clamp(mouseState.X *2, -127, 127)),
                    RightY = (byte)(128 + Math.Clamp(mouseState.Y *2, -127, 127)),
                    
                    L2 = leftClick ? (byte)255 : (byte)0.0, 
                    R2 = rightClick ? (byte)255 : (byte)0,
            }
            ;
            }

            return new InputState();
        }



        private void UpdateVirtualController(InputState s)
        {
            _virtualController.SetDPadDirection(
                s.DpadUp && s.DpadRight ? DualShock4DPadDirection.Northeast :
                s.DpadUp && s.DpadLeft ? DualShock4DPadDirection.Northwest :
                s.DpadDown && s.DpadRight ? DualShock4DPadDirection.Southeast :
                s.DpadDown && s.DpadLeft ? DualShock4DPadDirection.Southwest :
                s.DpadUp ? DualShock4DPadDirection.North :
                s.DpadDown ? DualShock4DPadDirection.South :
                s.DpadLeft ? DualShock4DPadDirection.West :
                s.DpadRight ? DualShock4DPadDirection.East :
                DualShock4DPadDirection.None);

            _virtualController.SetButtonState(DualShock4Button.Square, s.Square);
            _virtualController.SetButtonState(DualShock4Button.Cross, s.Cross);
            _virtualController.SetButtonState(DualShock4Button.Circle, s.Circle);
            _virtualController.SetButtonState(DualShock4Button.Triangle, s.Triangle);

            _virtualController.SetButtonState(DualShock4Button.ShoulderLeft, (s.L1 || s.L1R1));
            _virtualController.SetButtonState(DualShock4Button.ShoulderRight, (s.R1 || s.L1R1));

            _virtualController.SetButtonState(DualShock4Button.Share, s.Share);
            _virtualController.SetButtonState(DualShock4Button.Options, s.Options);
            _virtualController.SetButtonState(DualShock4SpecialButton.Ps, s.PS);
            _virtualController.SetButtonState(DualShock4SpecialButton.Touchpad, s.Touchpad);

            _virtualController.SetButtonState(DualShock4Button.ThumbLeft, s.L3);
            _virtualController.SetButtonState(DualShock4Button.ThumbRight, s.R3);

            _virtualController.SetAxisValue(DualShock4Axis.LeftThumbX, s.LeftX);
            _virtualController.SetAxisValue(DualShock4Axis.LeftThumbY, s.LeftY);
            _virtualController.SetAxisValue(DualShock4Axis.RightThumbX, s.RightX);
            _virtualController.SetAxisValue(DualShock4Axis.RightThumbY, s.RightY);

            _virtualController.SetSliderValue(DualShock4Slider.LeftTrigger,s.L2);

            _virtualController.SetSliderValue(DualShock4Slider.RightTrigger,s.R2); 
            _virtualController.SubmitReport();
        }

        public void Dispose()
        {
            _virtualController?.Disconnect();
            _virtualController?.Dispose();
            _client?.Dispose();
            _keyboard?.Unacquire();
            _keyboard?.Dispose();
            _mouse?.Unacquire();
            _mouse?.Dispose();
        }
    }
}
