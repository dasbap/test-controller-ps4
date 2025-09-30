using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace controller_ps4
{
    internal class Findinputcode
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint virtualKeyCode, uint scanCode,
            byte[] keyboardState, [Out] StringBuilder receivingBuffer, int bufferSize, uint flags);

        [DllImport("user32.dll")]
        private static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern int GetKeyNameText(int lParam, StringBuilder lpString, int nSize);

        private const int KEY_PRESSED = 0x8000;
        private static HashSet<int> pressedKeys = new HashSet<int>();

        public static void Run()
        {
            Console.WriteLine("Détection touche virtuelle -> scan code physique");
            Console.WriteLine("Appuyez sur Échap pour quitter.\n");

            while (true)
            {
                for (int vkCode = 1; vkCode < 255; vkCode++)
                {
                    short keyState = GetAsyncKeyState(vkCode);

                    if ((keyState & KEY_PRESSED) != 0 && !pressedKeys.Contains(vkCode))
                    {
                        pressedKeys.Add(vkCode);

                        // Convertir VK en scan code physique
                        int scanCode = MapVirtualKey((uint)vkCode, 0);

                        // Obtenir le nom physique via l'API Windows
                        string physicalKeyName = GetPhysicalKeyName(scanCode);

                        // Obtenir le caractère produit (pour identifier le layout)
                        char producedChar = GetCharFromVirtualKey(vkCode);

                        Console.WriteLine($"VK: 0x{vkCode:X2} -> Scan: 0x{scanCode:X2} -> Physique: {physicalKeyName} -> Caractère: '{(producedChar != 0 ? producedChar : ' ')}'");

                        if (vkCode == 0x1B) // Échap
                        {
                            Console.WriteLine("Arrêt du programme.");
                            return;
                        }
                    }
                    else if ((keyState & KEY_PRESSED) == 0 && pressedKeys.Contains(vkCode))
                    {
                        pressedKeys.Remove(vkCode);
                    }
                }

                System.Threading.Thread.Sleep(10);
            }
        }

        private static string GetPhysicalKeyName(int scanCode)
        {
            int lParam = (scanCode << 16);
            StringBuilder name = new StringBuilder(256);
            GetKeyNameText(lParam, name, 256);

            return name.Length > 0 ? name.ToString() : $"Scan{scanCode:X2}";
        }

        private static char GetCharFromVirtualKey(int vkCode)
        {
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            StringBuilder charBuffer = new StringBuilder(5);
            int result = ToUnicode((uint)vkCode, 0, keyboardState, charBuffer, charBuffer.Capacity, 0);

            return (result == 1 && charBuffer.Length > 0) ? charBuffer[0] : '\0';
        }
    }
}