using Windows.ApplicationModel.Background;
using Windows.Media.Playback;
using Windows.Foundation.Collections;

namespace MangoSpot
{
    public sealed class AudioTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private MediaPlayer mediaPlayer;

        public void Run(IBackgroundTaskInstance taskInstance)
        {

            deferral = taskInstance.GetDeferral();

            mediaPlayer = BackgroundMediaPlayer.Current;
            mediaPlayer.AutoPlay = false;

            taskInstance.Canceled += OnCanceled;
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {

            mediaPlayer.Pause();
            deferral.Complete();
        }
    }
}