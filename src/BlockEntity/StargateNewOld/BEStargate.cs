using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using AstriaPorta.Util;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AstriaPorta.Content
{
    public abstract partial class BEStargate : BlockEntity, IStargate
	{
		protected StargateDialingManager dialingManager;
		protected StargateNetworkManager networkManager;
		protected StargateStateManager stateManager;
		protected StargateRenderManager renderManager;

		private StargateAddress addressShort = new StargateAddress(EnumAddressLength.Short);
		private StargateAddress addressMedium = new StargateAddress(EnumAddressLength.Medium);
		private StargateAddress addressLong = new StargateAddress(EnumAddressLength.Long);

		private EnumStargateType stargateType = EnumStargateType.Milkyway;
		private EnumStargateState stargateState = EnumStargateState.Idle;

		internal BlockPos remoteGatePosition;
		internal float remoteRotation;
		internal BlockPos dhdPosition;

		private StargateAddress dialingAddress = new StargateAddress();

		private bool registered = false;
		private bool isForceLoaded = false;

		public IStargateAddress AddressShort
		{
			get { return addressShort; }
		}

		public IStargateAddress AddressMedium
		{
			get { return addressMedium; }
		}

		public IStargateAddress AddressLong
		{
			get { return addressLong; }
		}

		public EnumStargateType StargateType
		{
			get { return stargateType; }
		}

		public EnumStargateState StargateState
		{
			get { return stargateState; }
		}

		public IStargateAddress DialingAddress
		{
			get { return dialingAddress; }
			set
			{
				dialingAddress = (StargateAddress)value;
			}
		}

		public BlockEntityAnimationUtil animUtil
		{
			get { return GetBehavior<BEBehaviorAnimatable>().animUtil; }
		}

		internal StargateDialingManager DialingManager
		{
			get { return dialingManager; }
		}

		internal StargateNetworkManager NetworkManager
		{
			get { return networkManager; }
		}

		internal StargateStateManager StateManager
		{
			get { return stateManager; }
		}

		internal StargateRenderManager RenderManager
		{
			get { return renderManager; }
		}

		/// <summary>
		/// Attempts to dial a gate at the other end of the supplied address.
		/// Force loads the receiving gate chunk if it exists.
		/// Dials the address and fails when address is invalid or destination
		/// gate does not exist
		/// </summary>
		/// <remarks>
		/// Server side
		/// </remarks>
		/// <param name="address"></param>
		public void TryDial(IStargateAddress address)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				((StargateClientNetworkManager)networkManager).DialServer(address);
			}

			Api.Logger.Debug("Started dial to " + address);
			if (stargateState != EnumStargateState.Idle)
			{
				Api.Logger.Debug("Stargate not idle, aborting...");
				return;
			}

			dialingManager.remoteNotified = false;
			remoteGatePosition = null;
			dialingAddress = (StargateAddress)address;
			dialingManager.targetAddress = dialingAddress;

			WorldGateManager.GetInstance().LoadRemoteGate(dialingAddress, this);
			if (!isForceLoaded)
			{
				WorldGateManager.GetInstance().ForceLoadChunk(Pos);
				isForceLoaded = true;
			}

			stargateState = EnumStargateState.DialingOutgoing;
			dialingManager.currentAddressIndex = 0;
			dialingManager.activeChevrons = 0;
			dialingManager.rotateCW = true;
			dialingManager.nextGlyph = dialingAddress.AddressCoordinates.Glyphs[0];

			stateManager.remoteLoadTimeout = 5f;

			stateManager.StartTicking();
		}

		public void TryAbort()
		{
			if (Api.Side == EnumAppSide.Client)
			{
				
			}
		}

		protected void SucceedConnection()
		{
			
		}

		/// <summary>
		/// Handles an incoming player traveler.<br/>
		/// Synchronizes necessary date to client
		/// </summary>
		/// <param name="player"></param>
		/// <param name="yaw"></param>
		internal void ReceiveTraveler(EntityPlayer player, float yaw)
		{
			if (Api.Side == EnumAppSide.Client) return;

			((StargateServerNetworkManager)networkManager).SyncStateToClients();
			((StargateServerNetworkManager)networkManager).SendYawPacket(player.EntityId, yaw);
		}

		public void UpdateState(GateStatePacket newState)
		{
			stateManager.UpdateState(newState);
		}

		// called when:
		//		BE spawned
		//		BE loaded from chunk (fromTree called first)
		//
		// NOT called when:
		//		BE dropped by schematic placement
		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);

			addressShort.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
			addressMedium.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
			addressLong.FromCoordinates(Pos.X, Pos.Y, Pos.Z);

			if (api.Side == EnumAppSide.Client)
			{
				InitializeClientState((ICoreClientAPI)api);
			} else
			{
				InitializeServerState((ICoreServerAPI)api);
			}
		}

		protected void InitializeClientState(ICoreClientAPI capi)
		{
			dialingManager = new StargateClientDialingManager();
			networkManager = new StargateClientNetworkManager(this, capi);
			stateManager = new StargateClientStateManager(this, capi);
			renderManager = new MilkywayRenderManager(this, capi);

			renderManager.InitializeRenderer();
			renderManager.UpdateChevronGlow(dialingManager.activeChevrons, dialingAddress.AddressLength);


			if (stargateState != EnumStargateState.Idle)
			{
				renderManager.UpdateRendererState();
			}
		}

		protected void InitializeServerState(ICoreServerAPI sapi)
		{
			dialingManager = new StargateServerDialingManager();
			networkManager = new StargateServerNetworkManager(this, sapi);
			stateManager = new StargateServerStateManager(this, sapi);
			

			WorldGateManager gateManager = WorldGateManager.GetInstance();

			if (!registered)
			{
				gateManager.RegisterLoadedGate(this);
				registered = true;
			}

			if (stargateState != EnumStargateState.Idle)
			{
				if (!isForceLoaded)
				{
					gateManager.ForceLoadChunk(Pos);
					isForceLoaded = true;
				}
			}
		}

		/// <summary>
		/// Initializes and registers the gate main renderer, Should also call the
		/// event horizon renderer initializer
		/// </summary>
		/// <remarks>
		/// client side only
		/// </remarks>
		/// <param name="capi"></param>
		protected abstract void InitializeRenderer(ICoreClientAPI capi);

		protected void InitializeHorizonRenderer(ICoreClientAPI capi, Vec4f baseColor)
		{
			
		}
	}
}
