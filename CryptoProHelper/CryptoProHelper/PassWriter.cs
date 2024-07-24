using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoProHelper
{
    partial class Form1
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_SETTEXT = 0x000C;
        private const uint BM_CLICK = 0x00F5;
        private const int delay = 500;
        string pass = "";

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private Thread workerThread;
        private bool stopRequested;

        void startPassDemon(string pswd)
        {
            pass = pswd;
            stopRequested = false;
            workerThread = new Thread(new ThreadStart(WorkerThread));
            workerThread.Start();
        }

        void stopPassDemon()
        {
            stopRequested = true;
            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join();
            }
        }
        private void WorkerThread()
        {
            try
            {
                while (!stopRequested)
                {
                    EnumWindows((hWnd, lParam) =>
                    {
                        if (IsWindowVisible(hWnd))
                        {
                            StringBuilder windowText = new StringBuilder(256);
                            GetWindowText(hWnd, windowText, windowText.Capacity);
                            if (windowText.ToString().Contains("КриптоПро CSP"))
                            {
                                IntPtr passwordStatic = FindWindowEx(hWnd, IntPtr.Zero, null, "Пароль:");
                                if (passwordStatic != IntPtr.Zero)
                                {
                                    IntPtr passwordEdit = FindWindowEx(hWnd, IntPtr.Zero, "Edit", null);
                                    IntPtr button1 = FindWindowEx(hWnd, IntPtr.Zero, "Button", null);
                                    IntPtr button2 = FindWindowEx(hWnd, button1, "Button", null);

                                    SendMessage(passwordEdit, WM_SETTEXT, IntPtr.Zero, pass); // Вписываем пароль
                                    PostMessage(button1, BM_CLICK, IntPtr.Zero, IntPtr.Zero); // Ставим галочку
                                    PostMessage(button2, BM_CLICK, IntPtr.Zero, IntPtr.Zero); // Жмём ОК
                                }
                            }
                        }
                        return true;
                    }, IntPtr.Zero);

                    Thread.Sleep(delay);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка: " + ex.Message);
            }
        }
    }
}
