// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

using DiagnosticsHelper;
using SDKTemplateCS;
using StreamSocketTransportHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ControlChannelTrigger
{
    public sealed partial class ScenarioInput1 : Page, IDisposable
    {
    
        // A pointer back to the main page which is used to gain access to the input and output frames and their content.
        MainPage rootPage = null;
        CoreDispatcher coreDispatcher;
        CommModule commModule;
        bool lockScreenAdded = false;
        Page outputFrame;
        TextBlock debugOutputTextBlock;
        public ScenarioInput1()
        {
            InitializeComponent();
            coreDispatcher = Diag.coreDispatcher = Window.Current.Dispatcher;
            ConnectButton.Visibility = Visibility.Collapsed;
        }

        public void Dispose()
        {
            if (commModule != null)
            {
                commModule.Dispose();
                commModule = null;
            }
        }

        #region Template-Related Code - Do not remove
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get a pointer to our main page
            rootPage = e.Parameter as MainPage;

            // We want to be notified with the OutputFrame is loaded so we can get to the content.
            rootPage.OutputFrameLoaded += new System.EventHandler(rootPage_OutputFrameLoaded);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            rootPage.OutputFrameLoaded -= new System.EventHandler(rootPage_OutputFrameLoaded);
        }

        #endregion

        #region Use this code if you need access to elements in the output frame - otherwise delete
        void rootPage_OutputFrameLoaded(object sender, object e)
        {
            // At this point, we know that the Output Frame has been loaded and we can go ahead
            // and reference elements in the page contained in the Output Frame.

            // Get a pointer to the content within the OutputFrame.
            outputFrame = (Page)rootPage.OutputFrame.Content;

            // Go find the elements that we need for this scenario.
            debugOutputTextBlock = Diag.debugOutputTextBlock = outputFrame.FindName("DebugTextBlock") as TextBlock;
        }

        #endregion

        #region Click Handlers
        private enum appRoles
        {
            clientRole,
            serverRole
        };
        appRoles appRole;

        private void ServerRole_Click(object sender, RoutedEventArgs e)
        {
            Diag.DebugPrint("Server role selected.");

            // In order to simplify the sample and focus on the core controlchanneltrigger
            // related concepts, once a role is selected, the app has
            // to be restarted to change the role.
            ClientRoleButton.IsChecked = false;
            ClientRoleButton.IsEnabled = false;
            ClientSettings.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ConnectButton.Visibility = Visibility.Collapsed;
            ServerRoleButton.IsEnabled = false;
            ServerSettings.Visibility = Windows.UI.Xaml.Visibility.Visible;
            appRole = appRoles.serverRole;
            ServerInit();
        }

        private void ClientRole_Click(object sender, RoutedEventArgs e)
        {
            Diag.DebugPrint("Client role selected");

            // In order to simplify the sample and focus on the core controlchanneltrigger
            // related concepts, once a role is selected, the app has
            // to be restarted to change the role.
            ServerRoleButton.IsChecked = false;
            ServerRoleButton.IsEnabled = false;
            ServerSettings.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ClientRoleButton.IsEnabled = false;
            appRole = appRoles.clientRole;
            ClientInit();
        }
        #endregion
        async void ClientInit()
        {
            commModule = new CommModule(AppRole.ClientRole);

            // In the client role, we require the application to be on lock screen.
            // Lock screen is required to let in-process RealTimeCommunication related
            // background code to execute.
            if (lockScreenAdded == false)
            {
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                Diag.DebugPrint("Lock screen status" + status);

                switch (status)
                {
                    case BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity:

                        // App is allowed to use RealTimeConnection broker 
                        // functionality even in low power mode.
                        lockScreenAdded = true;
                        break;
                    case BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity:

                        // App is allowed to use RealTimeConnection broker 
                        // functionality but not in low power mode.
                        lockScreenAdded = true;
                        break;
                    case BackgroundAccessStatus.Denied:

                        // App should switch to polling mode (example: poll for email based on time triggers)
                        Diag.DebugPrint("As Lockscreen status was Denied, App should switch to polling mode such as email based on time triggers.");
                        break;
                }
            }

            // Now, enable the client settings if the role hasn't changed.
            if (appRole == appRoles.clientRole)
            {
                ClientSettings.Visibility = Visibility.Visible;
            }
            ConnectButton.Visibility = Visibility.Visible;
            return;
        }

        void ServerInit()
        {
            commModule = new CommModule(AppRole.ServerRole);
        }

        private string GetServerName()
        {
            return ServerName.Text;
        }

        private string GetServerPort()
        {
            return ServerPort.Text;
        }

        private enum connectionStates
        {
            notConnected = 0,
            connecting = 1,
            connected = 2,
        };

        private static connectionStates connectionState = connectionStates.notConnected;

        // Registers a background task with an network change system trigger.
        private void RegisterNetworkChangeTask()
        {
            try
            {
                // Delete previously registered network status change tasks as
                // the background triggers are persistent by nature across process
                // lifetimes.
                foreach (var cur in BackgroundTaskRegistration.AllTasks)
                {
                    Diag.DebugPrint("Deleting Background Taks " + cur.Value.Name);
                    cur.Value.Unregister(true);
                }
                var myTaskBuilder = new BackgroundTaskBuilder();
                var myTrigger = new SystemTrigger(SystemTriggerType.NetworkStateChange, false);
                myTaskBuilder.SetTrigger(myTrigger);
                myTaskBuilder.TaskEntryPoint = "Background.NetworkChangeTask";
                myTaskBuilder.Name = "Network change task";
                var myTask = myTaskBuilder.Register();
            }
            catch (Exception exp)
            {
                Diag.DebugPrint("Exception caught while setting up system event" + exp.ToString());
            }
        }

        Task ResetTransportTaskAsync()
        {
            return
                Task.Factory.StartNew(() =>
                {
                    if (commModule != null)
                    {
                        commModule.Reset();
                    }
                });
        }

        async private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (connectionState == connectionStates.notConnected)
            {
                ConnectButton.Content = "Connecting...";
                connectionState = connectionStates.connecting;
                string serverName = GetServerName();
                string serverPort = GetServerPort();

                // Ask to be notified when network status changes.
                RegisterNetworkChangeTask();

                // Finally, initiate the connection and set up transport
                // to be RTC capable. But do this heavy lifting outside of the UI thread.
                bool result = await Task<bool>.Factory.StartNew(() =>
                {
                    return commModule.SetupTransport(serverName, serverPort);
                });

                Diag.DebugPrint("CommModule setup result: " + result);
                if (result == true)
                {
                    ConnectButton.Content = "Disconnect";
                    connectionState = connectionStates.connected;
                }
                else
                {
                    ConnectButton.Content = "failed to connect. click to retry";
                    connectionState = connectionStates.notConnected;
                }
            }
            else if (connectionState == connectionStates.connected)
            {
                await ResetTransportTaskAsync();
                connectionState = connectionStates.notConnected;
                ConnectButton.Content = "Connect";
            }
        }

        private enum listeningStates
        {
            notListening = 0,
            listening = 1,
        };

        private static listeningStates listenState = listeningStates.notListening;
        private async void ListenButton_Click(object sender, RoutedEventArgs e)
        {
            if (listenState == listeningStates.notListening)
            {
                string serverPort = GetServerPort();

                bool result = await Task<bool>.Factory.StartNew(() =>
                {
                    return commModule.SetupTransport(null, serverPort);

                });
                Diag.DebugPrint("CommModule setup result: " + result);
                if (result == true)
                {
                    ListenButton.Content = "Stop Listening";
                    listenState = listeningStates.listening;
                }
                else
                {
                    ListenButton.Content = "failed to listen. click to retry";
                    listenState = listeningStates.notListening;
                }
            }
            else
            {
                await ResetTransportTaskAsync();
                listenState = listeningStates.notListening;
                ListenButton.Content = "Listen";
            }
        }

        // Try sending the message to the client.
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Text != "" && commModule != null)
            {
                string message = MessageBox.Text;
                await Task.Factory.StartNew(() =>
                {
                    commModule.SendMessage(message);
                });
            }
        }
    }
}
