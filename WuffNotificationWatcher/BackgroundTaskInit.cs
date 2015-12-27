using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using P3bble.Core;
using P3bble.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Phone.Notification.Management;
using Windows.Storage;
using Windows.UI.Core;
using NotificationsExtensions.Toasts; // NotificationsExtensions.Win10
using Windows.UI.Notifications;

namespace WuffNotificationWatcher
{
    public sealed class BackgroundTaskInit : IBackgroundTask
    {
        BackgroundTaskDeferral deferral = null;
        static string str;

        Timer t1;        
          
        static bool isCanceled = false;
        static string socketID = Guid.NewGuid().ToString();
        public async void DisposeTaskInit(IBackgroundTaskInstance taskInstance, BackgroundTaskCancellationReason cancellationReason)
        {
            try {
                t1.Dispose();
                ShowToast("Tarea Cancelada Razón-> " + cancellationReason.ToString());
                Debug.WriteLine(DateTime.Now.ToString() + ": Tarea Cancelada Razón-> " + cancellationReason.ToString());
                deferral = taskInstance.GetDeferral();
                ShowToast("Intentanto transferir socket al socketBroker");
                await _pebble.TransferOwnership(socketID);
                ShowToast("Transferencia correcta, terminando operación actual...");
                Debug.WriteLine(DateTime.Now.ToString() + ": Transferencia correcta, terminando operación actual...");
                isCanceled = true;
              
            }catch(Exception ex)
            {
                Debug.WriteLine(DateTime.Now.ToString() + ex.Message);
                ShowToast(ex.Message);
            }
            finally
            {
                ApplicationData.Current.LocalSettings.Values["canReconnect"] = "true";                
                deferral.Complete();
            }
        }
        static object locked = new object();

        static P3bble.Core.P3bble _pebble;
        private static byte[] _cookie = new byte[] { 0x00, 0xEB, 0x00, 0x00 };

        public static IAsyncOperation<bool> TryConnection()
        {
            return Task.Run<bool>(async () =>
            {
                bool isConnected = false;
                P3bble.Core.P3bble.IsMusicControlEnabled = true;
                //P3bble.Core.P3bble.IsLoggingEnabled = true;

                List<P3bble.Core.P3bble> pebbles = await P3bble.Core.P3bble.DetectPebbles();

                if (pebbles.Count >= 1)
                {
                    _pebble = pebbles[0];
                    isConnected = await _pebble.ConnectAsync(btr, socket); //Lleva await
                    if (_pebble != null && _pebble.IsConnected)
                    {
                        _pebble.MusicControlReceived += MusicControlReceived;
                        _pebble.InstallProgress += InstallProgressHandler;
                        ApplicationData.Current.LocalSettings.Values["isConnected"] = "true";
                    }
                    return isConnected;
                }
                else
                {
                    return false;
                }
            }).AsAsyncOperation<bool>();
        }

        /// <summary>
        /// Declared here because we need 2 way communication
        /// </summary>
        /// <param name="action"></param>
        private static void MusicControlReceived(MusicControlAction action)
        {
            switch (action)
            {
                case MusicControlAction.PlayPause:
                    switch (AccessoryManager.MediaPlaybackStatus)
                    {
                        case PlaybackStatus.Paused:
                        case PlaybackStatus.Stopped:
                            AccessoryManager.PerformMediaPlaybackCommand(PlaybackCommand.Play);
                            break;
                        case PlaybackStatus.Playing:
                            AccessoryManager.PerformMediaPlaybackCommand(PlaybackCommand.Pause);
                            break;
                    }
                    //AccessoryManager.PerformMediaPlaybackCommand(PlaybackCommand.Play);
                    break;
                case MusicControlAction.Next:
                    AccessoryManager.PerformMediaPlaybackCommand(PlaybackCommand.Next);
                    break;
                case MusicControlAction.Previous:
                    AccessoryManager.PerformMediaPlaybackCommand(PlaybackCommand.Previous);
                    break;
                case MusicControlAction.VolDown:
                    AccessoryManager.DecreaseVolume(1);
                    break;
                case MusicControlAction.VolUp:
                    AccessoryManager.IncreaseVolume(1);
                    break;
            }
        }

