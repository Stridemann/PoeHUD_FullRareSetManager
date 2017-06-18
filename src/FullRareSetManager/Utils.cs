using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;

namespace Utils
{
    public class MouseUtils
    {
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public const int MOUSEEVENTF_MIDDOWN = 0x0020;
        public const int MOUSEEVENTF_MIDUP = 0x0040;

        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const int MOUSEEVENTF_RIGHTUP = 0x0010;


        ////////////////////////////////////////////////////////////


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }


        private const int ActionDelay = 100;

        public static void LeftMouseClick(Vector2 pos)
        {
            LeftMouseClick((int)pos.X, (int)pos.Y);
        }
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            Thread.Sleep(ActionDelay);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            Thread.Sleep(ActionDelay);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        public static void MidMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            Thread.Sleep(ActionDelay);
            mouse_event(MOUSEEVENTF_MIDDOWN, xpos, ypos, 0, 0);
            Thread.Sleep(ActionDelay);
            mouse_event(MOUSEEVENTF_MIDUP, xpos, ypos, 0, 0);
        }

        public static void LeftMouseDown(int xpos, int ypos)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
        }
        public static void LeftMouseUp(int xpos, int ypos)
        {
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        public static void RightMouseDown(int xpos, int ypos)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, xpos, ypos, 0, 0);
        }
        public static void RightMouseUp(int xpos, int ypos)
        {
            mouse_event(MOUSEEVENTF_RIGHTUP, xpos, ypos, 0, 0);
        }

    }



    public static class VirtualKeyboard
    {
        [DllImport("user32.dll")]
        static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private static int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private static int KEYEVENTF_KEYUP = 0x0002;

        private const int ActionDelay = 50;


        public static void KeyDown(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
        }

        public static void KeyUp(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);//0x7F
        }

        public static void KeyDownUp(Keys key)
        {
            KeyDown(key);
            Thread.Sleep(ActionDelay);
            KeyUp(key);
        }
    }
}
