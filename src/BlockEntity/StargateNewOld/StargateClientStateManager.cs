using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AstriaPorta.Content
{
	public class StargateClientStateManager : StargateStateManager
	{
		public override ICoreAPI Api
		{
			get { return (ICoreClientAPI)api; }
		}

		public StargateClientStateManager(BEStargate gate, ICoreClientAPI capi) : base(gate, capi) { }

		public override void UpdateState(GateStatePacket newStatePacket)
		{
			EnumStargateState newState = (EnumStargateState)newStatePacket.State;

			gate.DialingManager.activeChevrons = newStatePacket.ActiveChevrons;
			gate.DialingManager.currentAngle = newStatePacket.CurrentAngle;
			gate.DialingManager.currentGlyph = newStatePacket.CurrentGlyph;
			gate.DialingManager.rotateCW = newStatePacket.RotateCW;

			if (gate.StargateState == newState)
			{
				// No state change, update renderer only
				
				if (!gate.DialingManager.isRotating && newStatePacket.Rotating)
				{
					// resume the dialing sequence
					
				}

				return;
			}

			gate.DialingManager.isRotating = newStatePacket.Rotating;

			switch (newState)
			{
				case EnumStargateState.Idle:
					{
						// update renderer (won't tick when idle)
						
						
						break;
					}
				case EnumStargateState.DialingOutgoing:
					{
						// start dialing manager
						break;
					}
				case EnumStargateState.DialingIncoming:
					{
						// update renderer
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						// update renderer
						// play kawoosh sound
						break;
					}
				case EnumStargateState.ConnectedIncoming:
					{
						// update renderer
						// play kawoosh sound
						break;
					}
			}
		}

		public override void TickState(float delta)
		{
			throw new NotImplementedException();
		}
	}
}
