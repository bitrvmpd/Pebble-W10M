using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using P3bble.Core.Constants;
namespace P3bble.Core.Messages
{
    internal class ResetMessage : P3bbleMessage
    {
        public ResetMessage()
            : base(Endpoint.Reset)
        {
        }

        protected override void AddContentToMessage(List<byte> payload)
        {
            payload.Add(0x00);
        }

        protected override void GetContentFromMessage(List<byte> payload)
        {
        }
    }
}
