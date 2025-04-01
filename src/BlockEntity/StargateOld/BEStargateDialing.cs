using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using AstriaPorta.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Client;

namespace AstriaPorta.Content
{
    public abstract partial class BEStargate
	{
		/// <summary>
		/// Attempts to dial a gate at the other end of the supplied address.
		/// Force loads the receiving gate chunk if it exists. Dials the address
		/// and fails when address is invalid or destination gate does not exist
		/// </summary>
		/// <remarks>
		/// server side
		/// </remarks>
		/// <param name="address"></param>
		public void Dial(StargateAddress address)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				DialServerGate(address);
				return;
			}

			Api.Logger.Debug("Started dial to " + address);
			if (stargateState != EnumStargateState.Idle)
			{
				Api.Logger.Debug("Stargate not idle, aborting...");
				return;
			}
			remoteNotified = false;
			remoteGate = null;

			dialingAddress = address;
			//BEStargate remoteGate = WorldGateManager.GetInstance().GetGateAt(address);
			WorldGateManager.GetInstance().LoadRemoteGate(address, this);
			if (!isForceLoaded)
			{
				WorldGateManager.GetInstance().ForceLoadChunk(Pos);
				isForceLoaded = true;
			}

			stargateState = EnumStargateState.DialingOutgoing;
			currentAddressIndex = 0;
			activeChevrons = 0;
			rotateCW = true;
			nextGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];
			remoteLoadTimeout = 5f;

			// Register server-side ticklistener
			if (tickListenerId != -1) UnregisterGameTickListener(tickListenerId);
			tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);
			SyncStateToClient();
			// Sync dialingstate to client
		}

		/// <summary>
		///		<para>
		///		On Server:
		///		<br/>
		///		
		///		</para>
		///		<para>
		///		On Client:
		///		<br/>
		///		</para>
		/// </summary>
		private void OnTargetChevronReached()
		{
			// on server:
			// wait for virtual animation to activate
			// run animation, sync state to client

			// on client:
			// run animation, set chevron glow
			
			// Api.Logger.Debug("Reached target chevron");


			UnregisterGameTickListener(tickListenerId);
			tickListenerId = -1;

			if (stargateState == EnumStargateState.Idle)
			{
				if (Api.Side == EnumAppSide.Server) OnActivationFailureServer(0);
				else OnActivationFailureClient(0);

				return;
			}

			awaitingChevronAnimation = true;

			if (Api.Side == EnumAppSide.Server)
			{
				Api.World.RegisterCallback(OnGlyphActivationCompleted, 2000);
				return;
			}
			else
			{
				Api.World.RegisterCallback(OnGlyphDownCompleted, 1000);
				renderer.chevronGlow[8] = 200;
			}

			AnimationMetaData meta = new AnimationMetaData()
			{
				Animation = "chevron_activate",
				Code = "chevron_activate",
				AnimationSpeed = 1,
				EaseInSpeed = 1,
				EaseOutSpeed = 1,
				Weight = 1
			};
			animUtil.StartAnimation(meta);
		}

		// client side
		private void OnGlyphDownCompleted(float delta)
		{
			// RegisterDelayedCallback(OnGlyphActivationCompleted, 1000);
			Api.Logger.Debug("Glyph down completed, awaiting server confirmation...");
			if (renderer != null)
			{
				renderer.chevronGlow[8] = 0;
			}
		}

		// server side
		private void OnGlyphActivationCompleted(float delta)
		{
			// on server:
			// update active chevrons
			// sync to remote gate
			// when all active
			//		activate remote gate
			//
			// sync state to client

			// on client:
			// await server (n/a)

			// Api.Logger.Debug("Glyph activation completed");

			awaitingChevronAnimation = false;
			activeChevrons++;

			if (currentAddressIndex == (dialingAddress.AddressLengthNum - 1))
			{
				if (remoteGate == null)
				{
					Api.Logger.Debug("Last chevron will not lock, aborting and notifying client");
					// chevron 7 will not lock!
					activeChevrons--;
					RegisterDelayedCallback(OnActivationFailureServer, 1000);
					// sync state to client
					SyncStateToClient();

					return;
				}

				Api.Logger.Debug("Last chevron locked!, activating and notifying client");
				// chevron 7 locked!
				OnConnectionSuccessServer();
				return;
			}

			if (remoteGate != null)
			{
				if (!remoteNotified)
				{
					remoteGate.EvaluateIncoming(this);
					remoteNotified = true;
				}
				remoteGate.SetActiveGlyphs(activeChevrons);
			}

			// chevron n encoded!
			currentAddressIndex++;
			// alternate rotation directions every time
			rotateCW = (currentAddressIndex == 0) ? true : !rotateCW;
			nextGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];
			
			// sync state to client
			SyncStateToClient();

			// re-register tick listener
			tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);
		}

		/// <summary>
		/// Attempts to register the provided DHD to this stargate
		/// </summary>
		/// <param name="dhd"></param>
		/// <returns>True when registration succeeds, else false</returns>
		public bool AttemptDhdRegistration(BEDialHomeDevice dhd)
		{
			if (controllingDhd != null) return false;

			controllingDhd = dhd;

			return true;
		}

		public void DialServerGate(StargateAddress address)
		{
			GateStatePacket packet = new GateStatePacket
			{
				RemoteAddressBits = address.AddressBits,
			};

			ICoreClientAPI capi = (ICoreClientAPI)Api;
			capi.Network.SendBlockEntityPacket(Pos, 1, packet);
		}
	}
}
