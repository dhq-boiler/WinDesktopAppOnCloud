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

        public void OnPostMouseMove()
        {
            StartDesktopAppProcess();

            if (_process == null)
            {
                _process = Process.GetProcessesByName("boilersGraphics").First();
            }

            //SendMessageでマウスポインタが移動したことをDesktopApp側に伝える
            Trace.WriteLine($"SendMessage hWnd={_process.MainWindowHandle}, Msg={WM_MOUSEMOVE}, wParam={0x0}, lParam={JsonToPoint()}");
            SendMessage(_process.MainWindowHandle, WM_MOUSEMOVE, 0x0, new IntPtr(PointToParam(JsonToPoint())));
            PrintScreen();
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

        private int PointToParam(Point point)
        {
            return (int)point.X << 16 | (int)point.Y;
        }

        private Point JsonToPoint()
        {
            StreamReader reader = new StreamReader(Response.Body);
            Response.Body.Seek(0, SeekOrigin.Begin);
            string str = reader.ReadToEnd();
            var dict = HttpUtility.ParseQueryString(str);
            var point = new Point();
            point.X = int.Parse(dict["x"]);
            point.Y = int.Parse(dict["y"]);
            return point;
        }

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

        //送信するためのメソッド(文字も可能)
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, IntPtr wMsg, IntPtr wParam, ref IntPtr lParam);
        
        [DllImport("user32.dll")]
        static extern IntPtr SetCapture(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;
        public const int MK_LBUTTON = 0x0001;
    }
}
