using Windows.ApplicationModel.Background;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Media;

namespace MangoSpot
{
    public sealed class AudioBackgroundTask : IBackgroundTask
    {
        private SystemMediaTransportControls _systemControls;
        private BackgroundTaskDeferral _deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {

            _deferral = taskInstance.GetDeferral();

            _systemControls = SystemMediaTransportControls.GetForCurrentView();
            _systemControls.IsEnabled = true;
            _systemControls.ButtonPressed += SystemControls_ButtonPressed;

            taskInstance.Canceled += OnCanceled;

            BackgroundMediaPlayer.Current.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
        }

        private void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {

            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    BackgroundMediaPlayer.Current.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    BackgroundMediaPlayer.Current.Pause();
                    break;
            }
        }

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {

            _systemControls.PlaybackStatus = sender.CurrentState == MediaPlayerState.Playing
                ? MediaPlaybackStatus.Playing
                : MediaPlaybackStatus.Paused;
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {

            _systemControls.ButtonPressed -= SystemControls_ButtonPressed;
            BackgroundMediaPlayer.Current.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;
            _deferral.Complete();
        }
    }
}