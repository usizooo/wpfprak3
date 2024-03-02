using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Media;

namespace AudioPlayer
{
    public class LogsArgs : EventArgs
    {
        public readonly string FileName;
        public readonly string TrackName;
        public LogsArgs(string fileName, string trackName)
        {
            FileName = fileName;
            TrackName = trackName;
        }
    }
    public class VolumeArgs : EventArgs
    {
        public readonly double Volume;

        public VolumeArgs(double volume) => Volume = volume;
    }

    public enum PlayerState
    {
        Playing,
        Paused,
        Stopped
    }

    public class PlayerManager
    {
        public PlayerState State { get; private set; }
        public bool IsOpenLogsWindow { get; private set; }
        public List<string> Logs { get; private set; }
        public MediaPlayer MediaPlayer { get; private set; }

        public EventHandler LogsWindowWasClosed;
        public EventHandler LogsWindowWasOpened;
        public EventHandler TrackWasPausedOrPlayed;
        public EventHandler? PlayerStateOnChanged;
        public EventHandler<LogsArgs> AnotherTrackWasPlayed;
        public EventHandler<VolumeArgs> OnVolumeChanged;

        private static PlayerManager? instance;
        public static PlayerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PlayerManager();
                }
                return instance;
            }
        }

        private PlayerManager()
        {
            State = PlayerState.Stopped;
            IsOpenLogsWindow = false;
            Logs = new List<string>();
            MediaPlayer = new MediaPlayer();
            LogsWindowWasClosed += PlayerManager_OnLogsWindowClosed;
            LogsWindowWasOpened += PlayerManager_OnLogsWindowOpened;
            AnotherTrackWasPlayed += PlayerManager_AnotherTrackWasPlayed;
            OnVolumeChanged += PlayerManager_OnVolumeChanged;
            TrackWasPausedOrPlayed += PlayerManager_TrackWasPausedOrPlayed;
            MediaPlayer.MediaEnded += PlayerManager_MediaEnded;
        }

        private void PlayerManager_MediaEnded(object? sender, EventArgs e)
        { 
            State = PlayerState.Stopped;
            PlayerStateOnChanged?.Invoke(this, e);
        }
        
        private void PlayerManager_TrackWasPausedOrPlayed(object? sender, EventArgs e)
        {
            switch (State)
            {
                case PlayerState.Stopped:
                    MessageManager.Instance.Warning("Трек не выбран!");
                    break;
                case PlayerState.Playing:
                    MediaPlayer.Pause();
                    State = PlayerState.Paused;
                    break;
                case PlayerState.Paused:
                    MediaPlayer.Play();
                    State = PlayerState.Playing;
                    break;
            }
            PlayerStateOnChanged?.Invoke(this, e);
        }

        private void PlayerManager_OnVolumeChanged(object? sender, VolumeArgs e)
        {
            if (e.Volume < 0 || e.Volume > 1)
            {
                throw new ArgumentException(nameof(e.Volume));
            }
            MediaPlayer.Volume = e.Volume;
        }

        private void PlayerManager_AnotherTrackWasPlayed(object? sender, LogsArgs e)
        {
            MediaPlayer.Close();
            MediaPlayer.Open(new Uri(e.FileName));
            MediaPlayer.Play();
            State = PlayerState.Playing;
            PlayerStateOnChanged?.Invoke(this, e);
            Logs.Add($"{e.TrackName, -30} \t {DateTime.Now}");
        }

        private void PlayerManager_OnLogsWindowOpened(object? sender, EventArgs e) 
            => IsOpenLogsWindow = true;

        private void PlayerManager_OnLogsWindowClosed(object? sender, EventArgs e) 
            => IsOpenLogsWindow = false;
    }
}
