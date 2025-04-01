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

namespace AstriaPorta.Content
{
    public abstract partial class BEStargate : BlockEntity, IStargate
	{
		// TODO: add medium and long address versions + register those to the manager
		private StargateAddress addressShort = new StargateAddress(EnumAddressLength.Short);
		private StargateAddress addressMedium = new StargateAddress(EnumAddressLength.Medium);
		private StargateAddress addressLong = new StargateAddress(EnumAddressLength.Long);

		private StargateAddress dialingAddress = new StargateAddress();


		private static EnumStargateType stargateType = EnumStargateType.Milkyway;
		private EnumStargateState stargateState = EnumStargateState.Idle;

		public BEStargate remoteGate = null;
		public BlockPos remotePosition = null;

		public BEDialHomeDevice controllingDhd = null;

		private float timeOpen = 0f;
		private float timeSinceUpdate = 0f;
		private float glyphAngle = 0f;
		private int energyBuffer = 0;
		private byte currentGlyph = 0;
		private byte nextGlyph = 0;
		private byte activeChevrons = 0;
		private bool rotateCW = true;
		private float previousAngle = 0f;
		private float currentAngle = 0f;
		private bool gateActive = false;
		private int currentAddressIndex = 0;
		private long tickListenerId = -1;
		// private readonly float degPerS = 20f;
		private readonly float degPerS = 40f;
		private bool awaitingActivation = false;
		private bool awaitingChevronAnimation = false;
		private bool fromTree = false;
		private bool registered = false;
		private bool remoteNotified = false;
		public bool isForceLoaded = false;
		private bool isUnloaded = true;
		private bool notifyUnloaded = false;
		private float remoteLoadTimeout = 10f;

		private readonly int creationEnergy = 100000;
		private readonly int energyPerMsPerKm = 1;

		private string gateTypeString;

		public IStargateAddress AddressShort
		{
			get { return addressShort; }
			set { addressShort = (StargateAddress)value; }
		}

		public IStargateAddress AddressMedium
		{
			get { return addressMedium; }
			set { addressMedium = (StargateAddress)value; }
		}

		public IStargateAddress AddressLong
		{
			get { return addressLong; }
			set { addressLong = (StargateAddress)value; }
		}

		public IStargateAddress DialingAddress
		{
			get { return dialingAddress; }
			set { dialingAddress = (StargateAddress)value; }
		}

		public EnumStargateType StargateType
		{
			get { return stargateType; }
		}

		public EnumStargateState StargateState
		{
			get { return stargateState; }
			set { stargateState = value; }
		}

		public (int X, int Y, int Z) GatePos
		{
			get { return (Pos.X, Pos.Y, Pos.Z); }
		}

		public float TimeOpen
		{
			get { return timeOpen; }
			set { timeOpen = value; }
		}

		public int EnergyBuffer
		{
			get { return energyBuffer; }
			set { energyBuffer = value; }
		}

		public int GlyphLength
		{
			get
			{
				switch (StargateType)
				{
					case EnumStargateType.Milkyway: return 39;
					case EnumStargateType.Pegasus: return 36;
					case EnumStargateType.Destiny: return 36;
					default: return 36;
				}
			}
		}

		public byte TargetGlyph
		{
			get { return nextGlyph; }
			set { nextGlyph = value; }
		}

		public BlockEntityAnimationUtil animUtil
		{
			get { return GetBehavior<BEBehaviorAnimatable>().animUtil; }
		}

		// Server side
		private void OnTickServer(float delta)
		{
			if (remoteGate != null && remoteGate.isUnloaded && !notifyUnloaded)
			{
				notifyUnloaded = true;
				Api.Logger.Debug("Remote got unloaded??? WTF?");
			}

			switch (stargateState)
			{
				case EnumStargateState.DialingOutgoing:
					{
						NextAngle(delta);
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						if (remoteGate == null)
						{
							remoteLoadTimeout -= delta;

							if (remoteLoadTimeout <= 0)
							{
								ConnectionAborted();
							}
							return;
						}
						CheckEntityCollisions();
						timeOpen += delta;
						timeSinceUpdate += delta;

						if (timeOpen > 60f)
						{
							ConnectionAborted();
						}
						break;
					}
				default:
					{
						UnregisterGameTickListener(tickListenerId);
						tickListenerId = -1;
						break;
					}
			}
		}

