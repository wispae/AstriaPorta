using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content
{
	public class ServerDialingManager : StargateDialingManager
	{
		public override event EventHandler OnActivationFailed;
		public override event EventHandler OnActivationSuccess;

		public override void OnTargetGlyphReached()
		{
			gate.UnregisterGameTickListener(tickListenerId);
			tickListenerId = -1;

			if (gate.StargateState == EnumStargateState.Idle)
			{
				OnActivationFailure();
				return;
			}

			gate.RegisterDelayedCallback(OnGlyphActivationCompleted, 2000);
		}

		public override void OnDialingSequenceCompletion()
		{
			
		}

		public override void OnActivationFailure()
		{
			gate.StargateState = EnumStargateState.Idle;
			activeChevrons = 0;

			if (gate.isForceLoaded)
			{
				WorldGateManager.GetInstance().ReleaseChunk(gate.Pos);
				gate.isForceLoaded = false;
			}

			OnActivationFailed.Invoke(this, null);
		}

		public override void OnGlyphActivationCompleted(float delta)
		{
			activeChevrons++;

			if (currentAddressIndex == (targetAddress.AddressLengthNum - 1))
			{
				if (gate.remotePosition != null)
				{

				}
			}
		}
	}
}
