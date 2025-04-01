using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content
{
	public class StargateServerDialingManager : StargateDialingManager
	{
		public override event EventHandler OnActivationFailed;
		public override event EventHandler OnActivationSuccess;

		public override void OnTargetGlyphReached()
		{
			gate.UnregisterGameTickListener(tickListenerId);
			tickListenerId = -1;

			if (gate.StargateState == EnumStargateState.Idle)
			{
				OnActivationFailure(0);
				return;
			}

			gate.RegisterDelayedCallback(OnGlyphActivationCompleted, 2000);
		}

		public override void OnDialingSequenceCompletion()
		{
			
		}

		public override void OnActivationFailure(float delta)
		{
			// Activation failure
			// un-forceload gate
			// set state to idle
			// notify client of failure
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
					activeChevrons--;
					gate.RegisterDelayedCallback(OnActivationFailure, 1000);
				}
			}
		}
	}
}