		// Client side
		public void OnTickClient(float delta)
		{
			switch (stargateState)
			{
				case EnumStargateState.DialingOutgoing:
					{
						NextAngle(delta);
						UpdateRendererState();
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						timeOpen += delta;
						if (timeOpen >= 60f)
						{
							timeOpen = 60f;
						}
						break;
					}
				default:
					{
						UnregisterGameTickListener(tickListenerId);
						tickListenerId = -1;
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
		private void CheckEntityCollisions()
		{
			// When player enters from front:
			//   teleport, change momentum and orientation to match output
			//
			// When player enters from the back:
			//   kill player
			//
			// Same for other entities

			// Api.Logger.Debug("Running entity collision check...");

			if (remoteGate == null) return;

			ICoreServerAPI sapi = ((ICoreServerAPI)Api);
			// ICoreClientAPI sapie = ((ICoreClientAPI)Api);

			BlockPos startPos;
			BlockPos endPos;
			if (Block.Shape.rotateY == 180 || Block.Shape.rotateY == 0)
			{
				startPos = Pos.AddCopy(-1, 0.5f, 0);
				endPos = Pos.AddCopy(2, 3, 1);
			} else
			{
				startPos = Pos.AddCopy(0, 0.5f, -1);
				endPos = Pos.AddCopy(1, 3, 2);
			}

			Entity[] travelers = Api.World.GetEntitiesInsideCuboid(startPos, endPos);

			float originYaw;
			float rotateRadLocal;
			float thetaf;
			float rotateLocal, rotateRemote;
			float motionX, motionY, motionZ;
			double offsetX, offsetY, offsetZ, offsetOriginX, offsetOriginZ;
			float costhetaf, sinthetaf;

			// Entity freshEntity;

			foreach (Entity traveler in travelers)
			{
				rotateLocal = Block.Shape.rotateY;
				// rotateRemote = (Block.Shape.rotateY + 180) % 360;
				rotateRemote = remoteGate.Block.Shape.rotateY;

				rotateRadLocal = rotateLocal * GameMath.DEG2RAD;
				thetaf = ((rotateRemote - rotateLocal + 540) % 360) * GameMath.DEG2RAD;
				costhetaf = MathF.Cos(thetaf);
				sinthetaf = MathF.Sin(thetaf);
				originYaw = (traveler.SidedPos.Yaw + thetaf) % GameMath.TWOPI;

				// rotate motion vector
				motionX = (float)traveler.SidedPos.Motion.X * costhetaf + (float)traveler.SidedPos.Motion.Z * sinthetaf;
				motionZ = (float)traveler.SidedPos.Motion.Z * costhetaf - (float)traveler.SidedPos.Motion.X * sinthetaf;
				motionY = (float)traveler.SidedPos.Motion.Y;

				offsetY = traveler.Pos.Y - Pos.Y;
				offsetOriginX = Pos.X - traveler.Pos.X + 0.5f;
				offsetOriginZ = Pos.Z - traveler.Pos.Z + 0.5f;

				// rotate offset vector
				offsetX = offsetOriginX * costhetaf + offsetOriginZ * sinthetaf;
				offsetZ = offsetOriginZ * costhetaf - offsetOriginX * sinthetaf;

				if (Block.Shape.rotateY == 0 || Block.Shape.rotateY == 180)
				{
					if (remoteGate.Block.Shape.rotateY == 0 || remoteGate.Block.Shape.rotateY == 180)
					{
						// mirror around Z-axis
						offsetX *= -1;
					} else
					{
						// mirror around X-axis
						offsetZ *= -1;
					}
				} else
				{
					if (remoteGate.Block.Shape.rotateY == 90 || remoteGate.Block.Shape.rotateY == 270)
					{
						offsetZ *= -1;
					} else
					{
						offsetX *= -1;
					}
				}

				traveler.SidedPos.Yaw = originYaw;
				traveler.SidedPos.HeadYaw = originYaw;

				traveler.TeleportToDouble(remoteGate.Pos.X + offsetX + 0.5f, remoteGate.Pos.Y + offsetY, remoteGate.Pos.Z + offsetZ + 0.5f);

				if (traveler is EntityPlayer)
				{
					((EntityPlayer)traveler).BodyYawServer = originYaw;
					PlayerYawPacket p = new PlayerYawPacket
					{
						EntityId = traveler.EntityId,
						Yaw = originYaw
					};
					RegisterDelayedCallback((t) =>
					{
						if (remoteGate == null) return;
						remoteGate.SyncStateToClient();
						sapi.Network.BroadcastBlockEntityPacket(remoteGate.Pos, 2, p);
					}, 20);
				} else
				{
					// teleport sets motion to 0, so add back the motion
					// this somehow doesn't work for the player
					traveler.SidedPos.Motion.Set(motionX, motionY, motionZ);
				}
			}
		}

		/// <summary>
		/// Calculates the next angle of the inner ring given
		/// a time since the last gametick in seconds
		/// </summary>
		/// <param name="delta"></param>
		private void NextAngle(float delta)
		{
			// Idea:
			//		CW: Normalize current glyph to pos 38
			//		CC: Normalize current glyph to pos 0
			byte targetGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];
			previousAngle = currentAngle;
			if (currentGlyph != targetGlyph)
			{
				currentAngle += delta * (rotateCW ? -degPerS : degPerS);
				if (currentAngle < 0) currentAngle += 360;
				else if (currentAngle >= 360) currentAngle -= 360;

				nextGlyph = (byte)(currentGlyph + (rotateCW ? -1 : 1));
				if (nextGlyph > 250) nextGlyph = (byte)(GlyphLength - 1);
				else if (nextGlyph >= GlyphLength) nextGlyph = 0;

				// Api.Logger.Debug("Current direction: " + ((rotateCW) ? "clockwise" : "counter-clockwise"));
				// Api.Logger.Debug($"Current glyph: {currentGlyph} | Next glyph: {nextGlyph} | Target glyph: {targetGlyph}");

				float nextAngle = (nextGlyph * glyphAngle + 360f) % 360f;
				float tPreviousAngle;
				float tCurrentAngle;
				float tNextAngle;

				if (rotateCW)
				{
					tPreviousAngle = 360f;
					tCurrentAngle = (currentAngle - previousAngle + 360f) % 360f;
					tNextAngle = (nextAngle - previousAngle + 360f) % 360f;
				}
				else
				{
					tPreviousAngle = 0f;
					tCurrentAngle = (currentAngle + (360f - previousAngle)) % 360f;
					tNextAngle = (nextAngle + (360f - previousAngle)) % 360f;
				}

				if ((tCurrentAngle >= tNextAngle && tNextAngle > tPreviousAngle) || (tCurrentAngle <= tNextAngle && tNextAngle < tPreviousAngle))
				{
					currentGlyph = nextGlyph;

					if (currentGlyph == targetGlyph)
					{
						currentAngle = nextGlyph * glyphAngle;
						OnTargetChevronReached();
					}
				}

				// previousDelta = currentDelta;
			}
			else
			{
				OnTargetChevronReached();
			}

			if (Api.Side == EnumAppSide.Client)
			{
				UpdateRendererState();
			}
		}

		// server side
		/// <summary>
		/// Activates gate and syncs to clients
		/// </summary>
		private void OnConnectionSuccessServer()
		{
			stargateState = EnumStargateState.ConnectedOutgoing;
			timeOpen = 0f;
			if (tickListenerId != -1) UnregisterGameTickListener(tickListenerId);
			tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);

			remoteGate.AcceptConnection(activeChevrons);

			// sync state to client
			SyncStateToClient();
		}

		// ===================
		// Remote Gate State
		// ===================

		/// <summary>
		/// Updates the number of active glyphs as dictated by
		/// the dialing gate
		/// </summary>
		/// <param name="activeChevrons">active chevron count</param>
		public void SetActiveGlyphs(byte activeChevrons)
		{

			this.activeChevrons = activeChevrons;
			SyncStateToClient();
		}

		/// <summary>
		/// Forces the gate to accept the connection of a dialing gate
		/// </summary>
		/// <param name="activeChevrons">active chevron count</param>
		private void AcceptConnection(byte activeChevrons)
		{
			stargateState = EnumStargateState.ConnectedIncoming;
			timeOpen = 0f;
			this.activeChevrons = activeChevrons;

			SyncStateToClient();
		}

		/// <summary>
		/// Evaluates the connection request of a dialing gate
		/// </summary>
		/// <param name="remoteGate">The dialing gate BE</param>
		public void EvaluateIncoming(BEStargate remoteGate)
		{
			if (stargateState != EnumStargateState.Idle) return;

			this.remoteGate = remoteGate;
			dialingAddress = remoteGate.dialingAddress;

			stargateState = EnumStargateState.DialingIncoming;
			if (!isForceLoaded)
			{
				WorldGateManager.GetInstance().ForceLoadChunk(Pos);
				isForceLoaded = true;
			}
			SyncStateToClient();
		}

		public void RemoteAborted()
		{
			// server side
			ConnectionAborted(false);
		}

		// ===================
		// Local Gate State
		// ===================

		/// <summary>
		/// When gate failes to activate due to whatever reason on the server
		/// </summary>
		/// <param name="delta"></param>
		private void OnActivationFailureServer(float delta)
		{
			stargateState = EnumStargateState.Idle;
			activeChevrons = 0;

			// TODO: notify remote gate if exists

			if (isForceLoaded)
			{
				WorldGateManager.GetInstance().ReleaseChunk(Pos);
				isForceLoaded = false;
			}

			// sync state to client
			SyncStateToClient();
		}

		/// <summary>
		/// Handles failure sequence on the client<br/>
		/// Mostly visuals and sound stuff
		/// </summary>
		/// <param name="delta"></param>
		private void OnActivationFailureClient(float delta)
		{
			stargateState = EnumStargateState.Idle;
			activeChevrons = 0;

			UpdateChevronGlow();
		}

		// server side
		private void ConnectionAborted(bool notifyRemote = true)
		{
			stargateState = EnumStargateState.Idle;
			activeChevrons = 0;

			if (notifyRemote && remoteGate != null)
			{
				remoteGate.RemoteAborted();
			}

			if (isForceLoaded)
			{
				WorldGateManager.GetInstance().ReleaseChunk(Pos);
				isForceLoaded = false;
			}
			if (tickListenerId != -1)
			{
				UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}

			SyncStateToClient();
		}

		// ===================
		// Block creation
		// ===================

		// called when:
		//   BE spawned
		//   BE loaded from chunk (but fromTree first)
		//
		// NOT called when:
		//   BE dropped by schematic placement
		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			// Api.Logger.Debug("Initializing...");

			addressShort.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
			addressMedium.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Medium);
			addressLong.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Long, Pos.dimension);
			remoteLoadTimeout = 5f;

