// SPDX-FileCopyrightText: © 2019-2023 YorVeX, https://github.com/YorVeX
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace xComboText
{
  public sealed class NotificationIcon
  {
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenu notificationMenu;
    readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
    readonly Settings _settings = new Settings();
    readonly object _fileWriteLock = new object();

    #region Initialize icon and menu
    public NotificationIcon()
    {
      _notifyIcon = new NotifyIcon();
      notificationMenu = new ContextMenu(InitializeMenu());

      _notifyIcon.DoubleClick += IconDoubleClick;
      _notifyIcon.Icon = (Icon)new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon)).GetObject("$this.Icon");
      _notifyIcon.ContextMenu = notificationMenu;

      _settings.LoadFromFile(Application.ProductName + ".conf");

      foreach (var fileCombo in _settings.FileCombos.FileComboList)
      {
        combineFileTexts(fileCombo);
        foreach (var inputFile in fileCombo.InputFiles)
        {
          try
          {
            FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(inputFile.InputFile), Path.GetFileName(inputFile.InputFile));
            watcher.Changed += (sender, e) => combineFileTexts(fileCombo);
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
          }
          catch (Exception ex)
          {
            MessageBox.Show(ex.GetType().Name + " creating file watcher for " + inputFile.InputFile + ":\n" + ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
        }
      }
    }

    private MenuItem[] InitializeMenu()
    {
      MenuItem[] menu = new MenuItem[] {
        new MenuItem("About", menuAboutClick),
        new MenuItem("Exit", menuExitClick)
      };
      return menu;
    }
    #endregion

    #region Main - Program entry point
    /// <summary>Program entry point.</summary>
    [STAThread]
    public static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      // Please use a unique name for the mutex to prevent conflicts with other programs
      using (Mutex mtx = new Mutex(true, "xComboText", out bool isFirstInstance))
      {
        if (isFirstInstance)
        {
          NotificationIcon notificationIcon = new NotificationIcon();
          notificationIcon._notifyIcon.Visible = true;
          Application.Run();
          notificationIcon._notifyIcon.Dispose();
        }
        else
        {
          // The application is already running
          // TODO: Display message box or change focus to existing application instance
        }
      } // releases the Mutex
    }
    #endregion

    #region private methods
    private void showBalloonInvoke(int timeout, string tipTitle, string tipText, ToolTipIcon tipIcon)
    {
      // ShowBalloonTip() runs on the GUI thread so it needs to be invoked if called from a different thread
      System.Reflection.MethodInfo methodInfo = typeof(NotifyIcon).GetMethod("ShowBalloonTip", new[] { typeof(int), typeof(string), typeof(string), typeof(ToolTipIcon) });
      methodInfo.Invoke(_notifyIcon, new object[] { timeout, tipText, tipTitle, tipIcon });
    }

    private void combineFileTexts(FileCombo fileCombo)
    {
      Debug.WriteLine(string.Format("[{0:T}] Writing combined file.", DateTime.Now));
      lock (_fileWriteLock)
      {
        string comboString = "";
        foreach (var inputFile in fileCombo.InputFiles)
        {
          string fileContent = "";
          try
          {
            if (File.Exists(inputFile.InputFile))
              fileContent = File.ReadAllText(inputFile.InputFile);
          }
          catch (IOException)
          {
            // retry a bit later
            (new Thread(() =>
            {
              Thread.Sleep(3000);
              Debug.WriteLine("File read retry after IOException.");
              combineFileTexts(fileCombo);
            })).Start();
            return;
          }
          catch (Exception ex)
          {
            showBalloonInvoke(5000, Application.ProductName, ex.GetType().Name + " trying to read " + inputFile.InputFile, ToolTipIcon.Error);
          }

          if (string.IsNullOrEmpty(fileContent))
          {
            if (inputFile.SkipIfEmpty)
              continue;
            fileContent = inputFile.EmptyText;
          }
          if (!string.IsNullOrEmpty(inputFile.ReplaceSearch) && !string.IsNullOrEmpty(inputFile.ReplaceWith))
            fileContent = fileContent.Replace(inputFile.ReplaceSearch, inputFile.ReplaceWith);
          comboString += (comboString == "" ? "" : fileCombo.Separator) + inputFile.Prefix + fileContent + inputFile.Suffix;
        }
        try
        {
          if (string.IsNullOrEmpty(comboString))
            File.WriteAllText(fileCombo.OutputFile, fileCombo.EmptyText);
          else
            File.WriteAllText(fileCombo.OutputFile, fileCombo.Prefix + comboString + fileCombo.Suffix);
        }
        catch (Exception ex)
        {
          showBalloonInvoke(5000, Application.ProductName, ex.GetType().Name + " trying to write " + fileCombo.OutputFile, ToolTipIcon.Error);
        }
      }
    }

    #endregion private methods

    #region Event Handlers
    private void menuAboutClick(object sender, EventArgs e)
    {
      MessageBox.Show(Application.ProductName + " v" + Application.ProductVersion);
    }

    private void menuExitClick(object sender, EventArgs e)
    {
      foreach (FileSystemWatcher watcher in _watchers)
        watcher.Dispose();
      Application.Exit();
    }

    private void IconDoubleClick(object sender, EventArgs e)
    {
      //			MessageBox.Show("The icon was double clicked");

      //			var list = new List<FileComboInputFile>();
      //			list.Add(new FileComboInputFile("input1.txt", "Input 1:", ""));
      //			list.Add(new FileComboInputFile("input2.txt", "Input 2:", ""));
      //			_settings.FileCombos.FileComboList.Add(new FileCombo("output.txt", ", ", list));
      //			_settings.SaveToFile("Example.conf");
    }
    #endregion
  }
}