        private static void InstallProgressHandler(int percentComplete)
        {
            ApplicationData.Current.LocalSettings.Values["installProgress"] = percentComplete.ToString();
            Debug.WriteLine(DateTime.Now.ToString() + " InstallProgress: " +percentComplete + "%" );
        }
        private async void Callback(object state)
        {            
            try {
                t1.Dispose();
                ShowToast("Se superó el tiempo de espera, intentanto transferir socket al socketBroker");
                Debug.WriteLine(DateTime.Now.ToString() + ": Se superó el tiempo de espera, intentanto transferir socket al socketBroker ");
                await _pebble.TransferOwnership(socketID);
                isCanceled = true;
                ShowToast("Transferencia correcta, terminando operación actual...");
                Debug.WriteLine(DateTime.Now.ToString() + ": Transferencia correcta, terminando operación actual...");               
            }
            catch (Exception ex)
            {
                Debug.WriteLine(DateTime.Now.ToString() + ex.Message);
                ShowToast(ex.Message);
            }
            finally
            {
                ApplicationData.Current.LocalSettings.Values["canReconnect"] = "true";
                deferral.Complete();
            }
        }

        private void ShowToast(string message)
        {
            string title = "Pebble Wuff!";
            string content = message;

            // Construct the visuals of the toast
            ToastVisual visual = new ToastVisual()
            {
                TitleText = new ToastText()
                {
                    Text = title
                },

                BodyTextLine1 = new ToastText()
                {
                    Text = content
                }                
            };


            // Now we can construct the final toast content
            ToastContent toastContent = new ToastContent()
            {
                Visual = visual
            };

            // And create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddDays(2);
            toast.Tag = "1";
            toast.Group = "wuffP";

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        //private static void InstallProgressReceived(int percentComplete)
        //{
        //    throw new NotImplementedException();
        //}
        static Guid btr;
        static StreamSocket socket = null;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            GC.KeepAlive(this);
            GC.SuppressFinalize(this);
            //t1 = new Timer(Callback, null, 1000 * 30, 0);
            t1 = new Timer(Callback, null, 1000 *60* 8, 0);

            deferral = taskInstance.GetDeferral();

            ShowToast("Called!! " + taskInstance.TriggerDetails.GetType().Name);
            Debug.WriteLine(DateTime.Now.ToString() + ": Called! " + taskInstance.TriggerDetails.GetType().Name);
            taskInstance.Canceled += DisposeTaskInit;
            #region
            bool isRegistered;
            if (ApplicationData.Current.LocalSettings.Values["SocketInput"] == null)
            {
                isRegistered = false;
            }
            else
            {
                bool.TryParse(ApplicationData.Current.LocalSettings.Values["SocketInput"].ToString(), out isRegistered);
            }
            if (!isRegistered)
            {
                try
                {
                    var res = await BackgroundExecutionManager.RequestAccessAsync();
                    Debug.WriteLine(res.ToString());
                    var builder3 = new BackgroundTaskBuilder();
                    builder3.Name = "SocketInput";
                    builder3.TaskEntryPoint = "WuffNotificationWatcher.BackgroundTaskInit";
                    SocketActivityTrigger st = new SocketActivityTrigger();
                    builder3.SetTrigger(st);
                    var r = builder3.Register();
                    btr = r.TaskId;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                ApplicationData.Current.LocalSettings.Values["SocketInput"] = "true";
            }
            else
            {
                //Busco mi tarea en las registradas
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name.Equals("SocketInput"))
                    {
                        btr = task.Value.TaskId;
                        break;
                    }
                }
            }
            #endregion

            //Verifico si es un trigger de socket! y lleno mis variables.
            if (taskInstance.TriggerDetails.GetType() == typeof(SocketActivityTriggerDetails))
            {
                bool canReconnect;
                //lock (ApplicationData.Current.LocalSettings.Values["canReconnect"])
                //{
                object canReconnectS = "";
                ApplicationData.Current.LocalSettings.Values.TryGetValue("canReconnect", out canReconnectS);

                if (canReconnectS == null)
                {
                    canReconnect = true;
                }
                else
                {
                    bool.TryParse((string)canReconnectS, out canReconnect);
                }
                //}
                //lock (_pebble)
                //{
                //if (!canReconnect)
                //{
                //    deferral.Complete();
                //    return;
                //}
                ApplicationData.Current.LocalSettings.Values["canReconnect"] = "false";
                var details = taskInstance.TriggerDetails as SocketActivityTriggerDetails;
                var socketInformation = details.SocketInformation;

                switch (details.Reason)
                {
                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                    case SocketActivityTriggerReason.SocketActivity:
                    //if (socketInformation.StreamSocket == null)
                    //{
                    //    Debug.WriteLine(DateTime.Now.ToString() + ": StreamSocket es null");
                    //    btr = taskInstance.Task.TaskId;
                    //    socket = null;
                    //    break;
                    //}
                    //else
                    //{
                    //    Debug.WriteLine(DateTime.Now.ToString() + ": StreamSocket no es null");
                    //    try
                    //    {
                    //        lock (socketInformation)
                    //        {
                    //            socket = socketInformation.StreamSocket;
                    //            socketID = socketInformation.Id;
                    //            btr = taskInstance.Task.TaskId;
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Debug.WriteLine(DateTime.Now.ToString() + ": " + ex.Message);
                    //    }
                    //    break;
                    //}
                    case SocketActivityTriggerReason.SocketClosed:
                        Debug.WriteLine(DateTime.Now.ToString() + ": Me desencadené por " + details.Reason.ToString());
                        btr = taskInstance.Task.TaskId;
                        socket = null;
                        break;
                }
            }
            //}

