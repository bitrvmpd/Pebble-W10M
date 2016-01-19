using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using P3bble.Core.Constants;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using P3bble.PCL;
using P3bble.Core.Messages;
using Windows.Networking.Proximity;
using Windows.Devices.Bluetooth.Rfcomm;

namespace P3bble.Core.Communication
{
    /// <summary>
    /// Encapsulates comms with the Pebble
    /// </summary>
    internal class Protocol : IDisposable
    {
        private readonly Mutex _mutex = new Mutex();
        private StreamSocket _socket;
        public StreamSocket Socket { get { return _socket; } }

        private DataWriter _writer;
        private DataReader _reader;
        private object _lock;
        public bool _isRunning;

        public string socketID = "";

        private Protocol(StreamSocket socket)
        {
            this._socket = socket;
            this._writer = new DataWriter(this._socket.OutputStream);
            this._reader = new DataReader(this._socket.InputStream);

            this._lock = new object();
            /*
            #if WINDOWS_PHONE
                this._isRunning = true;
                System.Threading.ThreadPool.QueueUserWorkItem(this.Run);
            #else
            */
            this._isRunning = true;
            this.Run(null);
            //#endif
        }

        public delegate void MessageReceivedHandler(P3bbleMessage message);

        public MessageReceivedHandler MessageReceived { get; set; }
        public static string SerialPort { get; private set; }

        /// <summary>
        /// Creates the protocol - encapsulates the socket creation
        /// </summary>
        /// <param name="peer">The peer</param>
        /// <returns>A protocol object</returns>
        public static async Task<Protocol> CreateProtocolAsync(PeerInformation peer, StreamSocket _socket, Guid backgroundTaskID)
        {
            bool isNull = false;
            if (_socket == null)
            {
                ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  El socket es null");
                isNull = true;
            }
            else
                ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  El socket no es null");

            StreamSocket socket = (_socket == null) ? new StreamSocket() : _socket;
            bool error = true;
            do
            {
                try
                {
                    cts = new CancellationTokenSource();
                    if (backgroundTaskID != Guid.Empty)
                    {
                        ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  Habilitaré TransferOwnership");
                        if (isNull)
                            socket.EnableTransferOwnership(backgroundTaskID, SocketActivityConnectedStandbyAction.Wake);
                    }
                    else
                    {
                        ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  No habilité TransferOwnership");
                    }
                    if (isNull)
                        await socket.ConnectAsync(peer.HostName, Guid.Parse("00000000-deca-fade-deca-deafdecacaff").ToString("B")).AsTask(cts.Token);
                    error = false;
                    //await socket.CancelIOAsync();
                    //var context = new SocketActivityContext();
                    //socket.TransferOwnership("CustomSocket");
                    //await socket.ConnectAsync(peer.HostName, RfcommServiceId.SerialPort.Uuid.ToString()).AsTask(cts.Token);
                }
                catch (Exception ex)
                {
                    socket = (_socket == null) ? new StreamSocket() : _socket;
                    ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:" + ex.Message);
                }
            } while (error);
            return new Protocol(socket);
        }
        public static async Task<Protocol> CreateProtocolAsync(RfcommDeviceService peer)
        {
            cts = new CancellationTokenSource();
            StreamSocket socket = new StreamSocket();
            await socket.ConnectAsync(peer.ConnectionHostName, Guid.Parse("00000000-deca-fade-deca-deafdecacaff").ToString("B")).AsTask(cts.Token);
            //await socket.ConnectAsync(peer.ConnectionHostName, RfcommServiceId.SerialPort.Uuid.ToString()).AsTask(cts.Token);
            ServiceLocator.Logger.WriteLine("DeviceID: " + peer.Device.DeviceId);
            return new Protocol(socket);
        }


        /// <summary>
        /// Sends a message to the Pebble.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>An async task to wait</returns>
        public Task WriteMessage(P3bbleMessage message)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    try
                    {
                        //this._mutex.ReleaseMutex();
                    }
                    catch (Exception)
                    {
                        ServiceLocator.Logger.WriteLine("##Traté del liberar un mutex y no pude :<##");
                    }
                    ctsSW = new CancellationTokenSource();
                    this._mutex.WaitOne();

                    byte[] package = message.ToBuffer();
                    ServiceLocator.Logger.WriteLine("<< SEND MESSAGE FOR ENDPOINT " + ((Endpoint)message.Endpoint).ToString() + " (" + ((int)message.Endpoint).ToString() + ")");
                    ServiceLocator.Logger.WriteLine("<< PAYLOAD: " + BitConverter.ToString(package));

                    this._writer.WriteBytes(package);
                    await this._writer.StoreAsync().AsTask(ctsSW.Token);