			// TODO: load remote gate and update state if state is connected

			gateTypeString = Block.Variant["gatetype"] ?? "milkyway";
			glyphAngle = 360f / GlyphLength;

			api.Logger.Debug("Initializing address " + addressShort + " with bits " + addressShort.AddressBits);
			isUnloaded = false;

			if (api.Side == EnumAppSide.Client)
			{
				InitializeRenderer(api);
				UpdateChevronGlow();
				UpdateRendererState();
				// Leave state restoration up to the server,
				// it will sync when necessary

				/*if (stargateState != EnumStargateState.Idle && tickListenerId != -1)
				{
					tickListenerId = RegisterGameTickListener(OnTickClient, 20, 0);
				}*/
			}
			else
			{
				WorldGateManager gmInstance = WorldGateManager.GetInstance();

				if (!registered)
				{
					gmInstance.RegisterLoadedGate(this);
					registered = true;
				}

				if (stargateState == EnumStargateState.ConnectedOutgoing && tickListenerId != -1)
				{
					if (remoteGate == null || remoteGate.isUnloaded == true)
					{
						gmInstance.LoadRemoteGate(dialingAddress, this);
					}
					tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);
				}
				if (stargateState != EnumStargateState.Idle)
				{
					if (!isForceLoaded)
					{
						gmInstance.ForceLoadChunk(Pos);
						isForceLoaded = true;
					}
				}

