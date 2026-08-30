using System.Windows;
using TablePet.Config;
using TablePet.Persistence;

namespace TablePet.Ui;

public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settingsStore;

    public SettingsWindow(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        InitializeComponent();

        IntervalSlider.Minimum = ReminderConfig.MinIntervalMinutes;
        IntervalSlider.Maximum = ReminderConfig.MaxIntervalMinutes;

        var settings = _settingsStore.Current;
        ReminderEnabled.IsChecked = settings.ReminderEnabled;
        IntervalSlider.Value = settings.ClampIntervalMinutes();
        ClickThrough.IsChecked = settings.ClickThrough;
        IntervalLabel.Text = ((int)IntervalSlider.Value).ToString();
    }

    private void IntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntervalLabel is not null)
        {
            IntervalLabel.Text = ((int)e.NewValue).ToString();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsStore.Current;
        settings.ReminderEnabled = ReminderEnabled.IsChecked == true;
        settings.IntervalMinutes = (int)IntervalSlider.Value;
        settings.ClickThrough = ClickThrough.IsChecked == true;
        _settingsStore.Save();
        DialogResult = true;
        Close();
    }
}
