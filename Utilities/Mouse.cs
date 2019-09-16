using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;

namespace FullRareSetManager.Utilities
{
    public class Mouse
    {
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_MIDDOWN = 0x0020;
        public const int MOUSEEVENTF_MIDUP = 0x0040;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const int MOUSEEVENTF_RIGHTUP = 0x0010;
        public const int MOUSE_EVENT_WHEEL = 0x800;
        private const int MOVEMENT_DELAY = 10;
        private const int CLICK_DELAY = 1;

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public static POINT GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }

        public static void SetCursorPos(Vector2 pos)
        {
            SetCursorPos((int) pos.X, (int) pos.Y);
        }

        public static void SetCursorPos(POINT pos)
        {
            SetCursorPos(pos.X, pos.Y);
        }

        /*
        public static void SetCursorPosAndLeftClick(Vector2 pos)
        {
            SetCursorPosAndLeftClick((int) pos.X, (int) pos.Y);
        }
        */
        public static void SetCursorPosAndLeftClick(Vector2 coords, int extraDelay)
        {
            var posX = (int) coords.X;
            var posY = (int) coords.Y;
            SetCursorPos(posX, posY);
            Thread.Sleep(MOVEMENT_DELAY + extraDelay);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(CLICK_DELAY);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void SetCursorPosAndLeftClick(int xpos, int ypos, int extraDelay)
        {
            SetCursorPos(xpos, ypos);
            Thread.Sleep(MOVEMENT_DELAY);
            LeftClick();
        }

        private static void LeftClick()
        {
            LeftMouseDown();
            Thread.Sleep(CLICK_DELAY);
            LeftMouseUp();
        }

        public static void LeftMouseDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }

        public static void LeftMouseUp()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void RightMouseDown()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        }

        public static void RightMouseUp()
        {
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public static void VerticalScroll(bool forward, int clicks)
        {
            if (forward)
                mouse_event(MOUSE_EVENT_WHEEL, 0, 0, clicks * 120, 0);
            else
                mouse_event(MOUSE_EVENT_WHEEL, 0, 0, -(clicks * 120), 0);
        }
    }
}
