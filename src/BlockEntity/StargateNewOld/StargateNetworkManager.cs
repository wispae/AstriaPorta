using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace AstriaPorta.Content
{
	public abstract class StargateNetworkManager
	{
		protected ICoreAPI api;
		protected BEStargate gate;
		
		protected abstract ICoreAPI Api
		{
			get;
		}

		public StargateNetworkManager(BEStargate gate, ICoreAPI api)
		{
			this.gate = gate;
			this.api = api;
		}

		public abstract void SyncState();

		public abstract void ProcessPacket(IPlayer player, int packetid, byte[] data);

		protected abstract GateStatePacket AssembleStatePacket();
	}
}
