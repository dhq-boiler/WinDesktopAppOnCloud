using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WinDesktopAppOnCloud.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private Process _process;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        private bool PrevInstance()
        {
            var processes = Process.GetProcessesByName("boilersGraphics");
            if (processes.Length >= 1)
                return true;
            else
                return false;
        }

        public void OnGet()
        {
            StartDesktopAppProcessAndPrintScreen();
        }

        private void StartDesktopAppProcessAndPrintScreen()
        {
            StartDesktopAppProcess();

            PrintScreen();
        }

        private void StartDesktopAppProcess()
        {
            if (PrevInstance() == false)
            {
                var app = new ProcessStartInfo();

                app.FileName = @"Z:\Git\boilersGraphics\boilersGraphics\bin\Debug\boilersGraphics.exe"; //将来的にはビルドシステムも搭載して、GitHubからソースコードをクローンして自鯖でビルドしてオンラインデバッグできるようにする
                _process = Process.Start(app);
                Thread.Sleep(1000);
            }
        }

        private void PrintScreen()
        {
            if (_process == null)
            {
                _process = Process.GetProcessesByName("boilersGraphics").First();
            }

            IntPtr hWnd = _process.MainWindowHandle;
            while (hWnd == IntPtr.Zero)
            {
                _process.Refresh();
                hWnd = _process.MainWindowHandle;
            }
            IntPtr winDC = GetWindowDC(hWnd);
            //ウィンドウの大きさを取得
            RECT winRect = new RECT();
            GetWindowRect(hWnd, ref winRect);
            //Bitmapの作成
            Bitmap bmp = new Bitmap(winRect.right - winRect.left,
                winRect.bottom - winRect.top);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //Graphicsのデバイスコンテキストを取得
            IntPtr hDC = g.GetHdc();

            PrintWindow(hWnd, hDC, 0);
            //Bitmapに画像をコピーする
            BitBlt(hDC, 0, 0, bmp.Width, bmp.Height,
                winDC, 0, 0, SRCCOPY);
            //解放
            g.ReleaseHdc(hDC);
            g.Dispose();
            ReleaseDC(hWnd, winDC);

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Jpeg);
            var array = ms.ToArray();
            ms.Close();

            ViewData["ImgSrc"] = String.Format("data:image/jpeg;base64,{0}", Convert.ToBase64String(array));
        }

        private byte[] PrintScreenAsByteArray()
        {
            if (_process == null)
            {
                _process = Process.GetProcessesByName("boilersGraphics").First();
            }

            IntPtr hWnd = _process.MainWindowHandle;
            while (hWnd == IntPtr.Zero)
            {
                _process.Refresh();
                hWnd = _process.MainWindowHandle;
            }
            IntPtr winDC = GetWindowDC(hWnd);
            //ウィンドウの大きさを取得
            RECT winRect = new RECT();
            GetWindowRect(hWnd, ref winRect);
            //Bitmapの作成
            Bitmap bmp = new Bitmap(winRect.right - winRect.left,
                winRect.bottom - winRect.top);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //Graphicsのデバイスコンテキストを取得
            IntPtr hDC = g.GetHdc();

            PrintWindow(hWnd, hDC, 0);
            //Bitmapに画像をコピーする
            BitBlt(hDC, 0, 0, bmp.Width, bmp.Height,
                winDC, 0, 0, SRCCOPY);
            //解放
            g.ReleaseHdc(hDC);
            g.Dispose();
            ReleaseDC(hWnd, winDC);

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Jpeg);
            var array = ms.ToArray();
            ms.Close();

            return array;
        }

        public IActionResult OnPostSetPoint(int x, int y)
        {
            var data = new Dictionary<string, string>() { { "x", x.ToString() }, { "y", y.ToString() } };
            return new JsonResult(data);
        }

        public void OnPostClick(int x, int y)
        {
            StartDesktopAppProcess();

            if (_process == null)
            {
                _process = Process.GetProcessesByName("boilersGraphics").First();
            }

            var point = new Point(x, y);
            PostMessage(_process.MainWindowHandle, WM_LBUTTONDOWN, new IntPtr(MK_LBUTTON), new IntPtr(PointToParam(point)));
            ShowIfError();
            PostMessage(_process.MainWindowHandle, WM_LBUTTONUP, new IntPtr(MK_LBUTTON), new IntPtr(PointToParam(point)));
            ShowIfError();
        }

        public IActionResult OnPostMouseMove(int x, int y)
        {
            StartDesktopAppProcess();

            if (_process == null)
            {
                _process = Process.GetProcessesByName("boilersGraphics").First();
            }
            var point = new Point(x, y);

            SetForegroundWindow(_process.MainWindowHandle);
            ShowIfError();
            SendMessage(_process.MainWindowHandle, WM_NCHITTEST, IntPtr.Zero, new IntPtr(PointToParam(point)));
            ShowIfError();
            SendMessage(_process.MainWindowHandle, WM_SETCURSOR, _process.MainWindowHandle, new IntPtr(WM_MOUSEMOVE << 16 | HTCLIENT));
            ShowIfError();
            PostMessage(_process.MainWindowHandle, WM_MOUSEMOVE, IntPtr.Zero, new IntPtr(PointToParam(point)));
            ShowIfError();

            //var inputMouseMove = new ClickOnPointTool.INPUT();
            //inputMouseMove.Type = 0; /// input type mouse
            //inputMouseMove.Data.Mouse.X = x;
            //inputMouseMove.Data.Mouse.Y = y;
            //inputMouseMove.Data.Mouse.Flags = 0x0001; /// MOUSEEVENTF_MOVE Movement occurred

            //var inputs = new ClickOnPointTool.INPUT[] { inputMouseMove };
            //ClickOnPointTool.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(ClickOnPointTool.INPUT)));
            //var wpfPoint = WriteMouseCoordinatesInWPFUnits(_process.MainWindowHandle);

            PrintScreen();
            var data = new Dictionary<string, string>() { { "src", ViewData["ImgSrc"].ToString() } };
            return new JsonResult(data);
        }

        public void OnPostSetCapture()
        {
            StartDesktopAppProcess();

            if (_process == null)
            {
                _process = Process.GetProcessesByName("boilersGraphics").First();
            }

            //Trace.WriteLine("SetCapture");

            SetCapture(_process.MainWindowHandle);
        }

        public ActionResult ShowImage()
        {
            return File(PrintScreenAsByteArray(), "image/jpeg");
        }

        public void OnPostReleaseCapture()
        {
            //Trace.WriteLine("ReleaseCapture");

            ReleaseCapture();
        }

        public void OnPostShutDown()
        {
            if (_process != null)
                _process.Kill();
        }

        private int PointToParam(Point point)
        {
            return (int)point.Y << 16 | (int)point.X & 0xFFFF;
        }

        private void ShowIfError()
        {
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode != 0)
            {
                StringBuilder message = new StringBuilder(255);
                FormatMessage(
                  FORMAT_MESSAGE_FROM_SYSTEM,
                  IntPtr.Zero,
                  (uint)errorCode,
                  0,
                  message,
                  message.Capacity,
                  IntPtr.Zero);
                _logger.LogError(message.ToString());
            }
            else
            {
                StringBuilder message = new StringBuilder(255);
                FormatMessage(
                  FORMAT_MESSAGE_FROM_SYSTEM,
                  IntPtr.Zero,
                  (uint)errorCode,
                  0,
                  message,
                  message.Capacity,
                  IntPtr.Zero);
                _logger.LogInformation(message.ToString());
            }
        }

        [DllImport("kernel32.dll")]
        public static extern uint FormatMessage(uint dwFlags, IntPtr lpSource,
                                                uint dwMessageId, uint dwLanguageId,
                                                StringBuilder lpBuffer, int nSize,
                                                IntPtr Arguments);
        public const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        private const int SRCCOPY = 13369376;
        private const int CAPTUREBLT = 1073741824;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr hDestDC,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hSrcDC,
            int xSrc,
            int ySrc,
            int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);


        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hwnd,
            ref RECT lpRect);
        
        [DllImport("User32.dll")]
        private extern static bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        public struct POINT
        {
            public short x;
            public short y;
        };


        //送信するためのメソッド(文字も可能)
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int mssg, int wParam, POINT lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int mssg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int mssg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int PostMessage(IntPtr hWnd, int mssg, int wParam, POINT lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int PostMessage(IntPtr hWnd, int mssg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int PostMessage(IntPtr hWnd, int mssg, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SetCapture(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        public const int SW_SHOW = 5;

        public enum GetWindowType : uint
        {
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is highest in the Z order.
            /// <para/>
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDFIRST = 0,
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDLAST = 1,
            /// <summary>
            /// The retrieved handle identifies the window below the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDNEXT = 2,
            /// <summary>
            /// The retrieved handle identifies the window above the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDPREV = 3,
            /// <summary>
            /// The retrieved handle identifies the specified window's owner window, if any.
            /// </summary>
            GW_OWNER = 4,
            /// <summary>
            /// The retrieved handle identifies the child window at the top of the Z order,
            /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
            /// The function examines only child windows of the specified window. It does not examine descendant windows.
            /// </summary>
            GW_CHILD = 5,
            /// <summary>
            /// The retrieved handle identifies the enabled popup window owned by the specified window (the
            /// search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled
            /// popup windows, the retrieved handle is that of the specified window.
            /// </summary>
            GW_ENABLEDPOPUP = 6
        }

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;
        public const int WM_MOUSEHOVER = 0x02A1;
        public const int WM_SETCURSOR = 0x0020;
        public const int WM_NCHITTEST = 0x0084;

        public const int MK_CONTROL = 0x0008;
        public const int MK_LBUTTON = 0x0001;
        public const int MK_MBUTTON = 0x0010;
        public const int MK_RBUTTON = 0x0002;
        public const int MK_SHIFT = 0x0004;
        public const int MK_XBUTTON1 = 0x0020;
        public const int MK_XBUTTON2 = 0x0040;

        /// <summary>
        /// サイズ変更境界線を持つウィンドウの境界線内。
        /// </summary>
        public const int HTBORDER = 18;

        /// <summary>
        /// サイズ変更可能なウィンドウの下向き境界線(ユーザーはマウスをクリックしてウィンドウの垂直方向のサイズを変更できます)。
        /// </summary>
        public const int HTBOTTOM = 15;

        /// <summary>
        /// サイズ変更可能なウィンドウの境界線の左下隅(ユーザーはマウスをクリックして、ウィンドウの対角線のサイズを変更できます)。
        /// </summary>
        public const int HTBOTTOMLEFT = 16;

        /// <summary>
        /// サイズ変更可能なウィンドウの境界線の右下隅(ユーザーはマウスをクリックして、ウィンドウの斜め方向のサイズを変更できます)。
        /// </summary>
        public const int HTBOTTOMRIGHT = 17;

        /// <summary>
        /// タイトル バー内。
        /// </summary>
        public const int HTAPTION = 2;

        /// <summary>
        /// クライアント領域内。
        /// </summary>
        public const int HTCLIENT = 1;

        /// <summary>
        /// [閉じる]ボタン をクリックします。
        /// </summary>
        public const int LOSE = 20;

        /// <summary>
        /// 画面の背景またはウィンドウ間の分割線(HTNOWHERE と同じ) で 、DefWindowProc 関数がエラーを示すシステム ビープ音を生成する場合を除きます。
        /// </summary>
        public const int HTERROR = -2;

        /// <summary>
        /// サイズ ボックス内(HTSIZE と同じ)。
        /// </summary>
        public const int HTGROWBOX = 4;

        /// <summary>
        /// [ヘルプ] ボタン 。
        /// </summary>
        public const int HTHELP = 21;
        
        /// <summary>
        /// 水平スクロール バー内。
        /// </summary>
        public const int HTHSCROLL = 6;
        
        /// <summary>
        /// サイズ変更可能なウィンドウの左側の境界線(ユーザーはマウスをクリックしてウィンドウの水平方向のサイズを変更できます)。
        /// </summary>
        public const int HTLEFT = 10;
        
        /// <summary>
        /// メニュー内。
        /// </summary>
        public const int HTMENU = 5;
        
        /// <summary>
        /// [最大化]ボタン をクリックします。
        /// </summary>
        public const int HTMAXBUTTON = 9;
        
        /// <summary>
        /// [最小化]ボタン をクリックします。
        /// </summary>
        public const int HTMINBUTTON = 8;
       
        /// <summary>
        /// 画面の背景またはウィンドウ間の分割線。
        /// </summary>
        public const int HTNOWHERE = 0;
        
        /// <summary>
        /// [最小化]ボタン をクリックします。
        /// </summary>
        public const int HTREDUCE = 8;
        
        /// <summary>
        /// サイズ変更可能なウィンドウの右側の境界線(ユーザーはマウスをクリックしてウィンドウの水平方向のサイズを変更できます)。
        /// </summary>
        public const int HTRIGHT = 11;
        
        /// <summary>
        /// サイズ ボックス内(HTGROWBOX と同じ)。
        /// </summary>
        public const int HTSIZE = 4;
        
        /// <summary>
        /// ウィンドウ メニューまたは子ウィンドウの[閉じる] ボタン。
        /// </summary>
        public const int HTSYSMENU = 3;
        
        /// <summary>
        /// ウィンドウの上方向の境界線。
        /// </summary>
        public const int HTTOP = 12;
        
        /// <summary>
        /// ウィンドウの境界線の左上隅。
        /// </summary>
        public const int HTTOPLEFT = 13;
        
        /// <summary>
        /// ウィンドウの境界線の右上隅。
        /// </summary>
        public const int HTTOPRIGHT = 14;
        
        /// <summary>
        /// 同じスレッド内の別のウィンドウで現在カバーされているウィンドウでは(メッセージは、そのうちの 1 つが HTTRANSPARENT ではないコードを返すまで、同じスレッド内の基になるウィンドウに送信されます)。
        /// </summary>
        public const int HTTRANSPARENT = -1;
        
        /// <summary>
        /// 垂直スクロール バー内。
        /// </summary>
        public const int HTVSCROLL = 7;
        
        /// <summary>
        /// [最大化]ボタン をクリックします。
        /// </summary>
        public const int HTZOOM = 9;

        [Flags]
        public enum MouseEventFlags
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out MousePoint lpMousePoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void SetCursorPosition(MousePoint point)
        {
            SetCursorPos(point.X, point.Y);
        }

        public static MousePoint GetCursorPosition()
        {
            MousePoint currentMousePoint;
            var gotPoint = GetCursorPos(out currentMousePoint);
            if (!gotPoint) { currentMousePoint = new MousePoint(0, 0); }
            return currentMousePoint;
        }

        public static void MouseEvent(MouseEventFlags value)
        {
            MousePoint position = GetCursorPosition();

            mouse_event
                ((int)value,
                 position.X,
                 position.Y,
                 0,
                 0)
                ;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MousePoint
        {
            public int X;
            public int Y;

            public MousePoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public class ClickOnPointTool
        {

            [DllImport("user32.dll")]
            static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

            [DllImport("user32.dll")]
            internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

#pragma warning disable 649
            internal struct INPUT
            {
                public UInt32 Type;
                public MOUSEKEYBDHARDWAREINPUT Data;
            }

            [StructLayout(LayoutKind.Explicit)]
            internal struct MOUSEKEYBDHARDWAREINPUT
            {
                [FieldOffset(0)]
                public MOUSEINPUT Mouse;
            }

            internal struct MOUSEINPUT
            {
                public Int32 X;
                public Int32 Y;
                public UInt32 MouseData;
                public UInt32 Flags;
                public UInt32 Time;
                public IntPtr ExtraInfo;
            }

#pragma warning restore 649


            public static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
            {
                GetCursorPos(out var oldPos);

                /// get screen coordinates
                ClientToScreen(wndHandle, ref clientPoint);

                /// set cursor on coords, and press mouse
                SetCursorPos((int)clientPoint.X, (int)clientPoint.Y);

                var inputMouseDown = new INPUT();
                inputMouseDown.Type = 0; /// input type mouse
                inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

                var inputMouseUp = new INPUT();
                inputMouseUp.Type = 0; /// input type mouse
                inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

                var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                /// return mouse 
                SetCursorPos((int)oldPos.X, (int)oldPos.Y);
            }

        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private Point ConvertPixelsToUnits(IntPtr hWnd, int x, int y)
        {
            // get the system DPI
            IntPtr dDC = GetDC(hWnd); // Get desktop DC
            int dpi = GetDeviceCaps(dDC, 88);
            bool rv = ReleaseDC(hWnd, dDC).ToInt32() != 0;

            // WPF's physical unit size is calculated by taking the 
            // "Device-Independant Unit Size" (always 1/96)
            // and scaling it by the system DPI
            double physicalUnitSize = (1d / 96d) * (double)dpi;
            Point wpfUnits = new Point(physicalUnitSize * (double)x,
            physicalUnitSize * (double)y);

            return wpfUnits;
        }
        private Point WriteMouseCoordinatesInWPFUnits(IntPtr hWnd)
        {
            if (GetCursorPos(out var p))
            {
                Point wpfPoint = ConvertPixelsToUnits(hWnd, p.X, p.Y);
                return wpfPoint;
            }
            return null;
        }
    }
}
