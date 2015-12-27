using P3bble.Core;
using P3bble.Core.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Phone.Notification.Management;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WuffNotificationWatcher
{
    /// <summary>
    /// This class is going to handle all the background operations
    /// Geting the new notification and send it as a message to the pebble
    /// In the future, this will handle all the music/media actions triggered
    /// by the Pebble
    /// </summary>
    public sealed class NotificationReciever : IBackgroundTask
    {
        BackgroundTaskDeferral deferral = null;        
        private static byte[] _cookie = new byte[] { 0x00, 0xEB, 0x00, 0x00 };

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();            
            Debug.WriteLine(DateTime.Now.ToString() + ": -------------NotificationReciever--------------");
            try
            {
                //Add notification to stack
                addNotificationToStack();
                deferral.Complete();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
            try {
                lock (ApplicationData.Current.LocalSettings.Values["notifications"])
                {
                    string s = JsonConvert.SerializeObject(objArray);
                    //var jtokenD = JsonConvert.DeserializeObject<JToken>(s);
                    //Debug.WriteLine(jtokenD.ToString());
                    //Debug.WriteLine("#################################################");
                    ApplicationData.Current.LocalSettings.Values["notifications"] = s;
                }
            }catch(InvalidOperationException ioe)
            {
                Debug.WriteLine(ioe.Message);
                Debug.WriteLine(ioe.StackTrace);
                Debug.WriteLine(ioe.Source);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine(ex.Source);
            }
        }
        static object locked = new object();
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
                case 6:
                    notificationArray = new object[] { testType, args[0] };
                    break;
                case 8:
                    notificationArray = new object[] { testType, args[0] };
                    break;
                case 9:
                    notificationArray = new object[] { testType, args[0] };
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
        #endregion
        
        public static void SetCurrentTime()
        {
            addTestToStack(0);
            return;
        }
        public static void SendPing()
        {
            addTestToStack(1);
            return;
        }
        public static void SendTestCall(string name, string number)
        {
          
            addTestToStack(2, name, number, _cookie);
            return;
            
        }
        public static void SendSMSTest(string sender, string message)
        {
           
            addTestToStack(3, sender, message);
            return;
            
        }
        public static void SendFBTest(string sender, string message)
        {
         
            addTestToStack(4, sender, message);
            return;
            
        }
        public static void GetInfoFromPebble()
        {
            addTestToStack(5);
            return;
        }
        public static void InstallWatchApp(string uri)
        {
            addTestToStack(6, uri);
            return;
        }
        public static void GetAppsFromPebble()
        {
            addTestToStack(7);
            return;
        }
        public static void RemoveWatchApp(string id)
        {
            addTestToStack(8, id);
            return;
        }
        public static void LaunchWatchApp(string id)
        {
            addTestToStack(9, uint.Parse(id));
            return;
        }
    }
    public struct PebbleData
    {
        public string Version;
        public string Name;
    }
}
