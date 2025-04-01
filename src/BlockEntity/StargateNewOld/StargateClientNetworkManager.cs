using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace AstriaPorta.Content
{
	public class StargateClientNetworkManager : StargateNetworkManager
	{
		protected override ICoreClientAPI Api
		{
			get { return (ICoreClientAPI)api; }
		}

		public StargateClientNetworkManager(BEStargate gate, ICoreClientAPI capi) : base(gate, capi)
		{

		}

		public override void SyncState()
		{
			throw new NotImplementedException();
		}

		public void DialServer(IStargateAddress address)
		{
			GateStatePacket packet = new GateStatePacket
			{
				RemoteAddressBits = address.AddressBits
			};

			Api.Network.SendBlockEntityPacket(gate.Pos, (int)EnumStargatePacketType.Dial, packet);
		}

		public override void ProcessPacket(IPlayer player, int packetid, byte[] data)
		{
			switch ((EnumStargatePacketType)packetid)
			{
				case EnumStargatePacketType.State:
					{
						// Update gate state and renderer
						ProcessStatePacket(SerializerUtil.Deserialize<GateStatePacket>(data));
						break;
					}
				case EnumStargatePacketType.PlayerYaw:
					{
						// Rotate player
						ProcessYawPacket(SerializerUtil.Deserialize<PlayerYawPacket>(data));
						break;
					}
			}
		}

		public void ProcessStatePacket(GateStatePacket packet)
		{
			EnumStargateState newState = (EnumStargateState)packet.State;
			
		}

		/// <summary>
		/// Processes a player yaw packet, meaning that a player was teleported
		/// to this gate's location.<br/>
		/// Also updates the renderstate, since it is likely that the receiving
		/// gate was not rendered by the player before this.
		/// </summary>
		/// <param name="packet"></param>
		public void ProcessYawPacket(PlayerYawPacket packet)
		{
			gate.RenderManager.ActivateHorizon();
			gate.RenderManager.UpdateChevronGlow(gate.DialingManager.activeChevrons, gate.DialingAddress.AddressLength);
			gate.RenderManager.UpdateRendererState();

			RotateClientPlayer(packet.EntityId, packet.Yaw);
		}

		internal void RotateClientPlayer(long playerId, float yaw)
		{
			Entity ent = Api.World.GetEntityById(playerId);
			if (ent == null || !(ent is EntityPlayer)) return;

			EntityPlayer ep = (EntityPlayer)ent;
			((IClientPlayer)ep.Player).CameraYaw = yaw;
		}

		protected override GateStatePacket AssembleStatePacket()
		{
			return new GateStatePacket
			{
				ActiveChevrons = gate.DialingManager.activeChevrons,
				CurrentAngle = gate.DialingManager.currentAngle,
				CurrentGlyph = gate.DialingManager.currentGlyph,
				CurrentGlyphIndex = gate.DialingManager.currentAddressIndex,
				RemoteAddressBits = gate.DialingAddress?.AddressBits ?? 0,
				RotateCW = gate.DialingManager.rotateCW,
				State = (int)gate.StargateState
			};
		}
	}
}