            try
            {
                #region Test Get Notification
                //Obtengo el siguiente trigger de notificacion
                while (true)
                {
                    if (isCanceled)
                    {
                        //deferral.Complete();
                        break;
                    }                   

                    string val = (string)ApplicationData.Current.LocalSettings.Values["isConnected"];
                    do
                    {
                        if (_pebble != null)
                        {
                            if (_pebble.IsConnected)
                            {
                                break;
                            }
                        }

                    } while (await TryConnection() == false);

                    //_pebble.Disconnect();
                    //await _pebble.TransferOwnership(socketID);
                    //break;
                    ApplicationData.Current.LocalSettings.Values["isConnected"] = "true";
                    if (str == null)
                    {
                        str = "";// AccessoryManager.RegisterAccessoryApp();
                    }
                    //If we want to so use await methods on this class we must
                    //get the deferral so this will prevent the class from terminate
                    //before the current async operation finish.


                    dynamic nextTriggerDetails;

                    /*do
                    {
                        nextTriggerDetails = AccessoryManager.GetNextTriggerDetails();
                    } while (nextTriggerDetails == null);

                    //Proceso lo que trae esta notificacion
                    if (nextTriggerDetails != null)
                    {
                        AccessoryManager.ProcessTriggerDetails(nextTriggerDetails);
                    }*/

                    object[] notificationData = null;

                    notificationData = getNotificationFromStack();

                    object notificationType;
                    if (notificationData != null)
                    {
                        if (notificationData[0] == null)
                        {
                            notificationType = notificationData[0];
                            //nextTriggerDetails = (IAccessoryNotificationTriggerDetails)notificationData[1]; //Arroja exception.
                            nextTriggerDetails = notificationData[1];
                        }
                        else
                        {
                            notificationType = notificationData[0];
                            nextTriggerDetails = notificationData[1];
                        }
                    }
                    else
                    {
                        await Task.Delay(1);
                        continue;
                    }

                    //Recorro los detalles, mientras no sean null
                    //while (nextTriggerDetails != null)
                    //{
                    Debug.WriteLine("Obtuve un trigger!");
                    //Debug.WriteLine("Nombre de app de notificacion: " + nextTriggerDetails.AppDisplayName);
                    //Debug.WriteLine("Tipo de notificacion: " + nextTriggerDetails.AccessoryNotificationType);
                    //Debug.WriteLine("################" + AccessoryManager.GetEnabledAccessoryNotificationTypes() + "#############");

                    if (_pebble != null && _pebble.IsConnected)
                    {
                        //lock (locked)
                        //{
                        if (notificationType != null)
                        {
                            //Custom code
                            switch ((long)notificationType)
                            {
                                case 0:
                                    //Set Time
                                    _pebble.SetTimeAsync(DateTime.Now).AsAsyncAction().AsTask().Wait();
                                    break;
                                case 1:
                                    //Ping
                                    _pebble.PingAsync().AsAsyncAction().AsTask().Wait();
                                    break;
                                case 2:
                                    //Test Call
                                    _pebble.PhoneCallAsync(
                                        (string)notificationData[1],
                                        (string)notificationData[2],
                                       _cookie).AsAsyncAction().AsTask().Wait();
                                    break;
                                case 3:
                                    //SMS
                                    _pebble.SmsNotificationAsync(
                                        (string)notificationData[1],
                                        (string)notificationData[2]).AsAsyncAction().AsTask().Wait();
                                    break;
                                case 4:
                                    _pebble.FacebookNotificationAsync(
                                        (string)notificationData[1],
                                        (string)notificationData[2]).AsAsyncAction().AsTask().Wait();
                                    break;
                                case 5:
                                    do
                                    {
                                        await Task.Delay(1);
                                    } while (_pebble.DisplayName == null || _pebble.FirmwareVersion == null);
                                    ApplicationData.Current.LocalSettings.Values["Name"] = _pebble.DisplayName?.ToString();
                                    ApplicationData.Current.LocalSettings.Values["Version"] = _pebble.FirmwareVersion?.ToString();
                                    break;
                                case 6:
                                    await _pebble.InstallAppAsync(await _pebble.DownloadBundleAsync((string)notificationData[1]));
                                    break;
                                case 7:
                                    await _pebble.GetRunningAppsAsync();
                                    var installedApps = await _pebble.GetInstalledAppsAsync();
                                    ApplicationData.Current.LocalSettings.Values["InstalledApplications"] = JObject.FromObject(installedApps).ToString();
                                    break;
                                case 8:
                                    //Meh meh meh
                                    ApplicationData.Current.LocalSettings.Values["InstalledApplications"] = null;
                                    uint id = uint.Parse((string)notificationData[1]);
                                    await _pebble.RemoveAppAsync((await _pebble.GetInstalledAppsAsync()).ApplicationsInstalled.Where(x=>x.Id==id).FirstOrDefault());
                                    break;
                                case 9:
                                    uint id2 = uint.Parse(notificationData[1].ToString());
                                    await _pebble.LaunchAppAsync((await _pebble.GetInstalledAppsAsync()).ApplicationsInstalled.Where(x => x.Id == id2).FirstOrDefault());
                                    break;
                            }
                        }
                        else
                        {
                            //Es una notificacion que viene del accessory Manager

                            var triggerDetails = notificationData[1]; //Contiene la info de la notifiacion
                            long notifType = (long)notificationData[2];

                            switch (notifType)
                            {
                                case (long)AccessoryNotificationType.Phone:
                                    //Tipo de notificacion telefonica, puede ser de otro tipo mas
                                    //PhoneNotificationTriggerDetails phonenotificationTriggerDetail = nextTriggerDetails as PhoneNotificationTriggerDetails;

                                    //switch (phonenotificationTriggerDetail.PhoneNotificationType)
                                    //{
                                    //    //El teléfono difiere entre sms y otra cosa mas.
                                    //    case PhoneNotificationType.CallChanged:
                                    //        break;
                                    //    case PhoneNotificationType.LineChanged:
                                    //        break;
                                    //    case PhoneNotificationType.NewCall:
                                    //        string contactName = phonenotificationTriggerDetail.CallDetails.ContactName;
                                    //        string phoneNumber = phonenotificationTriggerDetail.CallDetails.PhoneNumber;
                                    //        _pebble.PhoneCallAsync(contactName, phoneNumber, _cookie).AsAsyncAction().AsTask().Wait();
                                    //        break;
                                    //    case PhoneNotificationType.PhoneCallAudioEndpointChanged:
                                    //        break;
                                    //    case PhoneNotificationType.PhoneMuteChanged:
                                    //        break;
                                    //}
                                    break;
                                case (long)AccessoryNotificationType.Email:
                                    //EmailNotificationTriggerDetails emailNotificationTriggerDetail = nextTriggerDetails as EmailNotificationTriggerDetails;
                                    //_pebble.EmailNotificationAsync(
                                    //    emailNotificationTriggerDetail.SenderName,
                                    //    emailNotificationTriggerDetail.EmailMessage.Subject,
                                    //    emailNotificationTriggerDetail.EmailMessage.Body).AsAsyncAction().AsTask().Wait();
                                    break;
                                case (long)AccessoryNotificationType.Alarm:
                                    //AlarmNotificationTriggerDetails alarmNotificationTriggerDetail = nextTriggerDetails as AlarmNotificationTriggerDetails;
                                    //_pebble.FacebookNotificationAsync("WuffAlarm", alarmNotificationTriggerDetail.Title).AsAsyncAction().AsTask().Wait();
                                    break;
                                case (long)AccessoryNotificationType.VolumeChanged:
                                    //await _pebble.SmsNotificationAsync("PebbleWuff", "Cambiaste el vol!");//Usualmente lleva await                                
                                    break;
                                case (long)AccessoryNotificationType.Toast:
                                    //Windows.Phone.Notification.Management.ToastNotificationTriggerDetails toastNotificationTriggerDetail = nextTriggerDetails as Windows.Phone.Notification.Management.ToastNotificationTriggerDetails;
                                    //_pebble.SmsNotificationAsync(toastNotificationTriggerDetail.AppDisplayName,
                                    //    toastNotificationTriggerDetail.Text1 + " " +
                                    //    toastNotificationTriggerDetail.Text2 + " " +
                                    //    toastNotificationTriggerDetail.Text3 + " " +
                                    //    toastNotificationTriggerDetail.Text4).AsAsyncAction().AsTask().Wait();
                                    break;
                                case (long)AccessoryNotificationType.Media:
                                    //MediaControlsTriggerDetails mediaControlsTriggerDetails = nextTriggerDetails as MediaControlsTriggerDetails;
                                    string s1 = triggerDetails.ToString() + "";

                                    JObject data = JObject.Parse(s1);
                                    if (data != null)
                                    {
                                        try
                                        {
                                            //Debug.WriteLine(data[0].AppId.ToString());
                                            string artist = data["MediaMetadata"]["Artist"] == null ? "" : data["MediaMetadata"]["Artist"].ToString();
                                            string album = data["MediaMetadata"]["Album"] == null ? "" : data["MediaMetadata"]["Album"].ToString();
                                            string title = data["MediaMetadata"]["Title"] == null ? "Not Available" : data["MediaMetadata"]["Title"].ToString();

                                            await _pebble.SetNowPlayingAsync(
                                                data["MediaMetadata"]["Artist"].ToString(),
                                                data["MediaMetadata"]["Album"].ToString(),
                                                data["MediaMetadata"]["Title"].ToString()
                                                );
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }
                                    break;
                                case (long)AccessoryNotificationType.CortanaTile:
                                    //CortanaTileNotificationTriggerDetails cortanaNotificationTriggerDetails = nextTriggerDetails as CortanaTileNotificationTriggerDetails;
                                    //string content = cortanaNotificationTriggerDetails.Content + " ";
                                    //content += cortanaNotificationTriggerDetails.EmphasizedText + " ";
                                    //content += (cortanaNotificationTriggerDetails.LargeContent1 != "") ?
                                    //    cortanaNotificationTriggerDetails.LargeContent1 :
                                    //    cortanaNotificationTriggerDetails.LargeContent2;
                                    //content += " " + cortanaNotificationTriggerDetails.NonWrappedSmallContent1 + " " +
                                    //                 cortanaNotificationTriggerDetails.NonWrappedSmallContent2 + " " +
                                    //                 cortanaNotificationTriggerDetails.NonWrappedSmallContent3 + " " +
                                    //                 cortanaNotificationTriggerDetails.NonWrappedSmallContent4 + " ";
                                    //content += cortanaNotificationTriggerDetails.Source;
                                    //_pebble.SmsNotificationAsync("Cortana", content).AsAsyncAction().AsTask().Wait();
                                    break;
                            }
                        }
                        //}
                        //Finally we tell that the background task can terminate.
                        //deferral.Complete();
                        //return;
                    }
                    //Proceso el siguiente detalle que viene en las notificaciones.
                    //AccessoryManager.ProcessTriggerDetails(nextTriggerDetails);
                    //nextTriggerDetails = AccessoryManager.GetNextTriggerDetails();
                    await Task.Delay(1);
                }
                //}
                //taskInstance.Canceled -= new BackgroundTaskCanceledEventHandler(DisposeTaskInit);
                #endregion
                //deferral.Complete();
                //return;
            }
            catch (Exception ex)
            {
                ShowToast(ex.Message);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine(ex.InnerException.ToString());
                //Test
                //Finally we tell that the background task can terminate.
                //DisposePebble();
                //ApplicationData.Current.LocalSettings.Values["isConnected"] = null;               
                //deferral.Complete();
                //return;
                this.Run(taskInstance);
            }
            finally
            {
                //DisposePebble();
            }
        }

