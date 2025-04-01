using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace AstriaPorta.Content
{
	public abstract class StargateStateManager
	{
		protected BEStargate gate;
		protected ICoreAPI api;
		protected long tickListenerId = -1;

		internal float remoteLoadTimeout;
		internal float timeOpen = 0f;
		internal float timeSinceUpdate = 0f;

		public StargateStateManager(BEStargate gate, ICoreAPI api)
		{
			this.gate = gate;
			this.api = api;
		}

		public abstract ICoreAPI Api { get; }

		public abstract void UpdateState(GateStatePacket newStatePacket);

		public abstract void TickState(float delta);

		public void StartTicking()
		{
			if (tickListenerId != -1) return;
			tickListenerId = gate.RegisterGameTickListener(this.TickState, 20, 0);
		}

		public void StopTicking()
		{
			if (tickListenerId == -1) return;
			gate.UnregisterGameTickListener(tickListenerId);
			tickListenerId = -1;
		}
	}
}
