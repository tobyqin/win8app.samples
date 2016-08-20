﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SDKTemplate;
using System;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;

namespace Transcode
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Custom : SDKTemplate.Common.LayoutAwarePage, System.IDisposable
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        MainPage rootPage = MainPage.Current;

        Windows.UI.Core.CoreDispatcher _dispatcher = Window.Current.Dispatcher;
        CancellationTokenSource _cts;
        string _OutputFileName = "TranscodeSampleOutput.mp4";
        Windows.Media.MediaProperties.MediaEncodingProfile _Profile;
        Windows.Storage.StorageFile _InputFile = null;
        Windows.Storage.StorageFile _OutputFile = null;
        Windows.Media.Transcoding.MediaTranscoder _Transcoder = new Windows.Media.Transcoding.MediaTranscoder();
        bool _UseMp4 = true;

        public Custom()
        {
            this.InitializeComponent();
            _cts = new CancellationTokenSource();

            // Hook up UI
            PickFileButton.Click += new RoutedEventHandler(PickFile);
            TargetFormat.SelectionChanged += new SelectionChangedEventHandler(OnTargetFormatChanged);
            Transcode.Click += new RoutedEventHandler(TranscodeCustom);
            Cancel.Click += new RoutedEventHandler(TranscodeCancel);  

            // Media Controls
            InputPlayButton.Click += new RoutedEventHandler(InputPlayButton_Click);
            InputPauseButton.Click += new RoutedEventHandler(InputPauseButton_Click);
            InputStopButton.Click += new RoutedEventHandler(InputStopButton_Click);
            OutputPlayButton.Click += new RoutedEventHandler(OutputPlayButton_Click);
            OutputPauseButton.Click += new RoutedEventHandler(OutputPauseButton_Click);
            OutputStopButton.Click += new RoutedEventHandler(OutputStopButton_Click);

            // Initialize UI with default settings
            MediaEncodingProfile defaultProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Wvga);
            VideoW.Text = defaultProfile.Video.Width.ToString();
            VideoH.Text = defaultProfile.Video.Height.ToString();
            VideoBR.Text = defaultProfile.Video.Bitrate.ToString();
            VideoFR.Text = defaultProfile.Video.FrameRate.Numerator.ToString();
            AudioBPS.Text = defaultProfile.Audio.BitsPerSample.ToString();
            AudioCC.Text = defaultProfile.Audio.ChannelCount.ToString();
            AudioBR.Text = defaultProfile.Audio.Bitrate.ToString();
            AudioSR.Text = defaultProfile.Audio.SampleRate.ToString();

            // File is not selected, disable all buttons but PickFileButton
            DisableButtons();
            SetPickFileButton(true);
            SetCancelButton(false);
        }

        public void Dispose()
        {
            _cts.Dispose();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        #region Custom specific
        async void TranscodeCustom(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            StopPlayers();
            DisableButtons();
            GetCustomProfile();

            // Clear messages
            StatusMessage.Text = "";

            try
            {
                if (_InputFile != null && _Profile != null)
                {
                    _OutputFile = await KnownFolders.VideosLibrary.CreateFileAsync(_OutputFileName, CreationCollisionOption.GenerateUniqueName);

                    var preparedTranscodeResult = await _Transcoder.PrepareFileTranscodeAsync(_InputFile, _OutputFile, _Profile);
                    if (preparedTranscodeResult.CanTranscode)
                    {
                        SetCancelButton(true);
                        var progress = new Progress<double>(TranscodeProgress);
                        await preparedTranscodeResult.TranscodeAsync().AsTask(_cts.Token, progress);
                        TranscodeComplete();
                    }
                    else
                    {
                        TranscodeFailure(preparedTranscodeResult.FailureReason);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                OutputText("");
                TranscodeError("Transcode Canceled");
            }
            catch (Exception exception)
            {
                TranscodeError(exception.Message);
            }
        }

        void GetCustomProfile()
        {
            if(_UseMp4)
            {
                _Profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Wvga);
            }
            else
            {
                _Profile = MediaEncodingProfile.CreateWmv(VideoEncodingQuality.Wvga);
            }

            try
            {
                _Profile.Video.Width                 = UInt32.Parse(VideoW.Text);
                _Profile.Video.Height                = UInt32.Parse(VideoH.Text);
                _Profile.Video.Bitrate               = UInt32.Parse(VideoBR.Text);
                _Profile.Video.FrameRate.Numerator   = UInt32.Parse(VideoFR.Text);
                _Profile.Video.FrameRate.Denominator = 1;
                _Profile.Audio.BitsPerSample         = UInt32.Parse(AudioBPS.Text);
                _Profile.Audio.ChannelCount          = UInt32.Parse(AudioCC.Text);
                _Profile.Audio.Bitrate               = UInt32.Parse(AudioBR.Text);
                _Profile.Audio.SampleRate            = UInt32.Parse(AudioSR.Text);
            }
            catch (Exception exception)
            {
                TranscodeError(exception.Message);
                _Profile = null;
            }
        }
        #endregion Custom specific

        void TranscodeProgress(double percent)
        {
            OutputText("Progress:  " + percent.ToString().Split('.')[0] + "%");
        }

        async void TranscodeComplete()
        {
            OutputText("Transcode completed.");
            OutputPathText("Output (" + _OutputFile.Path + ")");
            Windows.Storage.Streams.IRandomAccessStream stream = await _OutputFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                OutputVideo.SetSource(stream, _OutputFile.ContentType);
            });

            EnableButtons();
            SetCancelButton(false);
        }

        async void TranscodeCancel(object sender, RoutedEventArgs e)
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();

                if (_OutputFile != null)
                {
                    await _OutputFile.DeleteAsync();
                }
            }
            catch (Exception exception)
            {
                TranscodeError(exception.Message);
            }
        }

        async void TranscodeFailure(TranscodeFailureReason reason)
        {
            try
            {
                if (_OutputFile != null)
                {
                    await _OutputFile.DeleteAsync();
                }
            }
            catch (Exception exception)
            {
                TranscodeError(exception.Message);
            }

            switch (reason)
            {
                case TranscodeFailureReason.CodecNotFound:
                    TranscodeError("Codec not found.");
                    break;
                case TranscodeFailureReason.InvalidProfile:
                    TranscodeError("Invalid profile.");
                    break;
                default:
                    TranscodeError("Unknown failure.");
                    break;
            }
        }

        async void PickFile(object sender, RoutedEventArgs e)
        {
            var currentState = Windows.UI.ViewManagement.ApplicationView.Value;
            if (currentState == Windows.UI.ViewManagement.ApplicationViewState.Snapped && !Windows.UI.ViewManagement.ApplicationView.TryUnsnap())
            {
                TranscodeError("Cannot pick files while application is in snapped view");
            }
            else
            {
                Windows.Storage.Pickers.FileOpenPicker picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                picker.FileTypeFilter.Add(".wmv");
                picker.FileTypeFilter.Add(".mp4");

                Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    _InputFile = file;
                    InputVideo.SetSource(stream, file.ContentType);
                    InputVideo.Play();

                    // Enable buttons
                    EnableButtons();
                }
            }
        }

        void OnTargetFormatChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetFormat.SelectedIndex > 0)
            {
                _OutputFileName = "TranscodeSampleOutput.wmv";
                _UseMp4 = false;
            }
            else
            {
                _OutputFileName = "TranscodeSampleOutput.mp4";
                _UseMp4 = true;
            }
        }

        void InputPlayButton_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (InputVideo.DefaultPlaybackRate == 0)
            {
                InputVideo.DefaultPlaybackRate = 1.0;
                InputVideo.PlaybackRate = 1.0;
            }

            InputVideo.Play(); 
        }

        void InputStopButton_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            InputVideo.Stop();
        }

        void InputPauseButton_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            InputVideo.Pause();
        }

        void OutputPlayButton_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (OutputVideo.DefaultPlaybackRate == 0)
            {
                OutputVideo.DefaultPlaybackRate = 1.0;
                OutputVideo.PlaybackRate = 1.0;
            }

            OutputVideo.Play(); 
        }

        void OutputStopButton_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            OutputVideo.Stop();
        }

        void OutputPauseButton_Click(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            OutputVideo.Pause();
        }

        async void SetPickFileButton(bool isEnabled)
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                PickFileButton.IsEnabled = isEnabled;
            });
        }

        async void SetCancelButton(bool isEnabled)
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Cancel.IsEnabled = isEnabled;
            });
        }

        async void EnableButtons()
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                PickFileButton.IsEnabled = true;
                TargetFormat.IsEnabled = true;
                Transcode.IsEnabled = true;
            });
        }

        async void DisableButtons()
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Transcode.IsEnabled = false;
                PickFileButton.IsEnabled = false;
                TargetFormat.IsEnabled = false;
            });
        }

        async void StopPlayers()
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (InputVideo.CurrentState != MediaElementState.Paused)
                {
                    InputVideo.Pause();
                }
                if (OutputVideo.CurrentState != MediaElementState.Paused)
                {
                    OutputVideo.Pause();
                }
            });
        }

        async void PlayFile(Windows.Storage.StorageFile MediaFile)
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    Windows.Storage.Streams.IRandomAccessStream stream = await MediaFile.OpenAsync(FileAccessMode.Read);
                    OutputVideo.SetSource(stream, MediaFile.ContentType);
                    OutputVideo.Play();
                }
                catch (Exception exception)
                {
                    TranscodeError(exception.Message);
                }
            });
        }

        async void TranscodeError(string error)
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                StatusMessage.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                StatusMessage.Text = error;
            });

            EnableButtons();
            SetCancelButton(false);
        }

        async void OutputText(string text)
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                OutputMsg.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                OutputMsg.Text = text;
            });
        }

        async void OutputPathText(string text)
        {
            await  _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                OutputPath.Text = text;
            });
        }
    }
}
