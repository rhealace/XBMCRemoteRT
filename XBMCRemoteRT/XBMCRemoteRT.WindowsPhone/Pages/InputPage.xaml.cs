﻿using XBMCRemoteRT.Common;
using System;
using System.Collections.Generic;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using XBMCRemoteRT.RPCWrappers;
using XBMCRemoteRT.Helpers;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Windows.Phone.Devices.Notification;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Popups;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace XBMCRemoteRT.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InputPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private bool isVolumeSetProgrammatically;
        int[] Speeds = { -32, -16, -8, -4, -2, -1, 1, 2, 4, 8, 16, 32 };

        private bool isVibrationOn;

        public InputPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            DataContext = GlobalVariables.CurrentPlayerState;
            PopulateFlyout();
            isVibrationOn = (bool)SettingsHelper.GetValue("Vibrate", false);
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GlobalVariables.CurrentTracker.SendView("InputPage");
            this.navigationHelper.OnNavigatedTo(e);
            ShowButtons();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.PortraitFlipped | DisplayOrientations.Portrait;
        }

        private void ShowButtons()
        {
            string[] buttons = ((string)SettingsHelper.GetValue("ButtonsToShow", "Home, TextInput")).Split(',');
            foreach (string button in buttons)
            {
                Button btn = this.FindName(button.Trim() + "Button") as Button;
                if (btn != null)
                    btn.Visibility = Visibility.Visible;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        #endregion

        #region Remote Keys
        private async void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            if(isVibrationOn)
                vibrate();
            await Input.ExecuteAction(InputCommands.Left);
        }

        private async void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (isVibrationOn)
                vibrate();
            await Input.ExecuteAction(InputCommands.Up);
        }

        private async void RightButton_Click(object sender, RoutedEventArgs e)
        {
            if (isVibrationOn)
                vibrate();
            await Input.ExecuteAction(InputCommands.Right);
        }

        private async void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (isVibrationOn)
                vibrate();
            await Input.ExecuteAction(InputCommands.Down);
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {            
            await Input.ExecuteAction(InputCommands.Home);
        }

        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            await Input.ExecuteAction(InputCommands.ContextMenu);
        }

        private async void OSDButton_Click(object sender, RoutedEventArgs e)
        {
            await Input.ExecuteAction(InputCommands.ShowOSD);
        }

        private async void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            await Input.ExecuteAction(InputCommands.Info);
        }

        private async void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            await Input.ExecuteAction(InputCommands.Select);
        }

        private async void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            await Input.ExecuteAction(InputCommands.Back);
        }

        #endregion

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            await Player.GoTo(GlobalVariables.CurrentPlayerState.PlayerType, GoTo.Previous);
            await PlayerHelper.RefreshPlayerState();
        }

        private async void SpeedDownButton_Click(object sender, RoutedEventArgs e)
        {
            string backwardCommand = (string)SettingsHelper.GetValue("BackwardButtonCommand", "SmallBackward");
            if (backwardCommand == "DecreaseSpeed")
            {
                int speed = GlobalVariables.CurrentPlayerState.Speed;

                if (speed != 0 && speed != -32)
                {
                    int index = Array.IndexOf(Speeds, speed);
                    int newSpeed = Speeds[index - 1];
                    await Player.SetSpeed(GlobalVariables.CurrentPlayerState.PlayerType, newSpeed);
                    await PlayerHelper.RefreshPlayerState();
                }
            }
            else
            {
                Player.Seek(GlobalVariables.CurrentPlayerState.PlayerType, backwardCommand.ToLower());
            }
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            await Player.PlayPause(GlobalVariables.CurrentPlayerState.PlayerType);
            await PlayerHelper.RefreshPlayerState();
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            await Player.Stop(GlobalVariables.CurrentPlayerState.PlayerType);
            //  await PlayerHelper.RefreshPlayerState();
        }

        private async void SpeedUpButton_Click(object sender, RoutedEventArgs e)
        {
            string forwardCommand = (string)SettingsHelper.GetValue("ForwardButtonCommand", "SmallForward");
            if (forwardCommand == "IncreaseSpeed")
            {
                int speed = GlobalVariables.CurrentPlayerState.Speed;

                if (speed != 0 && speed != 32)
                {
                    int index = Array.IndexOf(Speeds, speed);
                    int newSpeed = Speeds[index + 1];
                    await Player.SetSpeed(GlobalVariables.CurrentPlayerState.PlayerType, newSpeed);
                    await PlayerHelper.RefreshPlayerState();
                }
            }
            else
            {
                Player.Seek(GlobalVariables.CurrentPlayerState.PlayerType, forwardCommand.ToLower());
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await Player.GoTo(GlobalVariables.CurrentPlayerState.PlayerType, GoTo.Next);
            await PlayerHelper.RefreshPlayerState();
        }

        Slider slider;
        private async void VolumeSlider_Loaded(object sender, RoutedEventArgs e)
        {
            int volume = await Applikation.GetVolume();
            SetVolumeSliderValue(volume);
            slider = sender as Slider;
            slider.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(slider_PointerReleased), true);
        }

        private async void slider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            int value = (int)Math.Round(VolumeSlider.Value);
            await Applikation.SetVolume(value);
        }        

        private async void VolumeDownWrapper_Tapped(object sender, TappedRoutedEventArgs e)
        {
            int currentVolume = await Applikation.GetVolume();
            await Applikation.SetVolume(--currentVolume);
            SetVolumeSliderValue(currentVolume);
        }

        private async void VolumeUpWrapper_Tapped(object sender, TappedRoutedEventArgs e)
        {
            int currentVolume = await Applikation.GetVolume();
            await Applikation.SetVolume(++currentVolume);
            SetVolumeSliderValue(currentVolume);
        }

        private void SetVolumeSliderValue(int value)
        {
            isVolumeSetProgrammatically = true;
            VolumeSlider.Value = value;
        }

        private void SendTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                Input.SendText(SendTextBox.Text, true);
                SendTextBox.Text = string.Empty;
                LoseFocus(sender);
            }
        }

        private void LoseFocus(object sender)
        {
            var control = sender as Control;
            var isTabStop = control.IsTabStop;
            control.IsTabStop = false;
            control.IsEnabled = false;
            control.IsEnabled = true;
            control.IsTabStop = isTabStop;
        }

        private void TextInputButton_Click(object sender, RoutedEventArgs e)
        {
            SendTextBox.Visibility = Visibility.Visible;
            (this.Resources["ShowSendTextBox"] as Storyboard).Begin();
            SendTextBox.Focus(FocusState.Keyboard);
        }

        private void SendTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ((this.Resources["HideSendTextBox"]) as Storyboard).Begin();
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            AdvancedMenuFlyout.SelectedItem = null;
        }

        private string audioLibUpdate;
        private string videoLibUpdate;
        private string audioLibClean;
        private string videoLibClean;
        private string showSubtitleSerach;
        private string showVideoInfo;
        private string shutDown;
        private string suspend;

        private void PopulateFlyout()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            audioLibUpdate = loader.GetString("UpdateAudioLibrary");
            videoLibUpdate = loader.GetString("UpdateVideoLibrary");
            audioLibClean = loader.GetString("CleanAudioLibrary");
            videoLibClean = loader.GetString("CleanVideoLibrary");
            showSubtitleSerach = loader.GetString("DownloadSubtitles");
            showVideoInfo = loader.GetString("ShowCodecInfo");
            shutDown = loader.GetString("ShutDown");
            suspend = loader.GetString("Suspend");

            AdvancedMenuFlyout.ItemsSource = new List<string> { audioLibUpdate, videoLibUpdate, audioLibClean, videoLibClean, showSubtitleSerach, showVideoInfo, suspend, shutDown };
        }

        private async void AdvancedMenuFlyout_ItemsPicked(ListPickerFlyout sender, ItemsPickedEventArgs args)
        {
            string pickedCommand = (string)AdvancedMenuFlyout.SelectedItem;

            if (pickedCommand == audioLibUpdate) {
                AudioLibrary.Scan();
            } else if (pickedCommand == videoLibUpdate) {
                VideoLibrary.Scan();
            } else if (pickedCommand == audioLibClean) {
                AudioLibrary.Clean();
            } else if (pickedCommand == videoLibClean) {
                VideoLibrary.Clean();
            } else if (pickedCommand == showSubtitleSerach) {
                GUI.ShowSubtitleSearch();
            } else if (pickedCommand == showVideoInfo) {
                Input.ExecuteAction("codecinfo");
            } else if (pickedCommand == suspend) {
                await Input.ExecuteAction(SystemCommands.Suspend);
            } else if (pickedCommand == shutDown) {
                await Input.ExecuteAction(SystemCommands.Shutdown);
            }
        }

        private void vibrate()
        {
            VibrationDevice vibrationDevice = VibrationDevice.GetDefault();
            vibrationDevice.Vibrate(TimeSpan.FromMilliseconds(50));
        }

        private async void SubtitleFlyout_Closed(object sender, object e)
        {
            ListPickerFlyout f = (ListPickerFlyout)sender;

            object result = f.SelectedValue;
            if (result != null)
            {
                Player.SubtitleExtended st = (Player.SubtitleExtended)result;

                if (st.index == -1)
                    await Player.DisableSubtitle(GlobalVariables.CurrentPlayerState.PlayerType);
                else
                    await Player.SetAndEnableSubtitle(GlobalVariables.CurrentPlayerState.PlayerType, st.index);
            }
        }

        private async void SubtitlesButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AppBarButton btn = sender as AppBarButton;
            FlyoutBase f = btn.Flyout;

            List<Player.SubtitleExtended> subtitles = await PlayerHelper.GetSubtitles();

            if (subtitles == null)
            {
                e.Handled = true;
                MessageDialog md = new MessageDialog("No subtitles available", "Change subtitle");
                await md.ShowAsync();
            }
            else
            {
                Player.SubtitleExtended disabledst = new Player.SubtitleExtended();
                disabledst.index = -1;
                disabledst.name = "Disabled";
                disabledst.isInUse = !subtitles.Exists((s) => s.isInUse == true);

                subtitles.Add(disabledst);

                SubtitlesFlyout.ItemsSource = subtitles;
            }
        }

        private async void AudioStreamsFlyout_Closed(object sender, object e)
        {
            ListPickerFlyout f = (ListPickerFlyout)sender;

            object result = f.SelectedValue;
            if (result != null)
            {
                Player.AudioStreamExtended ase = (Player.AudioStreamExtended)result;

                await Player.SetAudioStream(GlobalVariables.CurrentPlayerState.PlayerType, ase.index);
            }
        }

        private async void AudioStreamsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AppBarButton btn = sender as AppBarButton;
            FlyoutBase f = btn.Flyout;

            List<Player.AudioStreamExtended> audiostreams = await PlayerHelper.GetAudioStreams();

            if (audiostreams == null)
            {
                e.Handled = true;
                MessageDialog md = new MessageDialog("No audio streams available", "Change audio stream");
                await md.ShowAsync();
            }
            else
                AudioStreamsFlyout.ItemsSource = audiostreams;
        }

        private async void CycleRepeatButton_Click(object sender, RoutedEventArgs e) {
            string nextRepeat = "off";
            switch (GlobalVariables.CurrentPlayerState.Repeat) {
                case "off":
                    nextRepeat = "one";
                    break;
                case "one":
                    nextRepeat = "all";
                break;
            }
            await Player.SetRepeat(GlobalVariables.CurrentPlayerState.PlayerType, nextRepeat);
            await PlayerHelper.RefreshPlayerState();
        }

        private async void EjectOpticalDisk_Click(object sender, RoutedEventArgs e) {
            await Input.ExecuteAction(SystemCommands.EjectOpticalDrive);
        }
    }
}
