using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WinDesktopAppOnCloud.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
        private static readonly string AppPath = @"Z:\Git\boilersGraphics\boilersGraphics\bin\Debug\boilersGraphics.exe";

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
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
            if (ViewData["session"] == null)
            {
                var options = new AppiumOptions();
                options.AddAdditionalCapability("app", "Root");
                var session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), options);

                WindowsElement applicationWindow = null;
                
                var openWindows = session.FindElementsByClassName("Window").ToList();
                foreach (var window in openWindows)
                {
                    if (window.GetAttribute("Name").StartsWith("boiler's Graphics"))
                    {
                        applicationWindow = window;
                        break;
                    }
                }

                if (applicationWindow == null)
                {
                    options = new AppiumOptions();
                    options.AddAdditionalCapability("app", AppPath);
                    options.AddAdditionalCapability("deviceName", "WindowsPC");
                    session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), options);
                }
                else
                {
                    // Attaching to existing Application Window
                    var topLevelWindowHandle = applicationWindow.GetAttribute("NativeWindowHandle");
                    topLevelWindowHandle = int.Parse(topLevelWindowHandle).ToString("X");

                    var options1 = new AppiumOptions();
                    options1.AddAdditionalCapability("deviceName", "WindowsPC");
                    options1.AddAdditionalCapability("appTopLevelWindow", topLevelWindowHandle);
                    session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), options1);
                }
                ViewData["session"] = session;
            }
        }

        private void PrintScreen()
        {
            StartDesktopAppProcess();

            var session = (WindowsDriver<WindowsElement>)ViewData["session"];
            var base64 = session.GetScreenshot().AsBase64EncodedString;
            ViewData["ImgSrc"] = String.Format("data:image/jpeg;base64,{0}", base64);
        }

        public IActionResult OnPostSetPoint(int x, int y)
        {
            var data = new Dictionary<string, string>() { { "x", x.ToString() }, { "y", y.ToString() } };
            return new JsonResult(data);
        }

        public void OnPostClick(int x, int y)
        {
            StartDesktopAppProcess();

            try
            {
                var session = (WindowsDriver<WindowsElement>)ViewData["session"];
                var actions = new Actions(session);
                actions.MoveToElement(session.FindElementByClassName("MainWindow"), x, y);
                actions.Click();
                actions.Perform();
                actions.Perform();
            }
            catch (WebDriverException)
            {
                _logger.LogError("OnPostClick failed");
            }
        }

        public IActionResult OnPostMouseMove(int x, int y)
        {
            StartDesktopAppProcess();

            var session = (WindowsDriver<WindowsElement>)ViewData["session"];
            var actions = new Actions(session);
            actions.MoveToElement(session.FindElementByClassName("Window"), x, y);
            actions.Perform();

            PrintScreen();
            var data = new Dictionary<string, string>() { { "src", ViewData["ImgSrc"].ToString() } };
            return new JsonResult(data);
        }

        public void OnPostSetCapture()
        {
            StartDesktopAppProcess();
        }


        public void OnPostReleaseCapture()
        {
            //Trace.WriteLine("ReleaseCapture");

            //ReleaseCapture();
        }

        public void OnPostShutDown()
        {
            var session = (WindowsDriver<WindowsElement>)ViewData["session"];
            if (session != null)
            {
                var actions = new Actions(session);
                actions.SendKeys(OpenQA.Selenium.Keys.Alt + OpenQA.Selenium.Keys.F4 + OpenQA.Selenium.Keys.Alt);
                actions.Perform();
                session.WindowHandles.Select(x => session.SwitchTo().Window(x)).ToList().ForEach(x => x.Dispose());
                session.Quit();
                ViewData["session"] = null;
            }
        }
    }
}