				SyncStateToClient();
			}
			// MarkDirty();
		}

		public override void OnBlockPlaced(ItemStack byItemStack = null)
		{
			base.OnBlockPlaced(byItemStack);
			Api.Logger.Debug("Normal placement");

			if (Api.Side == EnumAppSide.Server)
			{
				addressShort.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
				addressMedium.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Medium);
				addressLong.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Long, 0);
				// Api.Logger.Debug("Creating address " + addressShort + " with bits " + addressShort.AddressBits);
				WorldGateManager gateManager = WorldGateManager.GetInstance();
				if (!registered)
				{
					gateManager.RegisterLoadedGate(this);
					registered = true;
				}
				MarkDirty();
			}
		}

		public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
		{
			base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);

			api.Logger.Debug("Placement by schematic");

			gateTypeString = Block.Variant["gatetype"] ?? "milkyway";
			glyphAngle = 360f / GlyphLength;

			// address.FromCoordinates(Pos.X, Pos.Z, 0, 0, api.WorldManager.MapSizeX, api.WorldManager.MapSizeZ);

			addressShort.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
			addressMedium.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Medium);
			addressLong.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Long, 0);
			if (!registered)
			{
				WorldGateManager.GetInstance().RegisterLoadedGate(this);
				registered = true;
			}
			MarkDirty();
		}

		/*public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos)
		{
			base.OnPlacementBySchematic(api, blockAccessor, pos);
			api.Logger.Debug("Placement by schematic");

			gateTypeString = Block.Variant["gatetype"] ?? "milkyway";
			glyphAngle = 360f / GlyphLength;

			// address.FromCoordinates(Pos.X, Pos.Z, 0, 0, api.WorldManager.MapSizeX, api.WorldManager.MapSizeZ);

			addressShort.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
			addressMedium.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Medium);
			addressLong.FromCoordinates(Pos.X, Pos.Y, Pos.Z, EnumAddressLength.Long, 0);
			if (!registered)
			{
				WorldGateManager.GetInstance().RegisterLoadedGate(this);
				registered = true;
			}
			MarkDirty();
		}*/

		// ===================
		// Block removal
		// ===================

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			if (Api != null) Api.Logger.Debug("Unloading gate with address " + addressShort);

			if (registered)
			{
				WorldGateManager.GetInstance().UnregisterLoadedGate(this);
				registered = false;
			}

			if (isForceLoaded)
			{
				WorldGateManager.GetInstance().ReleaseChunk(Pos);
				isForceLoaded = false;
			}

			if (tickListenerId != -1)
			{
				UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}

			isUnloaded = true;

			DisposeRenderers();
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();

			DestroyGate();
		}

		public override void OnBlockBroken(IPlayer byPlayer = null)
		{
			base.OnBlockBroken(byPlayer);

			DestroyGate();
		}

		private void DestroyGate()
		{
			if (registered)
			{
				WorldGateManager.GetInstance().UnregisterLoadedGate(this);
				registered = false;
			}

			if (isForceLoaded)
			{
				WorldGateManager.GetInstance().ReleaseChunk(Pos);
				isForceLoaded = false;
			}

			if (remoteGate != null) remoteGate.ConnectionAborted();

			if (tickListenerId != -1)
			{
				UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}

			DisposeRenderers();
		}

		// ===================
		// Tree Attributes
		// ===================

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			// if (Api != null) Api.Logger.Debug("From tree attributes yo!");

			base.FromTreeAttributes(tree, worldAccessForResolve);

			/*addressShort.FromTreeAttributes(tree);
			addressMedium.FromTreeAttributes(tree);
			addressLong.FromTreeAttributes(tree);*/
			dialingAddress.FromTreeAttributes(tree);

			timeOpen = tree.GetFloat("timeOpen");
			stargateState = (EnumStargateState)tree.GetInt("gateState");
			rotateCW = tree.GetBool("rotateCW");
			currentGlyph = (byte)tree.GetInt("currentGlyph");
			nextGlyph = (byte)tree.GetInt("nextGlyph");
			currentAddressIndex = tree.GetInt("currentAddressIndex");
			currentAngle = tree.GetFloat("currentAngle");
			activeChevrons = (byte)tree.GetInt("activeChevrons");

			if (Api != null && Api.Side == EnumAppSide.Client)
			{
				UpdateRendererState();
				UpdateChevronGlow();
			}

			// if (Api != null) Api.Logger.Debug("Short address from attributes: " + addressShort.ToString());
			if (Api != null) Api.Logger.Debug("Restoring " + addressShort + " from tree");

			fromTree = true;
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);

			// if (Api != null) Api.Logger.Debug("Short address to attributes: " + addressShort.ToString());
			if (Api != null) Api.Logger.Debug("Saving " + addressShort + " to attributes");

			/*addressShort.ToTreeAttributes(tree);
			addressMedium.ToTreeAttributes(tree);
			addressLong.ToTreeAttributes(tree);*/
			// if (dialingAddress.AddressCoordinates.Glyphs.Length != 0) dialingAddress.ToTreeAttributes(tree);
			dialingAddress.ToTreeAttributes(tree);

			tree.SetFloat("timeOpen", timeOpen);
			tree.SetInt("gateState", (int)stargateState);
			tree.SetBool("rotateCW", rotateCW);
			tree.SetInt("currentGlyph", currentGlyph);
			tree.SetInt("nextGlyph", nextGlyph);
			tree.SetInt("currentAddressIndex", currentAddressIndex);
			tree.SetFloat("currentAngle", currentAngle);
			tree.SetInt("activeChevrons", activeChevrons);
		}

		// ===================
		// Utility
		// ===================

		// extra inherited methods
		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			base.GetBlockInfo(forPlayer, dsc);

			dsc.AppendLine("Address: " + addressShort.ToString());

			switch (stargateState)
			{
				case EnumStargateState.DialingOutgoing:
					{
						dsc.AppendLine("Dialing " + dialingAddress);
						break;
					}
				case EnumStargateState.DialingIncoming:
					{
						dsc.AppendLine("Being dialed by remote gate");
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						dsc.AppendLine("Going to " + dialingAddress);
						dsc.AppendLine("Time until deactivation: " + (60f - timeOpen) + " s");
						break;
					}
				case EnumStargateState.ConnectedIncoming:
					{
						dsc.AppendLine("Receiving from " + dialingAddress);
						break;
					}
			}

			dsc.AppendLine("orientation: " + Block.Shape.rotateY);
		}

		// Mesh creation

		protected MeshData GenChevronMesh()
		{
			// MeshData baseMesh;
			ICoreClientAPI capi = (ICoreClientAPI)Api;
			ITesselatorAPI mesher = capi.Tesselator;
			AssetLocation chevronLight = new AssetLocation("astriaporta", $"shapes/gates/{gateTypeString}_chevron.json");

			MeshData baseMesh = ObjectCacheUtil.GetOrCreate(capi, "milkyway-chevron-mesh", () =>
			{
				MeshData mesh;
				Shape chevron = Shape.TryGet(capi, chevronLight);
				mesher.TesselateShape(Block, chevron, out mesh);

				return mesh;
			});

			return baseMesh;
		}

		protected MeshData GenRingMesh()
		{
			MeshData baseMesh;
			ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

			AssetLocation ring = new AssetLocation("astriaporta", $"shapes/gates/{gateTypeString}_ring.json");
			mesher.TesselateShape(Block, Shape.TryGet(Api, ring), out baseMesh);

			return baseMesh;
		}

		private MeshData GenRoundHorizonMesh(float radius, int angles, int sectors, float offsetx, float offsety)
		{
			MeshData baseMesh;
			// caching ftw
			baseMesh = ObjectCacheUtil.TryGet<MeshData>(Api, "astriaporta-eventhorizon");
			if (baseMesh != null) return baseMesh;

			// Precalculate required vertex / index counts
			int vertexCount = angles * sectors + 1;
			int indexCount = 6 * angles * (sectors - 1) + 3 * angles;

			// Set the "withNormals" flag to false when using default shaders!
			baseMesh = new MeshData(vertexCount, indexCount, true, true, true, true);

			int color = BitConverter.ToInt32(new byte[] { 0, 0, 0, 255 });
			float x, y, z, uvx, uvy;
			float angle = 2 * MathF.PI / angles;
			z = 0.5f;

			// Generate vertices
			for (int i = 0; i < sectors; i++)
			{
				for (int j = 0; j < angles; j++)
				{
					// x E [-radius, radius]
					// y E [-radius, radius]
					// 
					//
					x = MathF.Cos(angle * j) * (radius / (float)(sectors)) * (float)(sectors - i);
					y = MathF.Sin(angle * j) * (radius / (float)(sectors)) * (float)(sectors - i);
					uvx = (x + radius) / (2 * radius);
					uvy = 1f - ((y + radius) / (2 * radius));

					x += offsetx;
					y += offsety;

					baseMesh.AddVertexWithFlags(x, y, z, uvx, uvy, color, 0);

					// add normal ( ! SKIP WHEN USING DEFAULT SHADERS / RENDERERS ! )
					if (i == 0) baseMesh.AddNormal(0f, 0f, 0f);
					else baseMesh.AddNormal(1f, 1f, 1f);
				}
			}
			// Center vertex
			baseMesh.AddVertexWithFlags(offsetx, offsety, z, 0.5f, 0.5f, color, 0);
			// Skip normal when using defualt!
			baseMesh.AddNormal(1f, 1f, 1f);

			// indices for all ring sectors, except innermost sector
			for (int i = 0; i < (sectors - 1); i++)
			{
				for (int j = 0; j < (angles - 1); j++)
				{
					baseMesh.AddIndex(j + i * angles);
					baseMesh.AddIndex(j + (i + 1) * angles);
					baseMesh.AddIndex(j + (i + 1) * angles + 1);

					baseMesh.AddIndex(j + i * angles);
					baseMesh.AddIndex(j + i * angles + 1);
					baseMesh.AddIndex(j + (i + 1) * angles + 1);
				}

				// connect the last vertices of the sectors with the
				// first vertices of the sector
				baseMesh.AddIndex(angles - 1 + i * angles);
				baseMesh.AddIndex(angles - 1 + (i + 1) * angles);
				baseMesh.AddIndex((i + 1) * angles);

				baseMesh.AddIndex(i * angles);
				baseMesh.AddIndex((i + 1) * angles);
				baseMesh.AddIndex(angles - 1 + i * angles);
			}

			// innermost sector, with all triangles converging
			// onto the center vertex
			for (int i = 0; i < (angles - 1); i++)
			{
				baseMesh.AddIndex(i + (sectors - 1) * angles);
				baseMesh.AddIndex(i + (sectors - 1) * angles + 1);
				baseMesh.AddIndex(sectors * angles);
			}
			baseMesh.AddIndex(sectors * angles - 1);
			baseMesh.AddIndex((sectors - 1) * angles);
			baseMesh.AddIndex(sectors * angles);

			// Update UV coordinates to position in the texture atlas
			baseMesh.SetTexPos(((ICoreClientAPI)Api).ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonTexPos);

			baseMesh = ObjectCacheUtil.GetOrCreate(Api, "astriaporta-eventhorizon", () => { return baseMesh; });

			return baseMesh;
		}
	}
}
