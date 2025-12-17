using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content
{
    public interface IStargateStateManager
    {
        public EnumStargateState State { get; }

        public void ProcessStatePacket(EnumStargatePacketType packetType, byte[] data);
    }
}