        #region Refactor
        private void addNotificationToStack()
        {
            IAccessoryNotificationTriggerDetails nextTriggerDetails;
            do
            {
                nextTriggerDetails = AccessoryManager.GetNextTriggerDetails();
            } while (nextTriggerDetails == null);

            //Proceso lo que trae esta notificacion
            if (nextTriggerDetails != null)
            {
                AccessoryManager.ProcessTriggerDetails(nextTriggerDetails);
            }

            //Almaceno el triggerDetail
            //if (ApplicationData.Current.LocalSettings.Values["notifications"] == null)
            //{
            //    ApplicationData.Current.LocalSettings.Values["notifications"] = new Stack<object[]>();
            //}
            var notificationArray = new object[] { null, nextTriggerDetails, (int)nextTriggerDetails.AccessoryNotificationType };


            List<object[]> objArray = new List<object[]>();
            //if (ApplicationData.Current.LocalSettings.Values["notifications"] == null)
            //{
            //    //Genero el array
            //    objArray = new List<object[]>();
            //}
            //else
            //{
            //    //objArray = JsonConvert.DeserializeObject<List<object[]>>(ApplicationData.Current.LocalSettings.Values["notifications"] as string);
            //    objArray = new List<object[]>();
            //    var jtoken = JsonConvert.SerializeObject(notificationArray);
            //    var jtokenD = JsonConvert.DeserializeObject<JToken>(jtoken);
            //    jtokenD.ElementAt(2).AddAfterSelf(notificationArray);
            //}

            objArray.Add(notificationArray);


            string s = JsonConvert.SerializeObject(objArray);
            var jtokenD = JsonConvert.DeserializeObject<JToken>(s);
            Debug.WriteLine(jtokenD.ToString());
            Debug.WriteLine("#################################################");
            Debug.WriteLine(jtokenD.ElementAt(0)?.ToString());
            Debug.WriteLine(jtokenD.ElementAt(1)?.ToString());
            Debug.WriteLine(jtokenD.ElementAt(2)?.ToString());
            ApplicationData.Current.LocalSettings.Values["notifications"] = s;
        }

