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
            Console.WriteLine("Table Pet is running.");
            Console.WriteLine("Look at the CENTER of the desktop, or the tray icon near the clock.");
            Console.WriteLine("Right-click the tray icon and choose Exit to quit.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            MessageBox.Show(ex.ToString(), "Table Pet failed to start");
            Shutdown(-1);
        }
    }

    private TaskbarIcon CreateTray()
    {
        var tray = new TaskbarIcon
        {
            ToolTipText = "Table Pet",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Tray.ico")),
            ContextMenu = new ContextMenu()
        };

        tray.ContextMenu.Items.Add(MakeItem("Show pet", (_, _) =>
        {
            _petWindow.Show();
            _petWindow.Activate();
        }));
        tray.ContextMenu.Items.Add(MakeItem("Hide pet", (_, _) => _petWindow.Hide()));
        tray.ContextMenu.Items.Add(new Separator());
        tray.ContextMenu.Items.Add(MakeItem("Sit", (_, _) => _pet.RequestSit()));
        tray.ContextMenu.Items.Add(MakeItem("Lie down", (_, _) => _pet.RequestLie()));
        tray.ContextMenu.Items.Add(MakeItem("Walk", (_, _) =>
        {
            _petWindow.Show();
            var work = ScreenBounds.GetWorkArea(_petWindow);
            _pet.RequestWalk(_petWindow.Left, work.Left, work.Right - _petWindow.ActualWidth);
        }));
        tray.ContextMenu.Items.Add(new Separator());
        tray.ContextMenu.Items.Add(MakeItem("Settings...", (_, _) => _petWindow.OpenSettings()));
        tray.ContextMenu.Items.Add(new Separator());
        tray.ContextMenu.Items.Add(MakeItem("Exit", (_, _) => Shutdown()));
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
        _tray.ShowBalloonTip("Table Pet", ReminderConfig.Message, BalloonIcon.Info);
    }

    private void OnUiCrash(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Console.Error.WriteLine(e.Exception);
        MessageBox.Show(e.Exception.ToString(), "Table Pet error");
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