                    this._mutex.ReleaseMutex();
                }
                catch (AbandonedMutexException ex)
                {
                    this._mutex.ReleaseMutex();
                    ServiceLocator.Logger.WriteLine(ex.Message);
                    ServiceLocator.Logger.WriteLine(ex.StackTrace);
                    await WriteMessage(message);
                }
            });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {

                if (this._writer != null)
                {
                    ctsSW.Cancel(false);
                    //this._writer.Dispose();
                    //this._writer = null;
                }

                if (this._reader != null)
                {
                    ctsSR.Cancel(false);
                    ctsSR2.Cancel(false);
                    //this._reader.DetachBuffer();
                    //this._reader.DetachStream();
                    //this._reader.Dispose();
                    //this._reader = null;
                }

                //if (this._socket != null)
                //{
                //    cts.Cancel(false);
                //    this._socket.Dispose();
                //    this._socket = null;
                //}

                //if (this._mutex != null)
                //{
                //    this._mutex.ReleaseMutex();                    
                //    this._mutex.Dispose();
                //}
            }
            catch (TaskCanceledException ex)
            {
                ServiceLocator.Logger.WriteLine(ex.Message);
            }
        }

        static CancellationTokenSource cts;
        public static CancellationTokenSource ctsSR;
        static CancellationTokenSource ctsSR2;
        static CancellationTokenSource ctsSW;

        private async void Run(object host)
        {
            //Task.Factory.StartNew(
            //    async () =>
            //    {
            var readMutex = new AsyncLock();

            while (this._isRunning)
            {
                try
                {
                    ctsSR = new CancellationTokenSource();
                    if (this._reader != null)
                    {
                        await this._reader.LoadAsync(4).AsTask(ctsSR.Token);

                        ServiceLocator.Logger.WriteLine("[message available]");
                        using (await readMutex.LockAsync())
                        {
                            ServiceLocator.Logger.WriteLine("[message unlocked]");
                            uint payloadLength;
                            uint endpoint;

                            if (this._reader.UnconsumedBufferLength > 0)
                            {
                                IBuffer buffer = this._reader.ReadBuffer(4);

                                this.GetLengthAndEndpoint(buffer, out payloadLength, out endpoint);
                                ServiceLocator.Logger.WriteLine(">> RECEIVED MESSAGE FOR ENDPOINT: " + ((Endpoint)endpoint).ToString() + " (" + endpoint.ToString() + ") - " + payloadLength.ToString() + " bytes");
                                if (endpoint > 0 && payloadLength > 0)
                                {
                                    byte[] payload = new byte[payloadLength];

                                    ctsSR2 = new CancellationTokenSource();
                                    await this._reader.LoadAsync(payloadLength).AsTask(ctsSR2.Token);


                                    this._reader.ReadBytes(payload);

                                    P3bbleMessage msg = this.ReadMessage(payload, endpoint);

                                    if (msg != null && this.MessageReceived != null)
                                    {
                                        this.MessageReceived(msg);
                                    }
                                }
                                else
                                {
                                    ServiceLocator.Logger.WriteLine(">> RECEIVED MESSAGE WITH BAD ENDPOINT OR LENGTH: " + endpoint.ToString() + ", " + payloadLength.ToString());
                                }
                            }
                        }
                        await Task.Delay(1);
                        if (_isRunning == false)
                        {
                            //ctsSR.Cancel(false);
                            ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  Intentando cancelar operaciones");
                            await _socket.CancelIOAsync();
                            ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  Cancelación correcta. Transfiriendo...");
                            var context = new SocketActivityContext(_reader.DetachBuffer());
                            _socket.TransferOwnership(socketID,context);
                            ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  OK! Transferencia Correcta.");
                            _ok = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  " + ex.Message);
                    ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  " + ex.StackTrace);
                    ServiceLocator.Logger.WriteLine(DateTime.Now.ToString() + " Protocol.cs:  " + ex.Source);
                }
            }

            //        await Task.Delay(100);
            //    }
            //},
            //TaskCreationOptions.LongRunning);
        }
        internal bool _ok = false;
        private void GetLengthAndEndpoint(IBuffer buffer, out uint payloadLength, out uint endpoint)
        {
            if (buffer.Length != 4)
            {
                payloadLength = 0;
                endpoint = 0;
                return;
            }

            byte[] payloadSize = new byte[2];
            byte[] endpo = new byte[2];

            using (var dr = DataReader.FromBuffer(buffer))
            {
                dr.ReadBytes(payloadSize);
                dr.ReadBytes(endpo);
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payloadSize);
                Array.Reverse(endpo);
            }

            payloadLength = BitConverter.ToUInt16(payloadSize, 0);
            endpoint = BitConverter.ToUInt16(endpo, 0);
        }

        private P3bbleMessage ReadMessage(byte[] payloadContent, uint endpoint)
        {
            List<byte> lstBytes = payloadContent.ToList();
            byte[] array = lstBytes.ToArray();
            ServiceLocator.Logger.WriteLine(">> PAYLOAD: " + BitConverter.ToString(array));
            return P3bbleMessage.CreateMessage((Endpoint)endpoint, lstBytes);
        }

        private IBuffer GetBufferFromByteArray(byte[] package)
        {
            using (DataWriter dw = new DataWriter())
            {
                dw.WriteBytes(package);
                return dw.DetachBuffer();
            }
        }
    }
}
