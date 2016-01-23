/*
    Copyright (C) 2016  Eduardo Elías Noyer Silva

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Phone.Notification.Management;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using P3bble.Core.Types;
using Windows.Storage;
using WuffNotificationWatcher;
using PebbleWuff_10.Models;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using Windows.Graphics.Imaging;
using Windows.Networking.Proximity;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Newtonsoft.Json.Linq;
// La plantilla de elemento Página en blanco está documentada en http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PebbleWuff_10
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        CancellationTokenSource backgroundToken;

        public List<AppItem> ItemList { get; set; }
        public List<PebbleAppItem> PebbleAppList { get; set; }

        public MainPage()
        {
            LittleWatson.CheckForPreviousException();
            this.InitializeComponent();
            ApplicationData.Current.LocalSettings.Values["Name"] = null;
            ApplicationData.Current.LocalSettings.Values["Version"] = null;

            Application.Current.Suspending += Current_Suspending;
            Application.Current.Resuming += Current_Resuming;
            Application.Current.UnhandledException += Current_UnhandledException;
        }

        private async void Current_Resuming(object sender, object e)
        {
            bool? firstTime = ApplicationData.Current.LocalSettings.Values["firstTime"] as bool?;
            ApplicationData.Current.LocalSettings.Values["Name"] = null;
            ApplicationData.Current.LocalSettings.Values["Version"] = null;
            if (firstTime == null || firstTime == true)
            {
                //Usuario debe presionar el boton Conectar
            }
            else
            {
                backgroundToken.Cancel(false);
                PebbleName.Text = "Connecting...";
                await initPebbleApp(false); //No es necesario registrar en cada momento la app
                Application.Current.Suspending += Current_Suspending;
            }
        }

        private void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LittleWatson.ReportException(e.Exception, "UnhandledException ");
            Debug.WriteLine(e.Message);
            e.Handled = true;
        }


        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            try
            {
                Debug.WriteLine("Suspending!!!!");
                bool? firstTime = ApplicationData.Current.LocalSettings.Values["firstTime"] as bool?;
                if (firstTime == null || firstTime == true)
                {
                    //Usuario debe presionar el boton Conectar
                }
                else
                {
                    backgroundToken = new CancellationTokenSource();

                    Application.Current.Suspending -= Current_Suspending;
                    PebbleName.Text = "";
                    PebbleVersion.Text = "";

                    if (applicationtrigger != null)
                    {
                        //await applicationtrigger.RequestAsync().AsTask(backgroundToken.Token);
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bool? firstTime = ApplicationData.Current.LocalSettings.Values["firstTime"] as bool?;
            if (firstTime == null || firstTime == true)
            {
                //Usuario debe presionar el boton Conectar
            }
            else
            {
                PebbleRegisterAcc.Visibility = Visibility.Collapsed;
                PebbleName.Text = "Connecting...";
                await initPebbleApp();
            }
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //await Task.Run(() => { NotificationReciever.DisposePebble(); });
            Debug.WriteLine("Unloaded!");
        }

        string deviceId = null;
        
        /// <summary>
        /// Inicializa la aplicacion con los datos del Pebble y deja la conexión abierta para pruebas.
        /// </summary>
        private async Task<bool> initPebbleApp(bool registerAllApps = true)
        {

            #region BTLE
            var watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += Watcher_Received;
            watcher.Start();

            #endregion

            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";

            try
            {
                var peers = await PeerFinder.FindAllPeersAsync();
                bool isPebblePaired = false;
                foreach (var item in peers)
                {
                    if (item.DisplayName.StartsWith("Pebble", StringComparison.OrdinalIgnoreCase))
                    {
                        isPebblePaired = true;

                        deviceId =  (await BluetoothDevice.FromHostNameAsync(item.HostName)).DeviceId;

                        //foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(Guid.Parse("00000000-deca-fade-deca-deafdecacaff")))))
                        //{                            
                        //   deviceId = di.Id;
                        //}



                        //deviceId = (await Windows.Devices.Bluetooth.BluetoothDevice.FromHostNameAsync(item.HostName)).DeviceInformation.Id.ToString();
                        //deviceId = item.HostName;
                        //deviceId = Guid.Parse("00000000-deca-fade-deca-deafdecacaff").ToString("B");
                        break;
                    }
                }

                if (!isPebblePaired)
                {
                    throw new Exception("NoPebble");
                }

                PebbleData pData = await Task.Run(async () =>
                {
                    if (ItemList == null)
                        ItemList = new List<AppItem>();
                    RegisterBackgrondTaskWuffPebble(replaceTasks, registerAllApps);

                    while (applicationtrigger == null)
                    {
                        //Espera a que no sea null la tarea..
                    }
                    backgroundToken = new CancellationTokenSource();
                    if (backgroundToken == null)
                        backgroundToken = new CancellationTokenSource();

                    bool errorInit = false;
                    do
                    {
                        try
                        {
                            applicationtrigger.RequestAsync().AsTask(backgroundToken.Token);
                            //await BackgroundTaskInit.TryConnection();
                            errorInit = false;
                        }
                        catch (Exception ex)
                        {
                            errorInit = true;
                            Debug.WriteLine(ex.Message);
                        }
                    } while (errorInit);
                    //ApplicationData.Current.LocalSettings.Values["Name"] = "Pebble test";
                    //ApplicationData.Current.LocalSettings.Values["Version"] = "Version!";
                    return await GetPebbleData();
                    //return await WuffNotificationWatcher.NotificationReciever.GetInfoFromPebble();                   
                    //No 
                });

                PebbleName.Text = (PebbleName.Text == "" || PebbleName.Text == "Not Available" || PebbleName.Text == "Connecting...") ? pData.Name : PebbleName.Text;
                PebbleVersion.Text = (PebbleVersion.Text == "" || PebbleVersion.Text == "Not Available" || PebbleVersion.Text == "Connecting...") ? pData.Version : PebbleVersion.Text;
                GetPebbleApps();
                sucessfullinit = true;
                backgroundToken.Cancel(false);
                
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x8007048F || ex.Message == "NoPebble")
                {
                    ApplicationData.Current.LocalSettings.Values["firstTime"] = true;
                    var dg = new Windows.UI.Popups.MessageDialog("Please turn on BT and pair your Pebble, then Reconect");
                    await dg.ShowAsync();
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:///"));
                    sucessfullinit = false;
                }
            }
            return sucessfullinit;
        }
        bool replaceTasks;
        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // The timestamp of the event
            DateTimeOffset timestamp = args.Timestamp;

            // The type of advertisement
            BluetoothLEAdvertisementType advertisementType = args.AdvertisementType;

            // The received signal strength indicator (RSSI)
            Int16 rssi = args.RawSignalStrengthInDBm;

            // The local name of the advertising device contained within the payload, if any
            string localName = args.Advertisement.LocalName;

            // Check if there are any manufacturer-specific sections.
            // If there is, print the raw data of the first manufacturer section (if there are multiple).
            string manufacturerDataString = "";
            var manufacturerSections = args.Advertisement.ManufacturerData;
            if (manufacturerSections.Count > 0)
            {
                // Only print the first one of the list
                var manufacturerData = manufacturerSections[0];
                var data = new byte[manufacturerData.Data.Length];
                using (var reader = DataReader.FromBuffer(manufacturerData.Data))
                {
                    reader.ReadBytes(data);
                }
                // Print the company ID + the raw data in hex format
                manufacturerDataString = string.Format("0x{0}: {1}",
                    manufacturerData.CompanyId.ToString("X"),
                    BitConverter.ToString(data));
                Debug.WriteLine(manufacturerDataString);
            }
        }

        private async Task<PebbleData> GetPebbleData()
        {
            NotificationReciever.GetInfoFromPebble();
            PebbleData pData = await Task<PebbleData>.Run<PebbleData>(() =>
            {
                object name = null, version = null;
                do
                {
                    name = ApplicationData.Current.LocalSettings.Values["Name"];                    
                    version = ApplicationData.Current.LocalSettings.Values["Version"];
                    Task.Delay(1); //Delay para dejar trabajar a los demas
                } while (name == null || version == null);

                return new PebbleData()
                {
                    Version = version as string,
                    Name = name as string
                };
            }
            );         

            return pData;
        }

        private void GetPebbleApps()
        {
            //Obtengo la informacion de las apps instaladas en el pebble
            NotificationReciever.GetAppsFromPebble();
            bool isNull = true;
            JToken ia = null;
            do
            {
                object InstalledApplications = ApplicationData.Current.LocalSettings.Values["InstalledApplications"];
                if (InstalledApplications == null)
                    continue;
                else
                {
                    ia = JObject.Parse(InstalledApplications.ToString());
                    break;                    
                }
            } while (isNull);
            if (PebbleAppList == null)
                PebbleAppList = new List<PebbleAppItem>();

            PebbleAppList.Clear();
            if (ia != null)
            {
                foreach (var item in ia["ApplicationsInstalled"])
                {
                    PebbleAppList.Add(new PebbleAppItem(item["Name"].ToString(),item["Id"].ToString()));
                }
            }

            lvpItems.ItemsSource = PebbleAppList;
        }

        private async void MusicControlReceived(MusicControlAction action)
        {
            Windows.UI.Popups.MessageDialog md = new Windows.UI.Popups.MessageDialog(action.ToString());
            await md.ShowAsync();
        }

        public static bool sucessfullinit;
        private async void PebbleRegisterAcc_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                PebbleName.Text = "Connecting...";
                ApplicationData.Current.LocalSettings.Values["firstTime"] = false;
                await initPebbleApp();
                if (sucessfullinit)
                {
                    PebbleRegisterAcc.Visibility = Visibility.Collapsed;
                    PebbleDisconnect.Visibility = Visibility.Visible;
                }
                else
                {
                    PebbleRegisterAcc.Visibility = Visibility.Visible;
                    PebbleDisconnect.Visibility = Visibility.Collapsed;
                    PebbleName.Text = "";
                }
            });
        }

        private void registerTask(int taskID)
        {
            try
            {
                AccessoryManager.EnableAccessoryNotificationTypes((int)taskID);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Sorry, cant register:" + taskID);
            }
        }


        ApplicationTrigger applicationtrigger;
        private async void RegisterBackgrondTaskWuffPebble(bool replaceTask, bool registerAllApps = true)
        {
            //Intento de registrar el accesorio
            try
            {
                //Windows.
                //Registrar la app como app. de accesorio, así obtenemos acceso al
                //centro de notificaciones.
                string str = AccessoryManager.RegisterAccessoryApp();
                ////Obtenemos todas las apps instaladas en el telefono.
                IReadOnlyDictionary<String, AppNotificationInfo> apps = AccessoryManager.GetApps();
                ////Modify media state.
                //AccessoryManager.PerformMediaPlaybackCommand(PlaybackCommand.Stop);



                //Habilito la app para escuchar ciertos tipos de notificaciones.
                Int32 enabledAccessoryNotificationTypes = AccessoryManager.GetEnabledAccessoryNotificationTypes();
                Debug.WriteLine("Enabled Notif types: " + enabledAccessoryNotificationTypes);
                Int32 num = enabledAccessoryNotificationTypes; // | notifType;
                if (enabledAccessoryNotificationTypes != num)
                {
                    //AccessoryManager.EnableAccessoryNotificationTypes((int)AccessoryNotificationType.Toast);
                }

                #region Register new Task

                Debug.WriteLine("Enabled Notif types: " + AccessoryManager.GetEnabledAccessoryNotificationTypes());
                //registerTask(16383);
                registerTask(65535);
                Debug.WriteLine("After 16: " + AccessoryManager.GetEnabledAccessoryNotificationTypes());

                ItemList.Clear();
                foreach (var item in apps)
                {
                    bool isEnabled = false;
                    WriteableBitmap _bmi = null;
                    //Debug.WriteLine("Key: " + item.Key + " Value: " + item.Value.Name);

                    isEnabled = AccessoryManager.IsNotificationEnabledForApplication(item.Key);

                    var appItem = new AppItem(
                                item.Value.Name,
                                item.Key,
                                _bmi,
                                isEnabled
                                );


                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ItemList.Add(appItem);
                    });

                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    lvItems.ItemsSource = ItemList;
                });



                BackgroundAccessStatus accessStatus = await BackgroundExecutionManager.RequestAccessAsync();
                if (accessStatus == BackgroundAccessStatus.Denied)
                {
                    Debug.WriteLine("Access Denied");
                    Debug.WriteLine("Trying to remove access to background");
                    BackgroundExecutionManager.RemoveAccess();
                    Debug.WriteLine("Try again");
                    return;
                }
                else
                {
                    Debug.WriteLine("Request Successfull");
                }

                #region Triggers
                //Debo registrar la tarea en bg con un trigger DeviceManufacterNotificationTrigger
                DeviceManufacturerNotificationTrigger devicemanufacturerNotificationTrigger =
                    new DeviceManufacturerNotificationTrigger(
                       "Microsoft.AccessoryManagement.Notification:" + str
                        , false
                        );

                applicationtrigger = new ApplicationTrigger();
                DeviceConnectionChangeTrigger dcct = null;
                #endregion
                
                //PushNotificationTrigger pushNotificationTrigger = new PushNotificationTrigger();
                //We check if this Task is already registered
                var taskRegistered = false;
                //We set a name to identify this task
                var taskName = "NotificationReciever";
                //And search for it in the AllTasks Object
                var taskNumber = ApplicationData.Current.LocalSettings.Values["taskNumber"];
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name.Equals("NotificationReciever") ||
                     task.Value.Name.Equals("AppTrigger") ||
                     task.Value.Name.Equals("SocketInput") ||
                     task.Value.Name.Equals("DeviceConnectionTrigger"))
                    {
                        //taskRegistered = true;
                        taskRegistered = true;
                        if (replaceTask)
                        {
                            task.Value.Unregister(true);//Always clean the task
                            taskRegistered = false;
                        }
                    }
                }

                replaceTasks = false;

                //If the Task is not registered, we proceed to do it
                if (!taskRegistered)
                {
                    var builder = new BackgroundTaskBuilder();
                    var builder2 = new BackgroundTaskBuilder();
                    var builder3 = new BackgroundTaskBuilder();

                    builder.Name = taskName;
                    builder2.Name = "AppTrigger";
                    builder3.Name = "DeviceConnectionTrigger";
                  
                    builder.TaskEntryPoint = "WuffNotificationWatcher.NotificationReciever";
                    builder2.TaskEntryPoint = "WuffNotificationWatcher.BackgroundTaskInit";
                    builder3.TaskEntryPoint = "WuffNotificationWatcher.BackgroundTaskInit";

                    builder.SetTrigger(devicemanufacturerNotificationTrigger);

                    builder2.SetTrigger(applicationtrigger);
                    try {
                        dcct = await DeviceConnectionChangeTrigger.FromIdAsync(deviceId);
                        builder3.SetTrigger(dcct);

                    }catch(Exception ex)
                    {
                        dcct = await DeviceConnectionChangeTrigger.FromIdAsync("serviceId:00000000-deca-fade-deca-deafdecacaff");
                    }
                    //builder2.SetTrigger(DeviceInformation.CreateWatcher().GetBackgroundTrigger(new[] { DeviceWatcherEventKind.Update,DeviceWatcherEventKind.Add,DeviceWatcherEventKind.Remove }.AsEnumerable<DeviceWatcherEventKind>()));
                    //try {
                    //    var builder3 = new BackgroundTaskBuilder();
                    //    builder3.Name = "SocketInput";
                    //    builder3.TaskEntryPoint = "WuffNotificationWatcher.BackgroundTaskInit";
                    //    SocketActivityTrigger st = new SocketActivityTrigger();
                    //    builder3.SetTrigger(st);
                    //    var r = builder3.Register();
                    //}catch(Exception ex)
                    //{
                    //    Debug.WriteLine(ex.Message);
                    //}
                    //builder3.SetTrigger(deviceConnectionChange);


                    Debug.WriteLine("Building Task");
                    BackgroundTaskRegistration task = builder.Register();
                    BackgroundTaskRegistration task2 = builder2.Register();
                    //try {
                    //    BackgroundTaskRegistration task3 = builder3.Register();
                    //}catch(Exception ex)
                    //{
                    //    dcct = await DeviceConnectionChangeTrigger.FromIdAsync("serviceId:00000000-deca-fade-deca-deafdecacaff");
                    //    builder3.SetTrigger(dcct);
                    //    builder3.Register();
                    //    Debug.WriteLine(ex.Message +"\n"+ ex.StackTrace +"\n"+ ex.Source);
                    //}
                    //deviceId = "any";                
                    


                    Debug.WriteLine("Task Registered Id: " + task.TaskId);
                    Debug.WriteLine("Task Registered Id: " + task2.TaskId);
                    //Debug.WriteLine("Task Registered Id: " + task3.TaskId);

                    ApplicationData.Current.LocalSettings.Values["taskNumber"] = "0";
                }
                else
                {
                    Debug.WriteLine("Task already registered");
                }
                #endregion
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERRORRRRRR: " + ex.Message);
            }
        }

        private async void PebbleSendPing_Click(object sender, RoutedEventArgs e)
        {
            lblStatusTest.Text = "Sending...";
            await Task.Run(() =>
            {
                if (isTaskRegistered())
                    WuffNotificationWatcher.NotificationReciever.SendPing();
                else
                {
                    showConnectionWarningMessage();
                }
            });
            await Task.Delay(1000);
            lblStatusTest.Text = "";
            //NotificationReciever.DisposePebble();
        }

        private async void PebbleSendCall_Click(object sender, RoutedEventArgs e)
        {
            lblStatusTest.Text = "Sending...";
            await Task.Run(() =>
            {
                if (isTaskRegistered())
                    WuffNotificationWatcher.NotificationReciever.SendTestCall("Eduardo Noyer", "555-555-555");
                else
                    showConnectionWarningMessage();
            });
            await Task.Delay(1000);
            lblStatusTest.Text = "";
            //NotificationReciever.DisposePebble();
        }

        private async void PebbleSendSMS_Click(object sender, RoutedEventArgs e)
        {
            lblStatusTest.Text = "Sending...";
            await Task.Run(() =>
            {
                if (isTaskRegistered())
                    WuffNotificationWatcher.NotificationReciever.SendSMSTest("PebbleWuff", "SMS from Windows Phone to your Pebble!!, yei!");
                else
                    showConnectionWarningMessage();
            });
            await Task.Delay(1000);
            lblStatusTest.Text = "";
            //NotificationReciever.DisposePebble();
        }

        private async void PebbleNotificationTest_Click(object sender, RoutedEventArgs e)
        {
            lblStatusTest.Text = "Sending...";
            await Task.Run(() =>
            {
                if (isTaskRegistered())
                    WuffNotificationWatcher.NotificationReciever.SendFBTest("PebbleWuff", "Hello!, have a nice day!");
                else
                    showConnectionWarningMessage();
            });
            await Task.Delay(1000);
            lblStatusTest.Text = "";
            //NotificationReciever.DisposePebble();
        }

        private async void PebbleSetCurrentTime_Click(object sender, RoutedEventArgs e)
        {
            lblStatusTest.Text = "Sending...";
            await Task.Run(() =>
            {
                if (isTaskRegistered())
                    WuffNotificationWatcher.NotificationReciever.SetCurrentTime();
                else
                    showConnectionWarningMessage();
            });
            await Task.Delay(1000);
            lblStatusTest.Text = "";
            //NotificationReciever.DisposePebble();
        }

        private bool isTaskRegistered()
        {
            try
            {
                //First we check if we have a running background task using our pebble connection
                //If it's possible, we call some functions from this app to the background class.                 
                //We set a name to identify this task
                var taskName = "NotificationReciever";
                //And search for it in the AllTasks Object
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name.Equals(taskName))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = (sender as ToggleSwitch);
            var ID = toggle.Tag as string;
            if (toggle.IsOn)
            {
                AccessoryManager.EnableNotificationsForApplication(
                    ID);
                Debug.WriteLine("### Enabled App: " + AccessoryManager.IsNotificationEnabledForApplication(ID));
            }
            else
            {
                AccessoryManager.DisableNotificationsForApplication(
                    ID);
                Debug.WriteLine("### Disabled App: " + AccessoryManager.IsNotificationEnabledForApplication(ID));
            }
        }

        private async Task<WriteableBitmap> Convert(IRandomAccessStreamReference parameter)
        {
            IRandomAccessStreamWithContentType streamContent;
            if (parameter != null)
            {
                BitmapImage bmi = new BitmapImage();
                using (streamContent = await parameter.OpenReadAsync())
                {
                    if (streamContent != null)
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(streamContent);
                        BitmapFrame frame = await decoder.GetFrameAsync(0);

                        var bitmap = new WriteableBitmap((int)frame.PixelWidth, (int)frame.PixelHeight);
                        streamContent.Seek(0);

                        await bitmap.SetSourceAsync(streamContent);
                        return bitmap;
                    }
                }
            }
            return new WriteableBitmap(40, 40);
        }

        private async void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            var m = new Windows.UI.Popups.MessageDialog(
                "PebbleWuff 1.0.0\n© 2015 Eduardo Noyer \n\n@Noyer\n\nAll rights reserved.\n\n",
                "About this app");
            await m.ShowAsync();
        }

        private async void PebbleDisconnect_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PebbleName.Text = "Disonnecting...";
                PebbleVersion.Text = "";
                ApplicationData.Current.LocalSettings.Values["firstTime"] = true;                
                backgroundToken.Cancel(false);
                #region Unregister Task
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name.Equals("NotificationReciever")||
                     task.Value.Name.Equals("AppTrigger") ||
                     task.Value.Name.Equals("SocketInput")
                     )
                    {
                        task.Value.Unregister(true);//Always clean the task
                    }
                    break;
                }
                #endregion

                PebbleDisconnect.Visibility = Visibility.Collapsed;
                PebbleRegisterAcc.Visibility = Visibility.Visible;

                PebbleName.Text = "";
                replaceTasks = true;
            });
        }

        private async void showConnectionWarningMessage()
        {
            var md = new Windows.UI.Popups.MessageDialog("First, Connect to your Pebble");
            await md.ShowAsync();
        }

        private async void btnLicense_Click(object sender, RoutedEventArgs e)
        {
            string str = @"License

Copyright (c) 2014, Steve Robbins.
Copyright (c) 2013-2014, p3root - Patrik Pfaffenbauer (patrik.pfaffenbauer@p3.co.at) All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

Neither the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";

            var md = new Windows.UI.Popups.MessageDialog(str, "License");
            await md.ShowAsync();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            //Elimino la app seleccionada
            Button btn = sender as Button;            
            NotificationReciever.RemoveWatchApp(btn.Tag as string);
            GetPebbleApps(); //Obtengo nuevamente mis apps.
        }

        private void btnLaunch_Click(object sender, RoutedEventArgs e)
        {
            //Lanzo la app de acuerdo a su id
            Button btn = sender as Button;
            NotificationReciever.LaunchWatchApp(btn.Tag as string);
        }
    }
}
