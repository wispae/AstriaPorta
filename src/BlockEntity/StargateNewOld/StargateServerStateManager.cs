using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AstriaPorta.Content
{
	public class StargateServerStateManager : StargateStateManager
	{
		public StargateServerStateManager(BEStargate gate, ICoreServerAPI api) : base(gate, api) { }

		public override ICoreServerAPI Api
		{
			get { return  (ICoreServerAPI)api; }
		}

		public override void UpdateState(GateStatePacket newStatePacket)
		{
			
		}

		public override void TickState(float delta)
		{
			switch (gate.StargateState)
			{
				case EnumStargateState.DialingOutgoing:
					{
						gate.DialingManager.NextAngle(delta);
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						if (gate.remoteGatePosition == null)
						{
							remoteLoadTimeout -= delta;

							if (remoteLoadTimeout < 0) gate.TryAbort();
							break;
						}

						break;
					}
			}
		}

		/// <summary>
		/// Checks for the presence of entities that collide with the
		/// event horizon.<br/>
		/// Teleports and rotates players<br/>
		/// Teleports, preserves and rotates momentum for other entities
		/// </summary>
		internal void CheckEntityCollisions()
		{
			// When player enters from front:
			//		teleport, change momentum and orientation to match output
			//
			// When player enters from back:
			//		kill player
			//
			// Same for other entities

			if (gate.remoteGatePosition == null) return;

			BlockPos startPos;
			BlockPos endPos;
			if (gate.Block.Shape.rotateY % 180 == 0)
			{
				startPos = gate.Pos.AddCopy(-1, 0.5f, 0);
				endPos = gate.Pos.AddCopy(2, 3, 1);
			} else
			{
				startPos = gate.Pos.AddCopy(0, 0.5f, -1);
				endPos = gate.Pos.AddCopy(1, 3, 2);
			}

			Entity[] travelers = Api.World.GetEntitiesInsideCuboid(startPos, endPos);

			float originYaw;
			float rotateRadLocal;
			float thetaf;
			float rotateLocal, rotateRemote;
			float motionX, motionY, motionZ;
			double offsetX, offsetY, offsetZ, offsetOriginX, offsetOriginZ;
			float costhetaf, sinthetaf;

			foreach (Entity traveler in travelers)
			{
				rotateLocal = gate.Block.Shape.rotateY;
				rotateRemote = gate.remoteRotation;

				rotateRadLocal = rotateLocal * GameMath.DEG2RAD;
				thetaf = ((rotateRemote - rotateLocal + 540) % 360) * GameMath.DEG2RAD;
				costhetaf = MathF.Cos(thetaf);
				sinthetaf = MathF.Sin(thetaf);
				originYaw = (traveler.SidedPos.Yaw + thetaf) % GameMath.TWOPI;

				motionX = (float)traveler.SidedPos.Motion.X * costhetaf + (float)traveler.SidedPos.Motion.Z * sinthetaf;
				motionZ = (float)traveler.SidedPos.Motion.Z * costhetaf - (float)traveler.SidedPos.Motion.X * sinthetaf;
				motionY = (float)traveler.SidedPos.Motion.Y;

				offsetY = traveler.Pos.Y - gate.Pos.Y;
				offsetOriginX = gate.Pos.X - traveler.Pos.X + 0.5f;
				offsetOriginZ = gate.Pos.Z - traveler.Pos.Z + 0.5f;

				// rotate offset vector
				offsetX = offsetOriginX * costhetaf + offsetOriginZ * sinthetaf;
				offsetZ = offsetOriginZ * costhetaf - offsetOriginX * sinthetaf;

				if (gate.Block.Shape.rotateY % 180 == 0)
				{
					if (gate.remoteRotation % 180 == 0)
					{
						// mirror around Z-axis
						offsetX *= -1;
					}
					else
					{
						// mirror around X-axis
						offsetZ *= -1;
					}
				}
				else
				{
					if (gate.remoteRotation % 180 == 0)
					{
						offsetX *= -1;
					}
					else
					{
						offsetZ *= -1;
					}
				}

				traveler.SidedPos.Yaw = originYaw;
				traveler.SidedPos.HeadYaw = originYaw;

				traveler.TeleportToDouble(gate.remoteGatePosition.X + offsetX + 0.5f, gate.remoteGatePosition.Y + offsetY, gate.remoteGatePosition.Z + offsetZ + 0.5f);

				if (traveler is EntityPlayer)
				{
					((EntityPlayer)traveler).BodyYawServer = originYaw;

					gate.RegisterDelayedCallback((t) =>
					{
						if (gate.remoteGatePosition == null) return;

						BEStargate remoteGate = api.World.BlockAccessor.GetBlockEntity<BEStargate>(gate.remoteGatePosition);
						if (remoteGate != null)
						{
							remoteGate.ReceiveTraveler((EntityPlayer)traveler, originYaw);
						}
					}, 20);
				} else
				{
					// teleport sets motion to 0, so add back the motion
					// this somehow doesn't work for the player
					traveler.SidedPos.Motion.Set(motionX, motionY, motionZ);
				}
			}
		}
	}
}
