using System.Windows;

namespace AudioPlayer
{
    /// <summary>
    /// Логика взаимодействия для LogsWindow.xaml
    /// </summary>
    public partial class LogsWindow : Window
    {
        public LogsWindow()
        {
            InitializeComponent();

            AddLogsToListBox();

            PlayerManager.Instance.AnotherTrackWasPlayed += LogsWindow_AnotherTrackWasPlayed;
        }

        private void LogsWindow_AnotherTrackWasPlayed(object? sender, LogsArgs e)
            => AddLogsToListBox();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PlayerManager.Instance.LogsWindowWasClosed?.Invoke(this, e);
        }

        private void AddLogsToListBox()
        {
            logsListBox.Items.Clear();
            foreach (var log in PlayerManager.Instance.Logs)
            {
                logsListBox.Items.Add(log);
            }
        }
    }
}
