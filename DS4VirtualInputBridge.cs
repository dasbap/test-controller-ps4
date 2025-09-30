using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using SharpDX.DirectInput;
using System.Text.Json;

namespace controller_ps4
{
    public class DS4VirtualInputBridge : IDisposable
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int KEY_PRESSED = 0x8000;

        private bool _isRunning;
        private Keyboard? _keyboard;
        private Mouse? _mouse;
        private readonly ViGEmClient _client;
        private readonly IDualShock4Controller _virtualController;
        private readonly TriggerAccelerator _leftTriggerAccelerator;
        private readonly TriggerAccelerator _rightTriggerAccelerator;
        private ControllerConfig _config = new();
        private int _lastMouseWheelValue;

        public DS4VirtualInputBridge()
        {
            _client = new ViGEmClient();
            _virtualController = _client.CreateDualShock4Controller();

            _leftTriggerAccelerator = new TriggerAccelerator(IsKeyPressed, 3.0);
            _rightTriggerAccelerator = new TriggerAccelerator(IsKeyPressed, 3.0);

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    Console.WriteLine("📄 Configuration chargée depuis le fichier");

                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;

                    if (root.TryGetProperty("keyMappings", out var keyMappings))
                    {
                        foreach (var prop in keyMappings.EnumerateObject())
                            _config.KeyMappings[prop.Name] = ExtractKeys(prop.Value);
                    }

                    if (root.TryGetProperty("combinations", out var combos))
                    {
                        foreach (var prop in combos.EnumerateObject())
                            _config.Combinations[prop.Name] = ExtractKeys(prop.Value);
                    }

                    if (root.TryGetProperty("mouseSettings", out var mouseSettings))
                    {
                        if (mouseSettings.TryGetProperty("rightStickSensitivity", out var sens))
                            _config.MouseSettings.RightStickSensitivity = sens.GetDouble();

                        if (mouseSettings.TryGetProperty("left", out var left))
                            _config.MouseSettings.Left = left.GetString() ?? "";

                        if (mouseSettings.TryGetProperty("right", out var right))
                            _config.MouseSettings.Right = right.GetString() ?? "";

                        if (mouseSettings.TryGetProperty("wheel", out var wheel))
                        {
                            if (wheel.TryGetProperty("up", out var up))
                                _config.MouseSettings.WheelUp = up.GetString() ?? "";
                            if (wheel.TryGetProperty("down", out var down))
                                _config.MouseSettings.WheelDown = down.GetString() ?? "";
                        }
                    }

                    Console.WriteLine("✅ Configuration chargée avec succès");
                }
                else
                {
                    Console.WriteLine("❌ Fichier config.json non trouvé");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur chargement config: {ex.Message}");
            }
        }

        private List<string> ExtractKeys(JsonElement element)
        {
            var keys = new List<string>();
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String)
                        keys.Add(item.GetString() ?? "");
            }
            else if (element.ValueKind == JsonValueKind.String)
                keys.Add(element.GetString() ?? "");
            return keys;
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

        private bool IsKeyPressed(int virtualKey) =>
            (GetAsyncKeyState(virtualKey) & KEY_PRESSED) != 0;

        private bool IsMouseButtonPressed(int mouseButton) =>
            (GetAsyncKeyState(mouseButton) & KEY_PRESSED) != 0;

        private bool IsKeyPressed(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return key.ToUpper() switch
            {
                "Z" => IsKeyPressed(0x5A),
                "Q" => IsKeyPressed(0x51),
                "S" => IsKeyPressed(0x53),
                "D" => IsKeyPressed(0x44),
                "W" => IsKeyPressed(0x57),
                "A" => IsKeyPressed(0x41),
                "E" => IsKeyPressed(0x45),
                "R" => IsKeyPressed(0x52),
                "T" => IsKeyPressed(0x54),
                "Y" => IsKeyPressed(0x59),
                "U" => IsKeyPressed(0x55),
                "I" => IsKeyPressed(0x49),
                "O" => IsKeyPressed(0x4F),
                "P" => IsKeyPressed(0x50),
                "F" => IsKeyPressed(0x46),
                "G" => IsKeyPressed(0x47),
                "H" => IsKeyPressed(0x48),
                "J" => IsKeyPressed(0x4A),
                "K" => IsKeyPressed(0x4B),
                "L" => IsKeyPressed(0x4C),
                "C" => IsKeyPressed(0x43),
                "V" => IsKeyPressed(0x56),
                "B" => IsKeyPressed(0x42),
                "N" => IsKeyPressed(0x4E),
                "M" => IsKeyPressed(0x4D),
                "X" => IsKeyPressed(0x58),
                "TAB" => IsKeyPressed(0x09),
                "UP" => IsKeyPressed(0x26),
                "DOWN" => IsKeyPressed(0x28),
                "LEFT" => IsKeyPressed(0x25),
                "RIGHT" => IsKeyPressed(0x27),
                "SPACE" => IsKeyPressed(0x20),
                "ESCAPE" => IsKeyPressed(0x1B),
                "LEFTSHIFT" => IsKeyPressed(0xA0),
                "LEFTCTRL" => IsKeyPressed(0xA2),
                "LEFTALT" => IsKeyPressed(0xA4),
                "LEFTCLICK" => IsMouseButtonPressed(0x01),
                "RIGHTCLICK" => IsMouseButtonPressed(0x02),
                "MOUSE4" => IsMouseButtonPressed(0x05),
                "MOUSE5" => IsMouseButtonPressed(0x06),
                _ => false
            };
        }

        private bool AreAllKeysPressed(List<string> keys) =>
            keys.All(IsKeyPressed);

        private bool AreAnyKeysPressed(List<string> keys) =>
            keys.Any(IsKeyPressed);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            InitializeInputDevices();
            _virtualController.Connect();
            ResetControllerAxes();
            _isRunning = true;

            Console.WriteLine("🎮 Bridge démarré - Tous les boutons actifs");

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                var inputState = ReadInputState();
                UpdateVirtualController(inputState);
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }

            _virtualController.Disconnect();
            Console.WriteLine("🛑 Bridge arrêté");
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

        private InputState ReadInputState()
        {
            var inputState = new InputState();

            foreach (var kvp in _config.KeyMappings)
                if (AreAnyKeysPressed(kvp.Value))
                    inputState.SetAction(kvp.Key, true);

            foreach (var kvp in _config.Combinations)
                if (AreAllKeysPressed(kvp.Value))
                    inputState.SetAction(kvp.Key, true);

            if (_mouse != null)
            {
                _mouse.Poll();
                var mouseState = _mouse.GetCurrentState();
                float sensitivity = (float)_config.MouseSettings.RightStickSensitivity;
                inputState.RightX = (byte)(128 + Math.Clamp((int)(mouseState.X * sensitivity), -127, 127));
                inputState.RightY = (byte)(128 + Math.Clamp((int)(mouseState.Y * sensitivity), -127, 127));
                HandleMouseWheel(inputState, mouseState);
            }

            return inputState;
        }

        private void HandleMouseWheel(InputState inputState, MouseState mouseState)
        {
            if (mouseState.Z > _lastMouseWheelValue)
                if (!string.IsNullOrEmpty(_config.MouseSettings.WheelUp))
                    inputState.SetAction(_config.MouseSettings.WheelUp, true);

            if (mouseState.Z < _lastMouseWheelValue)
                if (!string.IsNullOrEmpty(_config.MouseSettings.WheelDown))
                    inputState.SetAction(_config.MouseSettings.WheelDown, true);

            _lastMouseWheelValue = mouseState.Z;
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
            _virtualController.SetButtonState(DualShock4Button.ShoulderLeft, s.L1);
            _virtualController.SetButtonState(DualShock4Button.ShoulderRight, s.R1);
            _virtualController.SetButtonState(DualShock4Button.ThumbLeft, s.L3);
            _virtualController.SetButtonState(DualShock4Button.ThumbRight, s.R3);
            _virtualController.SetButtonState(DualShock4Button.Options, s.Options);
            _virtualController.SetButtonState(DualShock4SpecialButton.Ps, s.PS);
            _virtualController.SetButtonState(DualShock4SpecialButton.Touchpad, s.Touchpad);

            _virtualController.SetAxisValue(DualShock4Axis.LeftThumbX, (byte)(128 + Math.Clamp(s.LeftX, -127, 127)));
            _virtualController.SetAxisValue(DualShock4Axis.LeftThumbY, (byte)(128 + Math.Clamp(s.LeftY, -127, 127)));
            _virtualController.SetAxisValue(DualShock4Axis.RightThumbX, s.RightX);
            _virtualController.SetAxisValue(DualShock4Axis.RightThumbY, s.RightY);
            _virtualController.SetSliderValue(DualShock4Slider.LeftTrigger, s.L2);
            _virtualController.SetSliderValue(DualShock4Slider.RightTrigger, s.R2);

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

    public class InputState
    {
        public bool DpadUp { get; set; }
        public bool DpadDown { get; set; }
        public bool DpadLeft { get; set; }
        public bool DpadRight { get; set; }
        public bool Square { get; set; }
        public bool Cross { get; set; }
        public bool Circle { get; set; }
        public bool Triangle { get; set; }
        public bool L1 { get; set; }
        public bool R1 { get; set; }
        public bool L3 { get; set; }
        public bool R3 { get; set; }
        public bool Share { get; set; }
        public bool Options { get; set; }
        public bool Touchpad { get; set; }
        public bool PS { get; set; }
        public int LeftX { get; set; }
        public int LeftY { get; set; }
        public byte RightX { get; set; } = 128;
        public byte RightY { get; set; } = 128;
        public byte L2 { get; set; }
        public byte R2 { get; set; }

        public void SetAction(string action, bool value)
        {
            switch (action.ToLower())
            {
                case "dpadup": DpadUp = value; break;
                case "dpaddown": DpadDown = value; break;
                case "dpadleft": DpadLeft = value; break;
                case "dpadright": DpadRight = value; break;
                case "square": Square = value; break;
                case "cross": Cross = value; break;
                case "circle": Circle = value; break;
                case "triangle": Triangle = value; break;
                case "l1": L1 = value; break;
                case "r1": R1 = value; break;
                case "l3": L3 = value; break;
                case "r3": R3 = value; break;
                case "share": Share = value; break;
                case "options": Options = value; break;
                case "touchpad": Touchpad = value; break;
                case "ps": PS = value; break;
                case "stickleftup": LeftY = -127; break;
                case "stickleftdown": LeftY = 127; break;
                case "stickleftleft": LeftX = -127; break;
                case "stickleftright": LeftX = 127; break;
                case "l2": L2 = 255; break;
                case "r2": R2 = 255; break;
            }
        }
    }

    public class ControllerConfig
    {
        public Dictionary<string, List<string>> KeyMappings { get; set; } = new();
        public Dictionary<string, List<string>> Combinations { get; set; } = new();
        public MouseSettings MouseSettings { get; set; } = new();
    }

    public class MouseSettings
    {
        public double RightStickSensitivity { get; set; } = 2.0;
        public string Left { get; set; } = "";
        public string Right { get; set; } = "";
        public string WheelUp { get; set; } = "";
        public string WheelDown { get; set; } = "";
    }
}
