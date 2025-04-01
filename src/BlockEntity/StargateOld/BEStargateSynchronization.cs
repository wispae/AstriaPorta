using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Server;
using AstriaPorta.Util;

namespace AstriaPorta.Content
{

    public abstract partial class BEStargate
	{
		/// <summary>
		/// Synchronizes server state to client
		/// </summary>
		/// <remarks>
		/// server side
		/// </remarks>
		private void SyncStateToClient()
		{
			GateStatePacket packet = new GateStatePacket
			{
				ActiveChevrons = activeChevrons,
				CurrentAngle = currentAngle,
				CurrentGlyph = currentGlyph,
				CurrentGlyphIndex = currentAddressIndex,
				RemoteAddressBits = DialingAddress?.AddressBits ?? 0,
				RotateCW = rotateCW,
				State = (int)StargateState
			};

			((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1, packet);
		}

		/// <summary>
		/// Synchronizes server state only to specified client
		/// </summary>
		/// <remarks>
		/// server side
		/// </remarks>
		private void SyncStateToPlayer(IPlayer player)
		{
			if (player.ClientId == 0) return;

			ICoreServerAPI sapi = (ICoreServerAPI)Api;
			IServerPlayer splayer = sapi.Server.Players.Where((sp) => sp.ClientId == player.ClientId).FirstOrDefault(defaultValue: null);

			if (splayer == null) return;
			if (splayer.ConnectionState != EnumClientState.Connected) return;

			GateStatePacket packet = new GateStatePacket
			{
				ActiveChevrons = activeChevrons,
				CurrentAngle = currentAngle,
				CurrentGlyph = currentGlyph,
				CurrentGlyphIndex = currentAddressIndex,
				RemoteAddressBits = DialingAddress?.AddressBits ?? 0,
				RotateCW = rotateCW,
				State = (int)StargateState
			};

			sapi.Network.SendBlockEntityPacket(splayer, Pos, 1, packet);
		}

		/// <summary>
		/// Decodes server packet and redirects to the
		/// correct handler
		/// </summary>
		/// <remarks>
		/// client side
		/// </remarks>
		/// <param name="packetid"></param>
		/// <param name="data"></param>
		public override void OnReceivedServerPacket(int packetid, byte[] data)
		{
			// base.OnReceivedServerPacket(packetid, data);

			Api.Logger.Debug("Received server packet, synchronizing animation...");

			switch (packetid)
			{
				case 0:
					{
						GateAnimSyncPacket syncData = SerializerUtil.Deserialize<GateAnimSyncPacket>(data);
						ReceivedAnimationPacket(syncData);
						break;
					}
				case 1:
					{
						GateStatePacket syncData = SerializerUtil.Deserialize<GateStatePacket>(data);
						ReceivedStatePacket(syncData);
						break;
					}
				case 2:
					{
						PlayerYawPacket syncData = SerializerUtil.Deserialize<PlayerYawPacket>(data);
						RotateClientPlayer(syncData);
						break;
					}
			}
		}

		/// <summary>
		/// Handles package received from client stargates
		/// </summary>
		/// <param name="fromPlayer"></param>
		/// <param name="packetid"></param>
		/// <param name="data"></param>
		public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
		{
			// base.OnReceivedClientPacket(fromPlayer, packetid, data);

			switch (packetid)
			{
				case 0:
					{
						SyncStateToPlayer(fromPlayer);
						break;
					}
				case 1:
					{
						GateStatePacket syncData = SerializerUtil.Deserialize<GateStatePacket>(data);
						ReceivedGateDialPacket(syncData);
						break;
					}
				case 2:
					{
						ConnectionAborted(true);
						break;
					}
			}
		}

		/// <summary>
		/// Rotates the local players camera yaw.
		/// Also requests a state update from server
		/// </summary>
		/// <remarks>
		/// client side
		/// </remarks>
		/// <param name="packet"></param>
		private void RotateClientPlayer(PlayerYawPacket packet)
		{
			if (eventHorizonRenderer != null && !horizonRegistered)
			{
				ActivateHorizon();
				UpdateChevronGlow();
				UpdateRendererState();
			}

			Entity ent = Api.World.GetEntityById(packet.EntityId);
			if (ent == null || !(ent is EntityPlayer)) return;

			Api.Logger.Debug("Rotating player on client!");

			EntityPlayer ep = (EntityPlayer)ent;
			((IClientPlayer)ep.Player).CameraYaw = packet.Yaw;
		}

		/// <summary>
		/// Evaluates a received state packet and updates the state
		/// of the gate accordingly
		/// </summary>
		/// <remarks>
		/// client side
		/// </remarks>
		/// <param name="packet"></param>
		private void ReceivedStatePacket(GateStatePacket packet)
		{
			// update chevrons, angle, state, renderer
			EnumStargateState newState = (EnumStargateState)packet.State;
			dialingAddress = new StargateAddress();
			dialingAddress.FromBits(packet.RemoteAddressBits);
			// Api.Logger.Debug("Decoded address " + dialingAddress + " from packet bits " + packet.RemoteAddressBits);
			activeChevrons = packet.ActiveChevrons;
			currentGlyph = packet.CurrentGlyph;
			currentAddressIndex = packet.CurrentGlyphIndex;
			rotateCW = packet.RotateCW;
			activeChevrons = packet.ActiveChevrons;
			// Api.Logger.Debug("Current angle: " + packet.CurrentAngle);
			currentAngle = packet.CurrentAngle;

			// Wat doet dit? TargetGlyph bestaat niet meer na refactor? Where it go?  H E L P   M E
			TargetGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];

			// state transitions
			switch (newState)
			{
				case EnumStargateState.Idle:
					{
						if (stargateState != EnumStargateState.Idle)
						{
							if (stargateState == EnumStargateState.DialingIncoming)
							{
								// connection failure, play failure sound and deactivate all chevrons
							}
							else if (StargateState == EnumStargateState.ConnectedIncoming || StargateState == EnumStargateState.ConnectedOutgoing)
							{

							}

							DeactivateHorizon();
							UpdateChevronGlow();

							if (tickListenerId != -1)
							{
								UnregisterGameTickListener(tickListenerId);
								tickListenerId = -1;
							}
						}
						break;
					}
				case EnumStargateState.DialingOutgoing:
					{
						if (stargateState == EnumStargateState.Idle)
						{
							// start dialing outgoing
							// register OnTickClient if empty
						}
						// always register ticklistener, as it needs to be restarted when the server
						// signals that the glyph activation is completed
						if (tickListenerId == -1) tickListenerId = RegisterGameTickListener(OnTickClient, 20, 0);
						// play sound if not already playing
						break;
					}
				case EnumStargateState.DialingIncoming:
					{
						// 
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						if (stargateState != EnumStargateState.ConnectedOutgoing)
						{
							// play activation sound
							// register OnTickClient if empty
							if (tickListenerId == -1) tickListenerId = RegisterGameTickListener(OnTickClient, 20, 0);
							ActivateHorizon();
						}
						break;
					}
				case EnumStargateState.ConnectedIncoming:
					{
						if (stargateState != EnumStargateState.ConnectedIncoming)
						{
							// play activation sound
							// register OnTickClient if empty -> no!, only on server
							ActivateHorizon();
						}
						break;
					}
			}

			stargateState = newState;

			UpdateChevronGlow();
			UpdateRendererState();
		}

		[Obsolete]
		private void ReceivedAnimationPacket(GateAnimSyncPacket packet)
		{
			currentGlyph = packet.CurrentGlyph;
			currentAddressIndex = packet.NextGlyphIndex;
			rotateCW = packet.RotateCW;
			activeChevrons = packet.ActiveChevrons;
			EnumStargateState newState = (EnumStargateState)packet.GateState;
			if (stargateState != newState && (newState == EnumStargateState.ConnectedIncoming || newState == EnumStargateState.ConnectedOutgoing))
			{
				ActivateHorizon();
				UpdateRendererState();
				UpdateChevronGlow();
			}
			stargateState = newState;

			currentAngle = (GlyphLength - currentGlyph) * glyphAngle;

			awaitingActivation = false;

			UpdateChevronGlow();
			UpdateRendererState();
		}

		/// <summary>
		/// Handles a client request to start the server-side dialing procedure
		/// </summary>
		/// <param name="packet"></param>
		private void ReceivedGateDialPacket(GateStatePacket packet)
		{
			StargateAddress address = new StargateAddress();
			address.FromBits(packet.RemoteAddressBits);

			Dial(address);
		}
	}
}
