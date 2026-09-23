using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FliqloClock
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        private static string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");

        public static void Log(string msg)
        {
            try
            {
                File.AppendAllText(logFile, DateTime.Now.ToString("HH:mm:ss.fff") + ": " + msg + Environment.NewLine);
            }
            catch {}
        }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Log("Main started. Args count=" + args.Length + " (" + string.Join(" ", args) + ")");

                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    Log("ThreadException: " + e.Exception.ToString());
                };

                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    Log("UnhandledException: " + e.ExceptionObject.ToString());
                };

                try
                {
                    SetProcessDpiAwareness(2);
                }
                catch
                {
                    try { SetProcessDPIAware(); } catch {}
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string firstArg = args.Length > 0 ? args[0].ToLower().Trim() : "";
                string secondArg = null;

                if (firstArg.Length > 2 && (firstArg.StartsWith("/") || firstArg.StartsWith("-")))
                {
                    if (firstArg[2] == ':')
                    {
                        secondArg = firstArg.Substring(3);
                        firstArg = firstArg.Substring(0, 2);
                    }
                }

                if (args.Length > 1 && string.IsNullOrEmpty(secondArg))
                {
                    secondArg = args[1];
                }

                Log("Parsed firstArg=[" + firstArg + "] secondArg=[" + (secondArg ?? "") + "]");

                if (firstArg == "/c" || firstArg == "-c")
                {
                    IntPtr parentHwnd = IntPtr.Zero;
                    if (!string.IsNullOrEmpty(secondArg))
                    {
                        long hwndVal;
                        if (long.TryParse(secondArg, out hwndVal))
                        {
                            parentHwnd = new IntPtr(hwndVal);
                        }
                    }
                    Log("Starting SettingsForm with parentHwnd=" + parentHwnd);
                    Application.Run(new SettingsForm(parentHwnd));
                }
                else if (firstArg == "/p" || firstArg == "-p")
                {
                    if (string.IsNullOrEmpty(secondArg)) return;
                    long hwndVal;
                    if (long.TryParse(secondArg, out hwndVal))
                    {
                        Application.Run(new ScreensaverForm(new IntPtr(hwndVal)));
                    }
                }
                else if (firstArg == "/w" || firstArg == "-w" || firstArg == "/window" || firstArg == "/test")
                {
                    Log("Starting Windowed Mode");
                    Application.Run(new ScreensaverForm(Rectangle.Empty, true));
                }
                else
                {
                    Log("Calling ShowScreensaver()");
                    ShowScreensaver();
                }
            }
            catch (Exception ex)
            {
                Log("Main Catch: " + ex.ToString());
            }
        }

        private static void ShowScreensaver()
        {
            ScreensaverForm primaryForm = null;
            Log("AllScreens count=" + Screen.AllScreens.Length);

            foreach (Screen screen in Screen.AllScreens)
            {
                Log("Evaluating screen: " + screen.DeviceName + " Bounds=" + screen.Bounds + " Primary=" + screen.Primary);
                if (screen.Primary || primaryForm == null)
                {
                    if (primaryForm != null)
                    {
                        primaryForm.Show();
                    }
                    Log("Constructing primaryForm...");
                    primaryForm = new ScreensaverForm(screen.Bounds, false);
                    Log("Constructed primaryForm successfully.");
                }
                else
                {
                    ScreensaverForm secondaryForm = new ScreensaverForm(screen.Bounds, false);
                    secondaryForm.FormClosed += delegate
                    {
                        Application.Exit();
                    };
                    secondaryForm.Show();
                }
            }

            if (primaryForm != null)
            {
                primaryForm.FormClosed += delegate
                {
                    Log("primaryForm FormClosed fired -> Application.Exit()");
                    Application.Exit();
                };
                Log("Calling Application.Run(primaryForm)...");
                Application.Run(primaryForm);
                Log("Application.Run(primaryForm) returned.");
            }
            else
            {
                Log("No primaryForm found.");
            }
        }
    }
}
