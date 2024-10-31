using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace MangoSpot
{

    public sealed partial class App : Application
    {
        private TransitionCollection transitions;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.ContentTransitions = null; 
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                if (string.IsNullOrEmpty(Settings.AccessToken))
                {
      
                    if (!rootFrame.Navigate(typeof(LoginPage), e.Arguments))
                    {
                        throw new Exception("Failed to create LoginPage");
                    }
                }
                else
                {
                    // Navigate to MainPage if logged in
                    if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                    {
                        throw new Exception("Failed to create MainPage");
                    }
                }
            }

            Window.Current.Activate();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            var fileArgs = args as FileOpenPickerContinuationEventArgs;
            if (fileArgs != null && fileArgs.Files.Count > 0)
            {
                var file = fileArgs.Files[0];
                var rootFrame = Window.Current.Content as Frame;
                var mainPage = rootFrame?.Content as LoginPage;

                if (mainPage != null)
                {
                    mainPage.ImportFile(file);
                }
            }
        }


        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
    }
}