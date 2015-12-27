using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using P3bble.PCL;
using P3bble.Core.Constants;

namespace P3bble.Core.Messages
{
    internal class PhoneVersionMessage : P3bbleMessage
    {
        private const uint PhoneSessionCaps = (uint)SessionCaps.GammaRay;
        private const uint RemoteCapsMusicControl = (uint)(RemoteCaps.Telephony | RemoteCaps.Sms | RemoteCaps.Android | RemoteCaps.Gps);
        private const uint RemoteCapsNormal = (uint)(RemoteCaps.Telephony | RemoteCaps.Sms | RemoteCaps.Windows | RemoteCaps.Gps);

        private uint _remoteCaps;

        public PhoneVersionMessage(bool musicControlEnabled)
            : base(Endpoint.Version) //Pebble rejected Endpoint.PhoneVersion
        {
            ServiceLocator.Logger.WriteLine("PhoneVersionMessage musicControlEnabled=" + musicControlEnabled.ToString());
            if (musicControlEnabled)
            {
                this._remoteCaps = RemoteCapsMusicControl;
                //ServiceLocator.Logger.WriteLine("PhoneVersionMessage BTLE= Possibly Enabled");
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
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(session);
                Array.Reverse(remote);
            }

            byte[] msg = new byte[0];
            msg = msg.Concat(prefix).Concat(session).Concat(remote).ToArray();

            payload.AddRange(msg);
        }
    }
}
