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
using System.Threading;
using System.Threading.Tasks;

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
            if (PrevInstance() == false)
            {
                var app = new ProcessStartInfo();
                app.FileName = @"Z:\Git\boilersGraphics\boilersGraphics\bin\Debug\boilersGraphics.exe";
                _process = Process.Start(app);
                Thread.Sleep(1000);
            }

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
            bmp.Save(ms, ImageFormat.Png);
            var array = ms.ToArray();
            ms.Close();

            ViewData["ImgSrc"] = String.Format("data:image/png;base64,{0}", Convert.ToBase64String(array));
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
    }
}
