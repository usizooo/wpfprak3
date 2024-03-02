using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AudioPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isDraggingTrackSlider;
        private DispatcherTimer? timer;
        private List<string> currentFiles;
        public MainWindow()
        {
            InitializeComponent();
            timer = null;
            isDraggingTrackSlider = false;
            currentFiles = new List<string>();
            PlayerManager.Instance.OnVolumeChanged?
                .Invoke(this, new VolumeArgs(volumeSlider.Value));
            PlayerManager.Instance.MediaPlayer.MediaOpened += MainWindow_MediaOpened;
            PlayerManager.Instance.MediaPlayer.MediaEnded += MainWindow_TrackEndedOrSwitched;
            PlayerManager.Instance.AnotherTrackWasPlayed += MainWindow_TrackEndedOrSwitched;
            PlayerManager.Instance.PlayerStateOnChanged += MainWindow_PlayerStateOnChanged;
        }

        private void MainWindow_PlayerStateOnChanged(object? sender, EventArgs e)
        {
            playPauseButton.Content = PlayerManager.Instance.State switch
            {
                PlayerState.Playing => "Пауза",
                _ => "Играть"
            };
        }

        private void MainWindow_TrackEndedOrSwitched(object? sender, EventArgs e)
        {
            timer?.Stop();
            if (timer != null)
            {
                timer.Tick -= Timer_Tick;
            }
            timer = null;
            trackSlider.Value = 0;
            elapsedTimeLabel.Content = TimeSpan.FromSeconds(0);
            remainingTimeLabel.Content = TimeSpan.FromSeconds(trackSlider.Maximum);
            trackSlider.Maximum = 1;
        }

        private void MainWindow_MediaOpened(object? sender, EventArgs e)
        {
            var trackTimeInSeconds = Convert.ToInt32(
                PlayerManager.Instance.MediaPlayer
                    .NaturalDuration
                    .TimeSpan
                    .TotalSeconds);

            // настроить слайдер
            trackSlider.Value = 0;
            trackSlider.Maximum = trackTimeInSeconds;
            // настроить таймеры
            remainingTimeLabel.Content = TimeSpan.FromSeconds(trackTimeInSeconds);
            elapsedTimeLabel.Content = TimeSpan.FromSeconds(0);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer?.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (PlayerManager.Instance.State == PlayerState.Paused)
            {
                return;
            }

            var trackTimePosition = Convert.ToInt32(
                PlayerManager.Instance.MediaPlayer
                    .Position
                    .TotalSeconds);

            // настроить слайдер
            trackSlider.Value++;
            // настроить таймеры
            remainingTimeLabel.Content
                = TimeSpan.FromSeconds(Convert
                    .ToInt32(trackSlider.Maximum - trackSlider.Value));
            elapsedTimeLabel.Content
                = TimeSpan.FromSeconds(Convert
                    .ToInt32(trackSlider.Value));
        }

        private string GetSimpleFileName(string fileName)
        {
            var trackNameWithExtension = fileName.Split('\\').Last();
            while (trackNameWithExtension[trackNameWithExtension.Length - 1] != '.')
            {
                trackNameWithExtension = trackNameWithExtension.Substring(0, trackNameWithExtension.Length - 1);
            }
            trackNameWithExtension = trackNameWithExtension.Substring(0, trackNameWithExtension.Length - 1);

            return trackNameWithExtension;
        }

        private void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            tracksListBox.Items.Clear();

            var explorer = new CommonOpenFileDialog { IsFolderPicker = true };
            var result = explorer.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                var regex = new Regex(@"(\w)*(.mp3|.wav|.flac|.aac)$");
                currentFiles = Directory.GetFiles(explorer.FileName)
                    .Where(x => regex.IsMatch(x))
                    .ToList();
                foreach (var file in currentFiles)
                {
                    tracksListBox.Items.Add(GetSimpleFileName(file));
                }
            }

            tracksListBox.SelectedIndex = 0;
        }

        private void logButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PlayerManager.Instance.IsOpenLogsWindow)
            {
                var logsWin = new LogsWindow();
                logsWin.Show();
                PlayerManager.Instance.LogsWindowWasOpened(this, e);
            }
        }

        private void prevButton_Click(object sender, RoutedEventArgs e)
        {
            if (tracksListBox.Items.Count == 0)
            {
                MessageManager.Instance.Warning("Трек не выбран!");
                return;
            }
            tracksListBox.SelectedIndex = tracksListBox.SelectedIndex == 0
                ? tracksListBox.Items.Count - 1
                : tracksListBox.SelectedIndex - 1;
            MainWindow_AnotherTrackWasPlayed();
        }

        private void playPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (tracksListBox.Items.Count == 0)
            {
                MessageManager.Instance.Warning("Трек не выбран!");
                return;
            }
            if (PlayerManager.Instance.State == PlayerState.Stopped)
            {
                MainWindow_AnotherTrackWasPlayed();
            }
            else
            {
                //playPauseButton.Content = PlayerManager.Instance.State == PlayerState.Playing
                //    ? "Пауза"
                //    : "Играть";
                PlayerManager.Instance.TrackWasPausedOrPlayed?.Invoke(this, e);
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (tracksListBox.Items.Count == 0)
            {
                MessageManager.Instance.Warning("Трек не выбран!");
                return;
            }
            tracksListBox.SelectedIndex = tracksListBox.SelectedIndex == tracksListBox.Items.Count - 1
                ? 0
                : tracksListBox.SelectedIndex + 1;
            MainWindow_AnotherTrackWasPlayed();
        }

        private void replayButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow_AnotherTrackWasPlayed();
        }

        private void randomButton_Click(object sender, RoutedEventArgs e)
        {
            if (tracksListBox.Items.Count == 0)
            {
                MessageManager.Instance.Warning("Трек не выбран!");
                return;
            }                
            int index;
            var random = new Random();
            do
            {
                index = random.Next(tracksListBox.Items.Count);
            } while (index == tracksListBox.SelectedIndex);
            tracksListBox.SelectedIndex = index;
            MainWindow_AnotherTrackWasPlayed();
        }

        private void MainWindow_AnotherTrackWasPlayed()
        {
            if (tracksListBox.SelectedItem != null && currentFiles != null)
            {
                string trackName = tracksListBox.SelectedItem.ToString()
                    ?? throw new ArgumentNullException();
                PlayerManager.Instance.AnotherTrackWasPlayed?
                    .Invoke(this, new LogsArgs(currentFiles[tracksListBox.SelectedIndex], trackName));
            }
            else
            {
                MessageManager.Instance.Warning("Трек не выбран!");
            }
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PlayerManager.Instance.OnVolumeChanged?.Invoke(this, new VolumeArgs(e.NewValue));
        }

        private void trackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((isDraggingTrackSlider || Math.Abs(e.NewValue - e.OldValue) > 1)
                && PlayerManager.Instance.State != PlayerState.Stopped)
            {
                Slider? slider = sender as Slider;
                if (slider == null)
                {
                    throw new ArgumentNullException(nameof(slider));
                }
                double newValue = Math.Round(e.NewValue);
                slider.Value = newValue;

                // настроить таймеры
                remainingTimeLabel.Content 
                    = TimeSpan.FromSeconds(Convert
                        .ToInt32(trackSlider.Maximum - trackSlider.Value));
                elapsedTimeLabel.Content 
                    = TimeSpan.FromSeconds(Convert
                        .ToInt32(trackSlider.Value));

                // обновить время трека
                PlayerManager.Instance.MediaPlayer.Position 
                    = new TimeSpan(0, 0, (int)slider.Value);
            }
        }

        private void trackSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => isDraggingTrackSlider = true;


        private void trackSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => isDraggingTrackSlider = false;

        private void tracksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => MainWindow_AnotherTrackWasPlayed();
    }
}