        private static void addTestToStack(int testType, params object[] args)
        {
            //Almaceno el triggerDetail
            //if (ApplicationData.Current.LocalSettings.Values["notifications"] == null)
            //{
            //ApplicationData.Current.LocalSettings.Values["notifications"] = new object();
            //}

            object[] notificationArray = null;
            switch (testType)
            {
                case 2:
                    notificationArray = new object[] { testType, args[0], args[1], args[2] };
                    break;
                case 3:
                    notificationArray = new object[] { testType, args[0], args[1] };
                    break;
                case 4:
                    notificationArray = new object[] { testType, args[0], args[1] };
                    break;
                default:
                    notificationArray = new object[] { testType, null };
                    break;
            }

            List<object[]> objArray;
            object[] currentArray = null;
            if (ApplicationData.Current.LocalSettings.Values["notifications"] == null)
            {
                //Genero el array
                objArray = new List<object[]>();
            }
            else
            {
                objArray = JsonConvert.DeserializeObject<List<object[]>>(ApplicationData.Current.LocalSettings.Values["notifications"] as string);
            }
            objArray.Add(notificationArray);
            string s = JsonConvert.SerializeObject(objArray);
            ApplicationData.Current.LocalSettings.Values["notifications"] = s;
        }

        private object[] getNotificationFromStack()
        {
            lock (ApplicationData.Current.LocalSettings.Values["notifications"])
            {
                string val = (string)ApplicationData.Current.LocalSettings.Values["notifications"];

                if (val != null)
                {
                    JToken noti = JsonConvert.DeserializeObject<JToken>(val);
                    if (noti != null)
                    {
                        if (noti.Root.Count() >= 3)
                        {
                            Debug.WriteLine(noti.ToString());
                            Debug.WriteLine("Id of Notification: " + noti.Root[2]);
                        }
                    }
                    List<object[]> notifications = JsonConvert.DeserializeObject<List<object[]>>(val);

                    if (notifications.Count > 0)
                    {
                        object[] obj = notifications[0];

                        notifications.RemoveAt(0);
                        val = JsonConvert.SerializeObject(notifications);
                        ApplicationData.Current.LocalSettings.Values["notifications"] = val;

                        return obj;
                    }
                }
            }
            return null;
        }
        #endregion

        public static bool isPebbleConnected()
        {

            string isConnected = (string)ApplicationData.Current.LocalSettings.Values["isConnected"];
            if (isConnected != null && isConnected == "true")
                return true;
            else
                return false;
        }

        public static void DisposePebble()
        {
            try
            {
                if (_pebble != null)
                    _pebble.Disconnect();

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                _pebble = null;
                ApplicationData.Current.LocalSettings.Values["isConnected"] = null;
            }
        }
    }
}

