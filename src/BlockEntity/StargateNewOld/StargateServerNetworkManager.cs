using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace AstriaPorta.Content
{
	public class StargateServerNetworkManager : StargateNetworkManager
	{

		protected override ICoreServerAPI Api
		{
			get { return (ICoreServerAPI)api; }
		}

		public StargateServerNetworkManager(BEStargate gate, ICoreServerAPI sapi) : base(gate, sapi)
		{

		}

		public override void SyncState()
		{
			GateStatePacket packet = AssembleStatePacket();

			Api.Network.BroadcastBlockEntityPacket(gate.Pos, 1, packet);
		}

		public void SyncStateToPlayer(IPlayer player)
		{
			if (player.ClientId == 0) return;

			IServerPlayer splayer = Api.Server.Players.Where((sp) => sp.ClientId == player.ClientId).FirstOrDefault(defaultValue: null);

			if (splayer == null) return;
			if (splayer.ConnectionState != EnumClientState.Connected) return;

			GateStatePacket packet = AssembleStatePacket();
			Api.Network.SendBlockEntityPacket(splayer, gate.Pos, 1, packet);
		}

		public void SyncStateToClients()
		{
			GateStatePacket packet = AssembleStatePacket();
			Api.Network.BroadcastBlockEntityPacket(gate.Pos, (int)EnumStargatePacketType.State, packet);

		}

		public void SendYawPacket(long entityId, float yaw)
		{
			PlayerYawPacket p = new PlayerYawPacket
			{
				EntityId = entityId,
				Yaw = yaw
			};

			Api.Network.BroadcastBlockEntityPacket(gate.remoteGatePosition, (int)EnumStargatePacketType.PlayerYaw, p);
		}

		/// <summary>
		/// Handles packages received from client stargates
		/// </summary>
		/// <param name="player"></param>
		/// <param name="packetid"></param>
		/// <param name="data"></param>
		public override void ProcessPacket(IPlayer player, int packetid, byte[] data)
		{
			switch ((EnumStargatePacketType)packetid)
			{
				case EnumStargatePacketType.Dial:
					{
						// Client wants gate to dial
						GateStatePacket packet = SerializerUtil.Deserialize<GateStatePacket>(data);
						StargateAddress address = new StargateAddress();
						address.FromBits(packet.RemoteAddressBits);
						gate.TryDial(address);
						break;
					}
				case EnumStargatePacketType.Abort:
					{
						// Client wants gate to abort
						break;
					}
			}
		}

		public void ReceiveDialRequest(GateStatePacket packet)
		{
			// Tell gate to try dialing
			// gate will state update clients later
		}

		public void ReceiveAbortRequest(GateStatePacket packet)
		{
			// Tell gate to stop dialing / close wormhole
			// gate will state update clients later
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
