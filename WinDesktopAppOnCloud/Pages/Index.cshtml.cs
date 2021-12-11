using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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

        public IActionResult OnPostMouseMove(int x, int y)
        {
            StartDesktopAppProcess();

            if (_process == null)
            {
                _process = Process.GetProcessesByName("boilersGraphics").First();
            }
            var point = new Point(x, y);
            //PostMessageでマウスポインタが移動したことをDesktopApp側に伝える
            Trace.WriteLine($"SendMessage hWnd={_process.MainWindowHandle}, Msg={WM_MOUSEMOVE}, wParam={0x0}, lParam={point}");
            PostMessage(_process.MainWindowHandle, WM_MOUSEMOVE, 0, new POINT() { x = (short)point.X, y = (short)point.Y });
            ShowIfError();
            
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
        public static extern int PostMessage(IntPtr hWnd, int mssg, int wParam, POINT lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int PostMessage(IntPtr hWnd, int mssg, int wParam, int lParam);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SetCapture(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;
        public const int MK_LBUTTON = 0x0001;
        public const int WM_MOUSEHOVER = 0x02A1;
    }
}
