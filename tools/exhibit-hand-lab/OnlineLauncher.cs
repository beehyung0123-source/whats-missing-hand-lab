using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class OnlineLauncher
{
    private const string ExhibitUrl = "https://beehyung0123-source.github.io/whats-missing-hand-lab/arrangement/";

    [STAThread]
    private static int Main()
    {
        try
        {
            string folder = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).ToLowerInvariant();
            string id;
            using (var hash = SHA256.Create())
                id = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(
                    Environment.UserDomainName + "\\" + Environment.UserName + "\n" + folder))).Replace("-", "").Substring(0, 24);
            string mutexName = "Local\\WhatsMissingHandLabOnline_" + id;
            string stopName = mutexName + "_Stop";
#if STOP_EXHIBIT
            try
            {
                using (var stop = EventWaitHandle.OpenExisting(stopName)) stop.Set();
                using (var running = Mutex.OpenExisting(mutexName))
                {
                    bool acquired = false;
                    try
                    {
                        try { acquired = running.WaitOne(15000); }
                        catch (AbandonedMutexException) { acquired = true; }
                        if (!acquired) throw new InvalidOperationException("전시 창이 아직 종료되지 않았습니다. 전시용 Chrome 창을 닫아 주세요.");
                    }
                    finally { if (acquired) running.ReleaseMutex(); }
                }
            }
            catch (WaitHandleCannotBeOpenedException) { /* Already stopped. */ }
#else
            bool created;
            using (var running = new Mutex(true, mutexName, out created))
            {
                if (!created) return 0;
                try
                {
                    using (var stop = new EventWaitHandle(false, EventResetMode.AutoReset, stopName))
                    {
                        string chrome = FindChrome();
                        if (chrome == null)
                            throw new FileNotFoundException("Google Chrome이 필요합니다. 이 노트북에 Chrome을 설치한 뒤 다시 실행해 주세요.");
                        string profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "WhatsMissingHandLabOnline", id);
                        var options = new ProcessStartInfo
                        {
                            FileName = chrome,
                            Arguments = "--user-data-dir=\"" + profile + "\" --no-first-run --no-default-browser-check " +
                                "--disable-background-mode --autoplay-policy=no-user-gesture-required --start-maximized --new-window " + ExhibitUrl,
                            UseShellExecute = false
                        };
                        using (var browser = Process.Start(options))
                        {
                            while (!browser.HasExited)
                            {
                                if (stop.WaitOne(250))
                                {
                                    CloseExhibit(browser);
                                    break;
                                }
                            }
                        }
                    }
                }
                finally { running.ReleaseMutex(); }
            }
#endif
            return 0;
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "What’s Missing? 개선 버전 전시 운영", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string FindChrome()
    {
        foreach (string root in new[] {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) })
        {
            if (String.IsNullOrEmpty(root)) continue;
            string candidate = Path.Combine(root, "Google", "Chrome", "Application", "chrome.exe");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static void CloseExhibit(Process browser)
    {
        if (browser.HasExited) return;
        browser.CloseMainWindow();
        if (browser.WaitForExit(3000)) return;
        // Terminate only the isolated Chrome process started by this launcher.
        using (var closer = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe"),
            Arguments = "/PID " + browser.Id + " /T /F",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        })) closer.WaitForExit(5000);
        if (!browser.WaitForExit(3000))
            throw new InvalidOperationException("전시용 Chrome 창을 닫지 못했습니다. 해당 창을 직접 닫아 주세요.");
    }
}
