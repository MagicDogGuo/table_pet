using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using TablePet.Config;
using TablePet.Persistence;
using TablePet.Pet;
using TablePet.Reminder;
using TablePet.Shell;

namespace TablePet;

public partial class App : Application
{
    private SettingsStore _settingsStore = null!;
    private ReminderService _reminder = null!;
    private PetController _pet = null!;
    private PetWindow _petWindow = null!;
    private TaskbarIcon _tray = null!;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnUiCrash;
        AppDomain.CurrentDomain.UnhandledException += OnDomainCrash;

        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                WindowConfig.AppFolderName);
            _settingsStore = new SettingsStore(directory);
            _settingsStore.Load();
            _pet = new PetController();
            _reminder = new ReminderService(new SystemClock(), _settingsStore);
            _petWindow = new PetWindow(_pet, _reminder, _settingsStore);
            MainWindow = _petWindow;
            _tray = CreateTray();
            _reminder.ReminderDue += OnReminderDue;
            _petWindow.Show();
            _petWindow.Activate();
            Console.WriteLine("桌寵正在執行。");
            Console.WriteLine("請看桌面中央，或是時鐘旁的系統匣圖示。");
            Console.WriteLine("在系統匣圖示按右鍵，選「結束」即可關閉。");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            MessageBox.Show(ex.ToString(), "桌寵啟動失敗");
            Shutdown(-1);
        }
    }

    private TaskbarIcon CreateTray()
    {
        var tray = new TaskbarIcon
        {
            ToolTipText = "桌寵",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Tray.ico")),
            ContextMenu = new ContextMenu()
        };

        tray.ContextMenu.Items.Add(MakeItem("顯示寵物", (_, _) =>
        {
            _petWindow.Show();
            _petWindow.Activate();
        }));
        tray.ContextMenu.Items.Add(MakeItem("隱藏寵物", (_, _) => _petWindow.Hide()));
        tray.ContextMenu.Items.Add(new Separator());
        tray.ContextMenu.Items.Add(MakeItem("坐下", (_, _) => _pet.RequestSit()));
        tray.ContextMenu.Items.Add(MakeItem("趴下", (_, _) => _pet.RequestLie()));
        tray.ContextMenu.Items.Add(MakeItem("走路", (_, _) =>
        {
            _petWindow.Show();
            var work = ScreenBounds.GetWorkArea(_petWindow);
            _pet.RequestWalk(_petWindow.Left, work.Left, work.Right - _petWindow.ActualWidth);
        }));
        tray.ContextMenu.Items.Add(new Separator());
        tray.ContextMenu.Items.Add(MakeItem("設定...", (_, _) => _petWindow.OpenSettings()));
        tray.ContextMenu.Items.Add(new Separator());
        tray.ContextMenu.Items.Add(MakeItem("結束", (_, _) => Shutdown()));
        tray.TrayMouseDoubleClick += (_, _) =>
        {
            _petWindow.Show();
            _petWindow.Activate();
        };
        return tray;
    }

    private static MenuItem MakeItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    private void OnReminderDue()
    {
        _tray.ShowBalloonTip("桌寵", ReminderConfig.Message, BalloonIcon.Info);
    }

    private void OnUiCrash(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Console.Error.WriteLine(e.Exception);
        MessageBox.Show(e.Exception.ToString(), "桌寵發生錯誤");
        e.Handled = true;
    }

    private static void OnDomainCrash(object sender, UnhandledExceptionEventArgs e)
    {
        Console.Error.WriteLine(e.ExceptionObject);
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        if (_reminder is not null)
        {
            _reminder.ReminderDue -= OnReminderDue;
        }

        _tray?.Dispose();
    }
}
