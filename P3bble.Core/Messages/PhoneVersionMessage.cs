using System;
using System.Collections.Generic;
using System.Linq;
using P3bble.PCL;
using P3bble.Core.Constants;

namespace P3bble.Core.Messages
{
    internal class PhoneVersionMessage : P3bbleMessage
    {
        private const uint PhoneSessionCaps = (uint)SessionCaps.GammaRay;
        private const uint RemoteCapsMusicControl = (uint)(RemoteCaps.Telephony | RemoteCaps.Sms | RemoteCaps.BTLE | RemoteCaps.Android | RemoteCaps.CameraFront | RemoteCaps.CameraRear | RemoteCaps.Acceleromter | RemoteCaps.Compass | RemoteCaps.Gps);
        //private const uint RemoteCapsMusicControl = (uint)(RemoteCaps.Telephony | RemoteCaps.Sms | RemoteCaps.Android);
        private const uint RemoteCapsNormal = (uint)(RemoteCaps.Telephony | RemoteCaps.Sms | RemoteCaps.Windows | RemoteCaps.Gps);

        private uint _remoteCaps;

        public PhoneVersionMessage(bool musicControlEnabled)
            : base(Endpoint.PhoneVersion) //Pebble rejected Endpoint.PhoneVersion
        {
            ServiceLocator.Logger.WriteLine("PhoneVersionMessage musicControlEnabled=" + musicControlEnabled.ToString());
            if (musicControlEnabled)
            {
                this._remoteCaps = RemoteCapsMusicControl;
            }
            else
            {
                this._remoteCaps = RemoteCapsNormal;
            }
        }

        protected override void GetContentFromMessage(List<byte> payload)
        {

        }

        protected override void AddContentToMessage(List<byte> payload)
        {
            byte[] prefix = { 0x01, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] session = BitConverter.GetBytes(PhoneSessionCaps);
            byte[] remote = BitConverter.GetBytes(this._remoteCaps);
            byte[] version = new byte[] {0x00,0x09,0x06,0x02 };
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(session);
                Array.Reverse(remote);
                Array.Reverse(version);
            }

            byte[] msg = new byte[0];
            msg = msg.Concat(prefix).Concat(session).Concat(remote).Concat(version).ToArray();
            System.Diagnostics.Debug.WriteLine("LOBACCCCIOOOOO -> " + msg.ToString());
            payload.AddRange(msg);
        }
    }
}
