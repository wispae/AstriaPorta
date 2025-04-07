using AstriaPorta.Config;
using AstriaPorta.src.Block;
using AstriaPorta.src.Systems;
using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace AstriaPorta.Content
{
	public class BlockEntityStargate : BlockEntity, IStargate, IBlockEntityInteractable
	{
		protected float ROT_DEG_PER_S = 80f;

		protected static Cuboidi vortexOffsetNorth;
		protected static Cuboidi vortexOffsetEast;
		protected static Cuboidi vortexOffsetSouth;
		protected static Cuboidi vortexOffsetWest;
		protected static Cuboidi irisOffsetNorthSouth;
		protected static Cuboidi irisOffsetEastWest;

		private StargateAddress gateAddress = new StargateAddress(EnumAddressLength.Short);

		private StargateAddress dialingAddress;

		private EnumStargateType stargateType;
		private EnumStargateState stargateState;

		protected BlockPos remotePosition;
		protected BlockEntityStargate remoteGate = null;
		protected bool remoteSought = false;

		protected bool lightAdded = false;
		protected LightiningPointLight horizonlight;
		//        LightningPointLight

		public StargateAddress GateAddress
		{
			get { return gateAddress; }
		}

		public StargateAddress DialingAddress
		{
			get { return dialingAddress; }
		}

		internal BlockPos RemotePosition
		{
			get { return remotePosition; }
			set { remotePosition = value; }
		}

		internal bool IsForceLoaded
		{
			get { return isForceLoaded; }
			set { isForceLoaded = value; }
		}

		public EnumStargateType StargateType
		{
			get { return stargateType; }
		}

		public EnumStargateState StargateState
		{
			get { return stargateState; }
		}

		public int GlyphLength
		{
			get
			{
				switch (stargateType)
				{
					case EnumStargateType.Milkyway: return 39;
					case EnumStargateType.Pegasus: return 36;
					case EnumStargateType.Destiny: return 36;
					default: return 36;
				}
			}
		}

		public bool CanBreak
		{
			get
			{
				return StargateState != EnumStargateState.ConnectedOutgoing && StargateState != EnumStargateState.ConnectedIncoming;
			}
		}

		public Cuboidi VortexArea
		{
			get
			{
				switch (Block.Shape.rotateY)
				{
					case 0f:
						return vortexOffsetNorth;
					case 90f:
						return vortexOffsetWest;
					case 180f:
						return vortexOffsetSouth;
					case 270f:
						return vortexOffsetEast;
					default:
						return null;
				}
			}
		}

		public Cuboidi IrisArea
		{
			get
			{
				if (Block.Shape.rotateY % 180 == 0)
				{
					return irisOffsetNorthSouth;
				}
				return irisOffsetEastWest;
			}
		}

		public BlockEntityStargate()
		{
			InitializeInventory();

			ROT_DEG_PER_S = StargateConfig.Loaded.DialSpeedDegreesPerSecondMilkyway;
		}

		/// <summary>
		/// Attempts to dial a gate at the other end of the supplied address.<br/>
		/// Force loads the receiving gate chunk if it exists.<br/>
		/// Dials the address and fails when address is invalid or destination gate does not exists
		/// </summary>
		/// <param name="address"></param>
		/// <param name="dialType"></param>
		public void TryDial(StargateAddress address, EnumDialSpeed dialType)
		{
			ROT_DEG_PER_S = StargateConfig.Loaded.DialSpeedDegreesPerSecondMilkyway;
			if (Api.Side == EnumAppSide.Client)
			{
				DialServerGate(address, dialType);
				return;
			}

#if DEBUG
			Api.Logger.Debug($"Started dial to {address} with coordinates ({address.AddressCoordinates.X},{address.AddressCoordinates.Y},{address.AddressCoordinates.Z})");
#endif

			if (stargateState != EnumStargateState.Idle)
			{
				Api.Logger.Debug("Stargate not idle, aborting...");
				return;
			}

			remoteGate = null;
			remotePosition = null;

			dialingAddress = address;

			if (WillDialSucceed(address))
			{
				StargateManagerSystem.GetInstance(Api).LoadRemoteGate(address, this);
				if (!isForceLoaded)
				{
					StargateManagerSystem.GetInstance(Api).ForceLoadChunk(Pos);
					isForceLoaded = true;
				}
			}

			stargateState = EnumStargateState.DialingOutgoing;
			currentAddressIndex = 0;
			activeChevrons = 0;
			rotateCW = true;
			nextGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];
			remoteLoadTimeout = StargateConfig.Loaded.MaxTimeoutSeconds;

			if (tickListenerId != -1) UnregisterGameTickListener(tickListenerId);
			tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);
			SyncStateToClients();
		}

		public void TryDisconnect()
		{
			// Reserved for later when disconnect may fail for some reason
			if (Api.Side == EnumAppSide.Client)
			{
				DisconnectServerGate();
			} else
			{
				ForceDisconnect();
			}
		}

		public bool WillDialSucceed(StargateAddress address)
		{
			if (address.AddressBits == GateAddress.AddressBits) return false;
			int distanceChunks = GateAddress.GetDistanceTo(address);
			if (distanceChunks < StargateConfig.Loaded.MinRangeChunksMilkyway) return false;
			if (distanceChunks > StargateConfig.Loaded.MaxRangeChunksMilkyway) return false;

			return true;
		}

		/// <summary>
		/// Send a message to the server side gate to initiate
		/// the closing of the gate
		/// </summary>
		private void DisconnectServerGate()
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, (int)EnumStargatePacketType.Abort, null);
		}

		public void ForceDisconnect(bool notifyRemote = true)
		{
			stargateState = EnumStargateState.Idle;
			activeChevrons = 0;
			stableConnection = false;
			timeOpen = 0f;

			if (notifyRemote && GetRemoteGate() != null)
			{
				remoteGate.ForceDisconnect(false);
			}

			if (isForceLoaded)
			{
				StargateManagerSystem.GetInstance(Api).ReleaseChunk(Pos);
				isForceLoaded = false;
			}

			if (tickListenerId != -1)
			{
				UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}

			SyncStateToClients();
		}

		#region Universal

		protected long tickListenerId = -1;

		protected bool awaitingChevronAnimation = false;
		protected byte activeChevrons = 0;

		protected string gateTypeString;

		protected bool isForceLoaded = false;
		protected bool registeredToGateManager = false;

		// called when:
		//   BE spawned
		//   BE loaded from chunk (but fromTree first)
		//
		// NOT called when:
		//   BE dropped by schematic placement
		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);

			gateAddress.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
			remoteLoadTimeout = StargateConfig.Loaded.MaxTimeoutSeconds;

			gateTypeString = Block.Variant["gatetype"] ?? "milkyway";
			glyphAngle = 360f / GlyphLength;

			if (gateTypeString == "milkyway") stargateType = EnumStargateType.Milkyway;

#if DEBUG
			api.Logger.Debug("Initializing address " + gateAddress + " with bits " + gateAddress.AddressBits + " for gate at " + Pos);
#endif

			inventory.Pos = Pos;
			inventory.LateInitialize("stargate-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

			initializeVortexOffsets();
			initializeIrisOffsets();

			if (api.Side == EnumAppSide.Client) InitializeClient((ICoreClientAPI)api);
			else InitializeServer((ICoreServerAPI)api);
		}

		public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
		{
			base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);

			gateTypeString = Block.Variant["gatetype"] ?? "milkyway";
			glyphAngle = 360f / GlyphLength;

			gateAddress.FromCoordinates(Pos.X, Pos.Y, Pos.Z);
			if (!registeredToGateManager)
			{
				StargateManagerSystem.GetInstance(api).RegisterLoadedGate(this);
				registeredToGateManager = true;
			}

			MarkDirty();
		}

		public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
		{
			base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);

			if (!resolveImports) return;

			inventory.Pos = Pos;
			foreach (ItemSlot slot in inventory)
			{
				if (slot.Itemstack != null)
				{
					if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForNewMappings))
					{
						slot.Itemstack = null;
					} 
					else
					{
						slot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForNewMappings, slot, oldBlockIdMapping, oldItemIdMapping, resolveImports);
					}
					ItemStack itemstack = slot.Itemstack;
					IResolvableCollectible resolvable = ((itemstack != null) ? itemstack.Collectible : null) as IResolvableCollectible;
					if (resolvable != null)
					{
						resolvable.Resolve(slot, worldForNewMappings, resolveImports);
					}
				}
			}
		}

		public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
		{
			base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
			
			foreach (ItemSlot slot in inventory)
			{
				ItemStack itemStack = slot.Itemstack;
				if (itemStack != null)
				{
					itemStack.Collectible.OnStoreCollectibleMappings(this.Api.World, slot, blockIdMapping, itemIdMapping);
				}
			}
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();

			if (registeredToGateManager)
			{
				StargateManagerSystem.GetInstance(Api).UnregisterLoadedGate(this);
				registeredToGateManager = false;
			}

			if (isForceLoaded)
			{
				// TODO: Add to worldgatemanager to release when gate unregisters itself
				StargateManagerSystem.GetInstance(Api).ReleaseChunk(Pos);
				isForceLoaded = false;
			}

			if (tickListenerId != -1)
			{
				UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}

			if (Api.Side == EnumAppSide.Client) {
				DisposeAllSounds();
				DisposeRenderers();
			}
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

		public bool IsIrisClear()
		{
			Cuboidi offsets = IrisArea;
			Block b;

			for (int x = offsets.MinX; x <= offsets.MaxX; x++)
			{
				for (int z = offsets.MinZ; z <= offsets.MaxZ; z++)
				{
					for (int y = offsets.MinY; y <= offsets.MaxY; y++)
					{
						b = Api.World.BlockAccessor.GetBlock(Pos.AddCopy(x, y, z));
						if (b.Id != 0 && b is not BlockMultiblockStargate)
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		protected void initializeVortexOffsets()
		{
			if (vortexOffsetNorth == null)
			{
				vortexOffsetNorth = new Cuboidi(-1, 1, 1, 1, 4, 3);
				vortexOffsetSouth = new Cuboidi(-1, 1, 1, 1, 4, -3);
				vortexOffsetEast = new Cuboidi(-1, 1, -1, -3, 4, 1);
				vortexOffsetWest = new Cuboidi(1, 1, -1, 3, 4, 1);
			}
		}

		protected void initializeIrisOffsets()
		{
			if (irisOffsetNorthSouth == null)
			{
				irisOffsetNorthSouth = new Cuboidi(-2, 1, 0, 2, 5, 0);
				irisOffsetEastWest = new Cuboidi(0, 1, -2, 0, 5, 2);
			}
		}

		private void DestroyGate()
		{
			if (registeredToGateManager)
			{
				StargateManagerSystem.GetInstance(Api).UnregisterLoadedGate(this);
				registeredToGateManager = false;
			}

			if (isForceLoaded)
			{
				StargateManagerSystem.GetInstance(Api).ReleaseChunk(Pos);
				isForceLoaded = false;
			}

			if (GetRemoteGate() != null && Api.Side == EnumAppSide.Server) remoteGate.ForceDisconnect();

			if (tickListenerId != -1)
			{
				UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}

			DisposeAllSounds();
			DisposeRenderers();

			if (gateDialog != null)
			{
				gateDialog.Dispose();
			}
		}

#nullable enable
		/// <summary>
		/// In order to not keep a reference to the remote gate at all times,
		/// this function retrieves and caches the reference<br/>
		/// It is the responsibility of the caller to set this reference
		/// back to null when you're done with it at the end of the tick
		/// </summary>
		/// <returns></returns>
		protected BlockEntityStargate? GetRemoteGate()
		{
			if (remotePosition != null)
			{
				if (remoteSought)
				{
					return remoteGate;
				} else
				{
					BlockEntity gate = Api.World.BlockAccessor.GetBlockEntity(remotePosition);
					if (gate != null && gate is BlockEntityStargate)
					{
						remoteGate = (BlockEntityStargate)gate;
					}

					remoteSought = true;
					return remoteGate;
				}
			}

			return null;
		}
#nullable disable

		#region Dialing
		protected float currentAngle = 0f;
		protected float previousAngle = 0f;
		protected float glyphAngle = 0f;

		protected bool rotateCW = true;

		protected int currentAddressIndex;
		protected byte currentGlyph;
		protected byte nextGlyph;

		EnumDialSpeed dialType = EnumDialSpeed.Slow;

		/// <summary>
		/// Calculates the next angle of the inner ring given
		/// a time since the last gametick in seconds
		/// </summary>
		/// <param name="delta"></param>
		/// <returns></returns>
		protected float NextAngle(float delta)
		{
			// CW: Normalize current glyph to pos 38
			// CC: Normalize current glyph to pos 0
			byte targetGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];
			previousAngle = currentAngle;

			if (currentGlyph != targetGlyph)
			{
				currentAngle += delta * (rotateCW ? -ROT_DEG_PER_S : ROT_DEG_PER_S);
				if (currentAngle < 0) currentAngle += 360;
				else if (currentAngle >= 360) currentAngle -= 360;

				nextGlyph = (byte)(currentGlyph + (rotateCW ? -1 : 1));
				if (nextGlyph > 250) nextGlyph = (byte)(GlyphLength - 1);
				else if (nextGlyph >= GlyphLength) nextGlyph = 0;

				float nextAngle = (nextGlyph * glyphAngle + 360f) % 360;
				float tPreviousAngle;
				float tCurrentAngle;
				float tNextAngle;

				if (rotateCW)
				{
					tPreviousAngle = 360f;
					tCurrentAngle = (currentAngle - previousAngle + 360f) % 360;
					tNextAngle = (nextAngle - previousAngle + 360) % 360;
				}
				else
				{
					tPreviousAngle = 0f;
					tCurrentAngle = (currentAngle + (360 - previousAngle)) % 360;
					tNextAngle = (nextAngle + (360 - previousAngle)) % 360;
				}

				if ((tCurrentAngle >= tNextAngle && tNextAngle > tPreviousAngle) || (tCurrentAngle <= tNextAngle && tNextAngle < tPreviousAngle))
				{
					currentGlyph = nextGlyph;

					if (currentGlyph == targetGlyph)
					{
						currentAngle = nextGlyph * glyphAngle;
						OnGlyphReached();
					}
				}
			}
			else
			{
				OnGlyphReached();
			}

			return currentAngle;
		}

		protected void OnGlyphReached()
		{
			UnregisterGameTickListener(tickListenerId);
			tickListenerId = -1;

			if (stargateState == EnumStargateState.Idle) return;

			awaitingChevronAnimation = true;

			if (Api.Side == EnumAppSide.Server)
			{
				OnGlyphReachedServer();
			} else
			{
				OnGlyphReachedClient();
			}
		}

		BlockEntityDialHomeDevice controllingDhd = null;
		/// <summary>
		/// Attempts to register the provided DHD to this stargate
		/// </summary>
		/// <param name="dhd"></param>
		/// <returns>True when registration succeeds, else false</returns>
		public bool AttemptDhdRegistration(BlockEntityDialHomeDevice dhd)
		{
			if (controllingDhd != null) return false;
			controllingDhd = dhd;

			return true;
		}

		#endregion

		#region Persistence
		protected bool fromTree = false;

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);

			if (dialingAddress != null)
			{
				dialingAddress.ToTreeAttributes(tree);
				tree.SetBool("hasAddress", true);
			} else
			{
				tree.SetBool("hasAddress", false);
			}

			tree.SetFloat("timeOpen", timeOpen);
			tree.SetFloat("travelerTimeout", 0f);
			tree.SetFloat("currentAngle", currentAngle);
			tree.SetInt("currentGlyph", currentGlyph);
			tree.SetInt("currentAddressIndex", currentAddressIndex);
			tree.SetInt("activeChevrons", activeChevrons);
			tree.SetInt("gateState", (int)stargateState);
			tree.SetInt("dialType", (int)dialType);
			tree.SetBool("rotateCW", rotateCW);
			if (remotePosition != null)
			{
				tree.SetBlockPos("remotePosition", remotePosition);
			}

			ITreeAttribute invtree = new TreeAttribute();
			inventory.ToTreeAttributes(invtree);
			tree["inventoryCamo"] = invtree;
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);

			Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventoryCamo"));
			if (Api != null)
			{
				Inventory.AfterBlocksLoaded(Api.World);
			}

			if (tree.GetBool("hasAddress", false))
			{
				dialingAddress = new StargateAddress();
				dialingAddress.FromTreeAttributes(tree);
			}

			timeOpen = tree.GetFloat("timeOpen", 0f);
			currentAngle = tree.GetFloat("currentAngle", 0f);
			currentGlyph = (byte)tree.GetInt("currentGlyph", 0);
			currentAddressIndex = (byte)tree.GetInt("currentAddressIndex", 0);
			activeChevrons = (byte)tree.GetInt("activeChevrons", 0);
			stargateState = (EnumStargateState)tree.GetInt("gateState", 0);
			dialType = (EnumDialSpeed)tree.GetInt("dialType", 0);
			rotateCW = tree.GetBool("rotateCW", false);
			if (tree.HasAttribute("remotePositionX"))
			{
				remotePosition = tree.GetBlockPos("remotePosition");
			}

			if (Api != null && Api.Side == EnumAppSide.Client)
			{
				RestoreClientStateFromTree();
			}

			fromTree = true;
		}

		protected void RestoreClientStateFromTree()
		{

		}

		#endregion

		#region Networking

		protected GateStatePacket AssembleStatePacket()
		{
			return new GateStatePacket
			{
				ActiveChevrons = activeChevrons,
				CurrentAngle = currentAngle,
				CurrentGlyph = currentGlyph,
				CurrentGlyphIndex = currentAddressIndex,
				RemoteAddressBits = DialingAddress?.AddressBits ?? 0,
				RotateCW = rotateCW,
				State = (int)StargateState,
				DialType = (int)dialType
			};
		}

		#endregion

		#region Inventory

		protected InventoryGeneric inventory;
		public InventoryBase Inventory => inventory;

		protected void InitializeInventory()
		{
			inventory = new InventoryGeneric(5, null, null, null);
			inventory.SlotModified += OnInventoryChanged;
		}

		protected void OnInventoryChanged(int slotId)
		{
			MarkDirty();
		}

		#endregion

		#endregion

		#region Serverside

		protected float remoteLoadTimeout = StargateConfig.Loaded.MaxTimeoutSeconds;
		protected float timeOpen = 0f;
		protected bool remoteNotified = false;
		protected bool willConnect = false;
		protected bool stableConnection = false;

		protected void InitializeServer(ICoreServerAPI sapi)
		{
			StargateManagerSystem gmInstance = StargateManagerSystem.GetInstance(sapi);

			if (!registeredToGateManager)
			{
				gmInstance.RegisterLoadedGate(this);
				registeredToGateManager = true;
			}

			if (StargateState == EnumStargateState.DialingOutgoing || StargateState == EnumStargateState.ConnectedIncoming)
			{
				if (!isForceLoaded)
				{
					gmInstance.ForceLoadChunk(Pos);
					isForceLoaded = true;
				}

				if (StargateState == EnumStargateState.DialingOutgoing && tickListenerId == -1)
				{
					tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);
				}
			}

			if (stargateState == EnumStargateState.ConnectedOutgoing && tickListenerId == -1)
			{
				if (remotePosition == null)
				{
					remoteLoadTimeout = StargateConfig.Loaded.MaxTimeoutSeconds;
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

			SyncStateToClients();
		}

		int tickTimer = 0;
		float tickAvg = 0;
		protected void OnTickServer(float delta)
		{
			switch (stargateState)
			{
				case EnumStargateState.DialingOutgoing:
					{
						NextAngle(delta);
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						tickTimer++;
						tickAvg += delta;
						if (tickTimer > 60)
						{
							tickTimer = 0;
							tickAvg = 0;
						}

						if (!stableConnection)
						{
							BlockEntityStargate remoteGate = GetRemoteGate();
							if (remoteGate == null)
							{
								remoteLoadTimeout -= delta;

								if (remoteLoadTimeout <= 0)
								{
									Api.Logger.Debug("remoteLoadTimeout reached, cancelling outgoing wormhole");
									TryDisconnect();
								}
								break;
							}

							stableConnection = true;
						}

						ProcessCollidingEntities();
						timeOpen += delta;
						
						if (timeOpen > StargateConfig.Loaded.MaxConnectionDurationSecondsMilkyway)
						{
							Api.Logger.Debug("wormhole has been open for max duration, shutting down connection");
							TryDisconnect();
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

			// We should preferably not keep a permanent reference to the remote gate
			// should be fine to keep it during a single tick, but best to yeet it here
			remoteGate = null;
			remoteSought = false;
		}

		/// <summary>
		/// Checks for, and teleports entities that collide with the event horizon<br/>
		/// Teleports and rotates players<br/>
		/// Teleports, preserves and rotates momentum for other entities
		/// </summary>
		protected void ProcessCollidingEntities()
		{
			BlockEntityStargate remoteGate = GetRemoteGate();
			if (remoteGate == null) return;

			Entity[] travelers = GetCollidingEntities();
			if (travelers.Length == 0) return;

			float rotateLocal = Block.Shape.rotateY;
			float rotateRemote = remoteGate.Block.Shape.rotateY;
			float rotateRadLocal = rotateLocal * GameMath.DEG2RAD;
			float thetaf = ((rotateRemote - rotateLocal + 540) % 360) * GameMath.DEG2RAD;
			float costhetaf = MathF.Cos(thetaf);
			float sinthetaf = MathF.Sin(thetaf);

			float originYaw;
			float motionX, motionY, motionZ;
			double offsetX, offsetY, offsetZ, offsetOriginX, offsetOriginZ;

			foreach (Entity traveler in travelers)
			{
				originYaw = (traveler.SidedPos.Yaw + thetaf) % GameMath.TWOPI;

				offsetY = traveler.Pos.Y - Pos.Y;
				offsetOriginX = Pos.X - traveler.Pos.X + 0.5f;
				offsetOriginZ = Pos.Z - traveler.Pos.Z + 0.5f;

				// rotate offset vector
				offsetX = offsetOriginX * costhetaf + offsetOriginZ * sinthetaf;
				offsetZ = offsetOriginZ * costhetaf - offsetOriginX * sinthetaf;

				if (Block.Shape.rotateY % 180 == 0)
				{
					if (remoteGate.Block.Shape.rotateY % 180 == 0) offsetX *= -1;
					else offsetZ *= -1;
				} else
				{
					if (remoteGate.Block.Shape.rotateY % 180 == 0) offsetZ *= -1;
					else offsetX *= -1;
				}

				traveler.SidedPos.Yaw = originYaw;
				traveler.SidedPos.HeadYaw = originYaw;

				traveler.TeleportToDouble(remoteGate.Pos.X + offsetX + 0.5f, remoteGate.Pos.Y + offsetY, remoteGate.Pos.Z + offsetZ + 0.5f);

				// coordinate system is wonky
				if (rotateLocal % 180 == 0)
				{
					rotateLocal = (rotateLocal + 180) % 360;
				}
				if (rotateRemote % 180 == 0)
				{
					rotateRemote = (rotateRemote + 180) % 360;
				}
				rotateLocal = (rotateLocal + 270) % 360;
				rotateRemote = (rotateLocal + 270) % 360;
				thetaf = (Math.Abs(rotateLocal - rotateRemote) + 180) % 360;
				costhetaf = MathF.Cos(thetaf * GameMath.DEG2RAD);
				sinthetaf = MathF.Sin(thetaf * GameMath.DEG2RAD);

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
						remoteGate.SyncStateToClients();
						((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(remotePosition, (int)EnumStargatePacketType.PlayerYaw, p);

						motionX = (float)traveler.SidedPos.Motion.X * costhetaf - (float)traveler.SidedPos.Motion.Z * sinthetaf;
						motionZ = (float)traveler.SidedPos.Motion.X * sinthetaf + (float)traveler.SidedPos.Motion.Z * costhetaf;
						motionY = (float)traveler.SidedPos.Motion.Y;

						traveler.SidedPos.Motion.Set(motionX, motionY, motionZ);
					}, 20);
				}
				else
				{
					motionX = (float)traveler.SidedPos.Motion.X * costhetaf - (float)traveler.SidedPos.Motion.Z * sinthetaf;
					motionZ = (float)traveler.SidedPos.Motion.X * sinthetaf + (float)traveler.SidedPos.Motion.Z * costhetaf;
					motionY = (float)traveler.SidedPos.Motion.Y;

					traveler.SidedPos.Motion.Set(motionX, motionY, motionZ);
				}
			}
		}

		protected Entity[] GetCollidingEntities()
		{
			BlockPos startPos;
			BlockPos endPos;
			if (Block.Shape.rotateY == 180 || Block.Shape.rotateY == 0)
			{
				startPos = Pos.AddCopy(-2f, 0.5f, 0);
				endPos = Pos.AddCopy(3f, 6f, 1f);
			} else
			{
				startPos = Pos.AddCopy(0, 0.5f, -2f);
				endPos = Pos.AddCopy(1f, 6f, 3f);
			}

			return Api.World.GetEntitiesInsideCuboid(startPos, endPos);
		}

		#region Dialing

		protected void OnGlyphReachedServer()
		{
			tickListenerId = RegisterDelayedCallback(OnGlyphActivatedServer, 2000);
		}

		private long timeoutCallbackId = -1;
		protected void OnGlyphActivatedServer(float delta)
		{
			awaitingChevronAnimation = false;
			activeChevrons++;

			if (currentAddressIndex == (dialingAddress.AddressLengthNum - 1))
			{
				if (dialingAddress.AddressBits == GateAddress.AddressBits)
				{
					ForceDisconnect();
					return;
				}

				// final glyph activated
				if (GetRemoteGate() == null)
				{
					Api.Logger.Debug("Final chevron locked, but remote gate not available yet");
					activeChevrons--;
					SyncStateToClients();
					if (timeoutCallbackId == -1)
					{
						timeoutCallbackId = RegisterDelayedCallback(OnRemoteTimeout, (int)(StargateConfig.Loaded.MaxTimeoutSeconds*1000));
					}

					return;
				}

				if (!IsIrisClear() || !remoteGate.IsIrisClear())
				{
					ForceDisconnect();
					return;
				}

				OnConnectionSuccessServer();
				return;
			}

			if (GetRemoteGate() != null)
			{
				if (!remoteNotified)
				{
					remoteGate.EvaluateIncomingConnection(this);
					remoteNotified = true;
				}

				remoteGate.SetActiveChevronsServer(activeChevrons);
			}

			currentAddressIndex++;
			rotateCW = (currentAddressIndex == 0) ? true : !rotateCW;
			nextGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];

			SyncStateToClients();

			tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);
		}

		protected void OnRemoteTimeout(float delta)
		{
			timeoutCallbackId = -1;
			if (stargateState == EnumStargateState.DialingOutgoing)
			{
				TryDisconnect();
				return;
			}
		}

		protected void OnConnectionSuccessServer()
		{
			stargateState = EnumStargateState.ConnectedOutgoing;
			timeOpen = 0f;
			if (tickListenerId != -1) UnregisterGameTickListener(tickListenerId);
			tickListenerId = RegisterGameTickListener(OnTickServer, 20, 0);
			spawnActivationParticles();

			RegisterDelayedCallback((t) =>
			{
				applyVortexDestruction();
			}, 750);

			if (GetRemoteGate() != null)
			{
				remoteGate.AcceptConnection(activeChevrons);
			}

			SyncStateToClients();
		}

		/// <summary>
		/// When gate fails to activate due to whatever reason on the server
		/// </summary>
		/// <param name="delta"></param>
		protected void OnConnectionFailureServer(float delta)
		{
			stargateState = EnumStargateState.Idle;
			activeChevrons = 0;

			if (GetRemoteGate() != null)
			{
				remoteGate.TryDisconnect();
			}

			if (isForceLoaded)
			{
				StargateManagerSystem.GetInstance(Api).ReleaseChunk(Pos);
				isForceLoaded = false;
			}

			SyncStateToClients();
		}

		protected void UpdateRemoteChevronsServer()
		{
			if (GetRemoteGate != null)
			{
				remoteGate.SetActiveChevronsServer(activeChevrons);
			}
		}

		/// <summary>
		/// Checks whether this stargate is capable of receiving
		/// an incoming wormhole<br/>
		/// If so, it will update it's own state accordingly
		/// </summary>
		/// <param name="caller"></param>
		/// <returns></returns>
		public bool EvaluateIncomingConnection(BlockEntityStargate caller)
		{
			// TODO: find a different way, since state may change over time
			if (stargateState != EnumStargateState.Idle) return false;
			if (!IsIrisClear()) return false;

			remotePosition = caller.Pos;
			dialingAddress = caller.GateAddress;

			stargateState = EnumStargateState.DialingIncoming;
			if (!isForceLoaded)
			{
				StargateManagerSystem.GetInstance(Api).RegisterLoadedGate(this);
				isForceLoaded = true;
			}

			SyncStateToClients();
			return true;
		}

		public bool AcceptConnection(byte activeChevrons)
		{
			stargateState = EnumStargateState.ConnectedIncoming;
			timeOpen = 0f;
			this.activeChevrons = activeChevrons;
			RegisterDelayedCallback((t) =>
			{
				applyVortexDestruction();
			}, 750);

			SyncStateToClients();

			return true;
		}

		#endregion

		#region Networking

		/// <summary>
		/// Handles packages received from client stargates
		/// </summary>
		/// <param name="fromPlayer"></param>
		/// <param name="packetid"></param>
		/// <param name="data"></param>
		public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
		{
			if (packetid <= 0xFFFF)
			{
				HandleInventoryPacket(fromPlayer, packetid, data);

				return;
			}

			switch ((EnumStargatePacketType)packetid)
			{
				case EnumStargatePacketType.Dial:
					GateStatePacket packet = SerializerUtil.Deserialize<GateStatePacket>(data);
					StargateAddress address = new StargateAddress();
					address.FromBits(packet.RemoteAddressBits);
					TryDial(address, (EnumDialSpeed)packet.DialType);
					break;
				case EnumStargatePacketType.Abort:
					TryDisconnect();
					break;
				case EnumStargatePacketType.CamoUpdate:
					TreeAttribute tree = new TreeAttribute();
					tree.FromBytes(data);
					ItemStack stack;

					for (int i = 0; i < Inventory.Count; i++)
					{
						if (tree.HasAttribute("stack" + i))
						{
							stack = tree.GetItemstack("stack" + i);
							stack.ResolveBlockOrItem(Api.World);
							Inventory[i].Itemstack = stack;
						} else
						{
							if (Inventory[i].Itemstack != null)
							{
								Inventory[i].TakeOutWhole();
							}
						}
					}
					break;
			}
		}

		/// <summary>
		/// Synchronizes server state only to specified client
		/// </summary>
		/// <param name="player"></param>
		public void SyncStateToPlayer(IPlayer player)
		{
			if (player.ClientId == 0) return;

			IServerPlayer splayer = ((ICoreServerAPI)Api).Server.Players.Where(
				(sp) => sp.ClientId == player.ClientId).FirstOrDefault(defaultValue: null);

			if (splayer == null || splayer.ConnectionState != EnumClientState.Connected) return;

			GateStatePacket packet = AssembleStatePacket();
			((ICoreServerAPI)Api).Network.SendBlockEntityPacket(splayer, Pos, (int)EnumStargatePacketType.State, packet);
		}

		/// <summary>
		/// Synchronizes server state to all clients
		/// </summary>
		public void SyncStateToClients()
		{
			GateStatePacket packet = AssembleStatePacket();
			((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, (int)EnumStargatePacketType.State, packet);
		}

		public void SendYawPacket(long entityId, float yaw)
		{
			PlayerYawPacket packet = new PlayerYawPacket
			{
				EntityId = entityId,
				Yaw = yaw
			};

			((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, (int)EnumStargatePacketType.PlayerYaw, packet);
		}

		protected void HandleInventoryPacket(IPlayer fromPlayer, int packetid, byte[] data)
		{
			// yes, I shamelessly copied this from BEOpenableContainer
			if (packetid < 1000)
			{
				Inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);

				Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
				return;
			}

			if (packetid == (int)EnumBlockEntityPacketId.Close)
			{
				fromPlayer.InventoryManager?.CloseInventory(Inventory);
			}

			if (packetid == (int)EnumBlockEntityPacketId.Open)
			{
				fromPlayer.InventoryManager?.OpenInventory(Inventory);
			}
		}

		#endregion

		#region State

		protected void SetActiveChevronsServer(byte chevrons)
		{
			activeChevrons = chevrons;
			SyncStateToClients();
		}

		public void CanAcceptConnection()
		{

		}

		public void CanAcceptConnectionFromAddress(StargateAddress incomingAddres)
		{

		}

		#endregion

		#endregion

		#region Clientside

		protected void InitializeClient(ICoreClientAPI capi)
		{
			// InitializeRenderers(capi);
			// TODO: move some things to attributes per type
			InitializeClientMilkyWay(capi);
			InitializeSounds(capi);
			UpdateChevronGlow(activeChevrons);
			UpdateRendererState();

			if (StargateState == EnumStargateState.DialingOutgoing || StargateState == EnumStargateState.ConnectedOutgoing)
			{
				if (tickListenerId == -1)
				{
					tickListenerId = RegisterGameTickListener(OnTickClient, 20, 0);
				}
			}

			inventory.SlotModified += OnCamoSlotModified;
		}

		/// <summary>
		/// Initializes and registers the main gate renderer. Should also call the
		/// event horizon renderer initializer
		/// </summary>
		/// <remarks>
		/// client side
		/// </remarks>
		/// <param name="capi"></param>
		protected void InitializeClientMilkyWay(ICoreClientAPI capi)
		{
			if (!rendererInitialized || renderer == null)
			{
				renderer = new MilkywayGateRenderer(capi, Pos);
				renderer.orientation = Block.Shape.rotateY;
				horizonlight = new LightiningPointLight(new Vec3f(0.9f, 0.5f, 0.5f), Pos.AddCopy(0, 3, 0).ToVec3d());

				animUtil.InitializeAnimator("milkyway_chevron_animation", null, null, new Vec3f(0, Block.Shape.rotateY, 0));

				capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
				UpdateRendererState();
				UpdateChevronGlow(activeChevrons);
			}

			if (!horizonInitialized)
			{
				InitializeHorizonRenderer(capi);
			}
		}

		protected void OnTickClient(float delta)
		{
			switch (stargateState)
			{
				case EnumStargateState.DialingOutgoing:
					{
						StartRotateSound();
						NextAngle(delta);
						UpdateRendererState();
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						timeOpen += delta;
						if (timeOpen >= StargateConfig.Loaded.MaxConnectionDurationSecondsMilkyway)
						{
							timeOpen = StargateConfig.Loaded.MaxConnectionDurationSecondsMilkyway;
						}

						break;
					}
				default:
					{
						if (tickListenerId == -1)
						{
							UnregisterGameTickListener(tickListenerId);
							tickListenerId = -1;
						}
						break;
					}
			}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			base.GetBlockInfo(forPlayer, dsc);

			dsc.AppendLine("Address: " + gateAddress.ToString());

#if (DEBUG)
			switch (stargateState)
			{
				case EnumStargateState.DialingOutgoing:
					{
						dsc.AppendLine("Dialing " + dialingAddress);
						dsc.AppendLine("Next glyph: " + nextGlyph);
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
#else
			switch (stargateState)
			{
				case EnumStargateState.DialingOutgoing:
					dsc.AppendLine(Lang.Get("astriaporta:stargate-blockinfo-dialingoutgoing") + dialingAddress);
					break;
				case EnumStargateState.DialingIncoming:
					dsc.AppendLine(Lang.Get("astriaporta:stargate-blockinfo-dialingincoming"));
					break;
				case EnumStargateState.ConnectedOutgoing:
					dsc.AppendLine(Lang.Get("astriaporta:stargate-blockinfo-connectedoutgoing"));
					break;
				case EnumStargateState.ConnectedIncoming:
					dsc.AppendLine(Lang.Get("astriaporta:stargate-blockinfo-connectedincoming"));
					break;
			}

			if (!IsIrisClear())
			{
				dsc.AppendLine(Lang.Get("astriaporta:stargate-blockinfo-obstructed"));
			}
#endif
		}

		#region Inventory

		protected void OnCamoSlotModified(int slotId)
		{
			MarkDirty(true);
		}

		#endregion

		#region Audio

		ILoadedSound rotateSound;
		ILoadedSound activeSound;
		
		// ILoadedSound lockSound;
		// ILoadedSound releaseSound;
		// ILoadedSound warningSound;
		// ILoadedSound vortexSound;
		// ILoadedSound breakSound;

		protected AssetLocation lockSoundLocation;
		protected AssetLocation releaseSoundLocation;
		protected AssetLocation warningSoundLocation;
		protected AssetLocation vortexSoundLocation;
		protected AssetLocation breakSoundLocation;

		protected void InitializeSounds(ICoreClientAPI capi)
		{
			// TODO:
			//		Create sounds in overriding classes
			lockSoundLocation = new AssetLocation("sounds/block/vesselclose.ogg");
			releaseSoundLocation = new AssetLocation("sounds/block/vesselopen.ogg");
			warningSoundLocation = new AssetLocation("sounds/block/hopperopen.ogg");
			vortexSoundLocation = new AssetLocation("sounds/environment/largesplash2.ogg");
			breakSoundLocation = new AssetLocation("sounds/effect/translocate-breakdimension.ogg");
		}

		protected void StartRotateSound()
		{
			if (rotateSound == null)
			{
				rotateSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
				{
					Location = new AssetLocation("sounds/block/quern.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 2f, 0.5f),
					DisposeOnFinish = false,
					Volume = 1f
				});
				rotateSound.Start();
				return;
			}

			if (rotateSound.IsPaused)
			{
				rotateSound.Start();
			}
		}

		protected void PauseRotateSound()
		{
			if (rotateSound == null) return;
			rotateSound.Pause();
		}

		protected void StopRotateSound()
		{
			if (rotateSound == null) return;
			rotateSound.Stop();
			rotateSound.Dispose();
			rotateSound = null;
		}

		protected void StartActiveSound()
		{
			if (activeSound == null)
			{
				activeSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
				{
					Location = new AssetLocation("sounds/environment/underwater.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 2f, 0.5f),
					DisposeOnFinish = false,
					Volume = 0.05f
				});
			}

			activeSound.Start();
		}

		protected void PauseActiveSound()
		{
			if (activeSound == null) return;
			activeSound.Pause();
		}

		protected void StopActiveSound()
		{
			if (activeSound == null) return;
			activeSound.Stop();
			activeSound.Dispose();
			activeSound = null;
		}

		protected void PauseAllSounds()
		{
			PauseRotateSound();
			PauseActiveSound();
		}

		protected void StopAllSounds()
		{
			StopRotateSound();
			StopActiveSound();
		}

		protected void DisposeAllSounds()
		{
			StopRotateSound();
			StopActiveSound();
		}

		#endregion

		#region Rendering

		protected GateRenderer renderer;
		protected EventHorizonRenderer eventHorizonRenderer;

		protected bool rendererInitialized = false;
		protected bool horizonInitialized = false;
		protected bool horizonRegistered = false;

		public BlockEntityAnimationUtil animUtil
		{
			get { return GetBehavior<BEBehaviorAnimatable>().animUtil; }
		}

		public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
		{
			ICoreClientAPI capi = (ICoreClientAPI)Api;

			Vec3f camoDirection = new Vec3f(0, 0, 0);
			camoDirection.X = (Block.Shape.rotateY == 0) ? 1 : (Block.Shape.rotateY == 180) ? -1 : 0;
			camoDirection.Z = (Block.Shape.rotateY == 90) ? -1 : (Block.Shape.rotateY == 270) ? 1 : 0;
			
			float offsetY;

			for (int i = 0; i < inventory.Count; i++)
            {
				offsetY = (i == 0 || i == Inventory.Count - 1) ? 1 : 0;
				ItemSlot slot = inventory[i];

				if (!slot.Empty && slot.Itemstack.Block != null)
				{
					MeshData mesh = capi.TesselatorManager.GetDefaultBlockMesh(
									slot.Itemstack.Block).Clone()
										.Translate(((i - 2) * camoDirection).Add(0, offsetY, 0));

					mesher.AddMeshData(mesh);
				}
			}

			return base.OnTesselation(mesher, tessThreadTesselator);
		}

		/// <summary>
		/// Initializes the event horizon renderer. Registers the renderer
		/// if the gate is connected
		/// </summary>
		/// <param name="capi"></param>
		internal void InitializeHorizonRenderer(ICoreClientAPI capi)
		{
			TextureAtlasPosition texPos = capi.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonTexPos;

			eventHorizonRenderer = new EventHorizonRenderer(capi, Pos, texPos, false);
			eventHorizonRenderer.shouldRender = false;
			eventHorizonRenderer.Orientation = Block.Shape.rotateY;

			horizonInitialized = true;

			if (stargateState == EnumStargateState.ConnectedIncoming || stargateState == EnumStargateState.ConnectedOutgoing)
			{
				ActivateHorizon(false);
			}
		}

		/// <summary>
		/// Activates and registers the renderer for
		/// the gate event horizon
		/// </summary>
		/// <param name="isActivating">Whether the splash should occur</param>
		internal void ActivateHorizon(bool isActivating = true)
		{
			if (eventHorizonRenderer == null) return;

			StopRotateSound();
			StartActiveSound();

			eventHorizonRenderer.t = 0;
			eventHorizonRenderer.activating = isActivating;
			eventHorizonRenderer.shouldRender = true;
			if (!horizonRegistered)
			{
				((ICoreClientAPI)Api).Event.RegisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
				horizonRegistered = true;
			}

			if (!lightAdded)
			{
				(Api as ICoreClientAPI).Render.AddPointLight(horizonlight);
				lightAdded = true;
			}

			if (isActivating)
			{
				spawnActivationParticles();
			}
		}

		/// <summary>
		/// Disables and unregisters the event horizon renderer
		/// </summary>
		internal void DeactivateHorizon()
		{
			if (eventHorizonRenderer == null) return;

			StopActiveSound();

			eventHorizonRenderer.shouldRender = false;
			((ICoreClientAPI)Api).Event.UnregisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
			horizonRegistered = false;
			(Api as ICoreClientAPI).Render.RemovePointLight(horizonlight);
			lightAdded = false;
		}

		/// <summary>
		/// Updates the renderer state.
		/// Currently updates only the inner ring rotation
		/// </summary>
		internal void UpdateRendererState()
		{
			if (renderer == null) return;

			renderer.ringRotation = currentAngle;
		}

		internal void UpdateChevronGlow(int activeChevrons)
		{
			if (DialingAddress == null)
			{
				UpdateChevronGlow(activeChevrons, EnumAddressLength.Short);
			} else
			{
				UpdateChevronGlow(activeChevrons, DialingAddress.AddressLength);
			}
		}

		/// <summary>
		/// Updates which chevrons should glow, depending on the
		/// active chevrons and the length of the address be
		/// </summary>
		/// <param name="activeChevrons"></param>
		/// <param name="length"></param>
		internal void UpdateChevronGlow(int activeChevrons, EnumAddressLength length)
		{
			if (renderer == null) return;

			renderer.chevronGlow = new byte[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			byte padding = 0;

			for (int i = 1; i < 10; i++)
			{
				switch (length)
				{
					case EnumAddressLength.Short:
						{
							if (i == 4 || i == 5)
							{
								padding++;
								continue;
							}

							if (i <= (activeChevrons + padding))
							{
								renderer.chevronGlow[i - 1] = 200;
							}
							break;
						}
					case EnumAddressLength.Medium:
						{
							if (i == 5)
							{
								padding++;
								continue;
							}

							if (i <= (activeChevrons + padding))
							{
								renderer.chevronGlow[i - 1] = 200;
							}

							break;
						}
					case EnumAddressLength.Long:
						{
							if (i <= activeChevrons + padding) renderer.chevronGlow[i - 1] = 200;

							break;
						}
				}
			}
		}

		internal void applyVortexDestruction()
		{
			if (Api.Side == EnumAppSide.Client) return;

			IBlockAccessor accessor = (Api as ICoreServerAPI).World.BlockAccessor;
			Cuboidi positions;

			positions = VortexArea;

			if (StargateConfig.Loaded.VortexDestroys)
			{
				if (positions == null) return;
				for (int x = positions.MinX; x <= positions.MaxX; x++)
				{
					for (int z = positions.MinZ; z <= positions.MaxZ; z++)
					{
						for (int y = positions.MinY; y <= positions.MaxY; y++)
						{
							accessor.SetBlock(0, Pos.AddCopy(x, y, z));
						}
					}
				}
			}

			if (!StargateConfig.Loaded.VortexKills) return;

			EntityPlayer player;
			Entity[] toKill = (Api as ICoreServerAPI).World.GetEntitiesInsideCuboid(
				Pos.AddCopy(positions.MinX, positions.MinY, positions.MinZ),
				Pos.AddCopy(positions.MaxX + 1, positions.MaxY + 1, positions.MaxZ + 1));

			for (int i = 0; i < toKill.Length; i++)
			{
				if (toKill[i] is EntityPlayer)
				{
					player = toKill[i] as EntityPlayer;
					if (player.Player.WorldData.CurrentGameMode != EnumGameMode.Survival)
					{
						continue;
					}
				}

				DamageSource source = new DamageSource();
				source.Source = EnumDamageSource.Void;

				toKill[i].Die(EnumDespawnReason.Death, source);
			}
		}

		internal void spawnActivationParticles()
		{
			int particleColor = ColorUtil.ColorFromRgba(new Vec4f(0.8f, 0.2f, 0.2f, 0.8f));
			float offsetXMin = 0;
			float offsetXMax = 0;
			float offsetZMin = 0;
			float offsetZMax = 0;
			Vec3f minV = new Vec3f();
			Vec3f maxV = new Vec3f();

			switch (Block.Shape.rotateY)
			{
				case 0f:
					{
						offsetXMin = -1f;
						offsetXMax = 2f;
						offsetZMin = 0.5f;
						offsetZMax = 0.5f;

						minV.Z = 1f;
						maxV.Z = 2f;

						break;
					}
				case 90f:
					{
						offsetXMin = 0.5f;
						offsetXMax = 0.5f;
						offsetZMin = -1f;
						offsetZMax = 2f;

						minV.X = 1f;
						maxV.X = 2f;

						break;
					}
				case 180f:
					{
						offsetXMin = -1f;
						offsetXMax = 2f;
						offsetZMin = 0.5f;
						offsetZMax = 0.5f;
						
						minV.Z = -1f;
						maxV.Z = -2f;

						break;
					}
				case 270f:
					{
						offsetXMin = 0.5f;
						offsetXMax = 0.5f;
						offsetZMin = -1f;
						offsetZMax = 2f;
						
						minV.X = -1f;
						maxV.X = -2f;

						break;
					}
			}

			RegisterDelayedCallback((float d) =>
			{
				Api.World.SpawnParticles(new SimpleParticleProperties()
				{
					MinQuantity = 64f,
					AddQuantity = 64f,
					Color = particleColor,
					MinPos = new Vec3d(Pos.X + offsetXMin, Pos.Y + 2, Pos.Z + offsetZMin),
					AddPos = new Vec3d(offsetXMax - offsetXMin, 3, offsetZMax - offsetZMin),
					MinVelocity = minV,
					AddVelocity = maxV - minV,
					ParticleModel = EnumParticleModel.Cube,
					OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f),
					Async = true,
					GravityEffect = 0,
					WithTerrainCollision = false,
					MinSize = 0.5f,
					MaxSize = 1f,
					LifeLength = 0.25f,
					addLifeLength = 0.25f,
					LightEmission = particleColor
				});
			}, 500);
		}

		internal void spawnDeactivationParticles()
		{
			int particleColor = ColorUtil.ColorFromRgba(new Vec4f(0.9f, 0.5f, 0.5f, 0.75f));
			float offsetXMin = 0;
			float offsetXMax = 0;
			float offsetZMin = 0;
			float offsetZMax = 0;

			switch (Block.Shape.rotateY)
			{
				case 0f:
					{
						offsetXMin = -1f;
						offsetXMax = 2f;
						offsetZMin = 0.5f;
						offsetZMax = 0.5f;

						break;
					}
				case 90f:
					{
						offsetXMin = 0.5f;
						offsetXMax = 0.5f;
						offsetZMin = -1f;
						offsetZMax = 2f;

						break;
					}
				case 180f:
					{
						offsetXMin = -1f;
						offsetXMax = 2f;
						offsetZMin = 0.5f;
						offsetZMax = 0.5f;

						break;
					}
				case 270f:
					{
						offsetXMin = 0.5f;
						offsetXMax = 0.5f;
						offsetZMin = -1f;
						offsetZMax = 2f;

						break;
					}
			}

			RegisterDelayedCallback((float d) =>
			{
				Api.World.SpawnParticles(new SimpleParticleProperties()
				{
					MinQuantity = 64f,
					AddQuantity = 64f,
					Color = particleColor,
					MinPos = new Vec3d(Pos.X + offsetXMin, Pos.Y + 2, Pos.Z + offsetZMin),
					AddPos = new Vec3d(offsetXMax - offsetXMin, 3, offsetZMax - offsetZMin),
					MinVelocity = new Vec3f(0, 0, 0),
					AddVelocity = new Vec3f(0, 0, 0),
					ParticleModel = EnumParticleModel.Cube,
					OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f),
					Async = true,
					GravityEffect = 0,
					WithTerrainCollision = false,
					MinSize = 0.5f,
					MaxSize = 1f,
					LifeLength = 0.1f,
					addLifeLength = 0.1f,
					LightEmission = particleColor
				});
			}, 500);
		}

		/// <summary>
		/// Disposes of the main and event horizon renderers
		/// </summary>
		internal void DisposeRenderers()
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			if (capi == null) return;

			if (renderer != null)
			{
				capi.Event.UnregisterRenderer(renderer, EnumRenderStage.Opaque);
			}
			if (eventHorizonRenderer != null)
			{
				capi.Event.UnregisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
				horizonRegistered = false;
			}
			if (lightAdded)
			{
				capi.Render.RemovePointLight(horizonlight);
			}

			renderer?.Dispose();
			renderer = null;
			eventHorizonRenderer?.Dispose();
			eventHorizonRenderer = null;

			rendererInitialized = false;
			horizonInitialized = false;
		}

		#endregion

		#region Dialing

		protected void OnGlyphReachedClient()
		{
			tickListenerId = RegisterDelayedCallback(OnGlyphDownClient, 1000);
			RegisterDelayedCallback((float t) =>
			{
				((ICoreClientAPI)Api).World.PlaySoundAt(releaseSoundLocation, Pos.X, Pos.Y, Pos.Z, null, false);
			}, 500);

			if (renderer != null) renderer.chevronGlow[8] = 200;

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
			PauseRotateSound();
			((ICoreClientAPI)Api).World.PlaySoundAt(lockSoundLocation, Pos.X, Pos.Y, Pos.Z, null, false);

			awaitingChevronAnimation = true;
		}

		protected void OnGlyphDownClient(float delta)
		{
			tickListenerId = -1;
			awaitingChevronAnimation = false;

			if (renderer != null)
			{
				renderer.chevronGlow[8] = 0;
			}
		}

		#endregion

		#region Networking

		/// <summary>
		/// Decodes server packet and redirects to the correct handler
		/// </summary>
		/// <param name="packetid"></param>
		/// <param name="data"></param>
		public override void OnReceivedServerPacket(int packetid, byte[] data)
		{
			// base.OnReceivedServerPacket(packetid, data);
			switch ((EnumStargatePacketType)packetid)
			{
				case EnumStargatePacketType.State:
					{
						ProcessStatePacket(SerializerUtil.Deserialize<GateStatePacket>(data));
						break;
					}
				case EnumStargatePacketType.PlayerYaw:
					{
						ProcessYawPacket(SerializerUtil.Deserialize<PlayerYawPacket>(data));
						break;
					}
			}
		}

		/// <summary>
		/// Evaluates a received state packet and updates the
		/// state of the gate accordingly
		/// </summary>
		/// <param name="packet"></param>
		protected void ProcessStatePacket(GateStatePacket packet)
		{
			EnumStargateState newState = (EnumStargateState)packet.State;
			dialingAddress = new StargateAddress();
			dialingAddress.FromBits(packet.RemoteAddressBits);

			Api.Logger.Debug("Received server packet, new state: " + newState);

			activeChevrons = packet.ActiveChevrons;
			currentGlyph = packet.CurrentGlyph;
			currentAddressIndex = packet.CurrentGlyphIndex;
			rotateCW = packet.RotateCW;
			activeChevrons = packet.ActiveChevrons;
			currentAngle = packet.CurrentAngle;
			nextGlyph = dialingAddress.AddressCoordinates.Glyphs[currentAddressIndex];

			switch (newState)
			{
				case EnumStargateState.Idle:
					{
						TransitionIdleClient();
						break;
					}
				case EnumStargateState.DialingIncoming:
					{
						TransitionDialingIncomingClient();
						break;
					}
				case EnumStargateState.DialingOutgoing:
					{
						TransitionDialingOutgoingClient();
						break;
					}
				case EnumStargateState.ConnectedIncoming:
					{
						TransitionConnectedIncomingClient();
						break;
					}
				case EnumStargateState.ConnectedOutgoing:
					{
						TransitionConnectedOutgoingClient();
						break;
					}
			}

			UpdateChevronGlow(activeChevrons);
			UpdateRendererState();
			stargateState = newState;
		}

		/// <summary>
		/// Rotates the local player camera yaw
		/// </summary>
		/// <param name="packet"></param>
		protected void ProcessYawPacket(PlayerYawPacket packet)
		{
			if (eventHorizonRenderer != null && !horizonRegistered)
			{
				ActivateHorizon();
				UpdateChevronGlow(activeChevrons);
				UpdateRendererState();
			}

			Entity ent = Api.World.GetEntityById(packet.EntityId);
			if (ent == null || !(ent is EntityPlayer)) return;
			EntityPlayer ep = (EntityPlayer)ent;

			if ((Api as ICoreClientAPI).World.Player.Entity.EntityId != ep.EntityId) return;

			(Api as ICoreClientAPI).World.Player.CameraYaw = packet.Yaw;
		}

		protected void DialServerGate(StargateAddress address, EnumDialSpeed speed)
		{
			GateStatePacket packet = new GateStatePacket
			{
				RemoteAddressBits = address.AddressBits,
				DialType = (int)speed
			};

			((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, (int)EnumStargatePacketType.Dial, packet);
		}

		#endregion

		#region State

		/// <summary>
		/// Manages transitions into the idle state
		/// </summary>
		protected void TransitionIdleClient()
		{
			if (stargateState == EnumStargateState.Idle) return;
			ICoreClientAPI capi = (ICoreClientAPI)Api;
			timeOpen = 0f;

			if (stargateState == EnumStargateState.DialingIncoming)
			{
				// Connection failure, play failure sound and deactivate all chevrons
			}
			else if (stargateState == EnumStargateState.ConnectedIncoming || stargateState == EnumStargateState.ConnectedOutgoing)
			{
				// play wormhole closing sound and disable event horizon,
				// summon short-lived particles and deactivate all chevrons
				capi.World.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension.ogg"), Pos.X, Pos.Y, Pos.Z, null, false);
				spawnDeactivationParticles();

			} else
			{
				StopAllSounds();
			}

			RegisterDelayedCallback((t) => DeactivateHorizon(), 750);
			RegisterDelayedCallback((t) => UpdateChevronGlow(0, EnumAddressLength.Short), 1000);

			if (tickListenerId != 0)
			{
				UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}
		}

		/// <summary>
		/// Manages transitions into the dialingOutgoing state
		/// </summary>
		protected void TransitionDialingOutgoingClient()
		{
			if (stargateState == EnumStargateState.Idle)
			{
				// start dialing outgoing wormhole
				// register OnTickClient if empty
			}

			// always register ticklistener, as it needs to be restarted when the server
			// signals that the glyph activation is completed

			if (tickListenerId == -1) tickListenerId = RegisterGameTickListener(OnTickClient, 20, 0);
		}

		/// <summary>
		/// Manages transitions into the dialingIncoming state
		/// </summary>
		protected void TransitionDialingIncomingClient()
		{
			// Update chevron glow
			UpdateChevronGlow(activeChevrons);
		}

		/// <summary>
		/// Manages transitions into the connectedOutgoing state
		/// </summary>
		protected void TransitionConnectedOutgoingClient()
		{
			if (stargateState != EnumStargateState.ConnectedOutgoing)
			{
				// play activation sound
				// register OnTickClient if empty
				if (tickListenerId == -1) tickListenerId = RegisterGameTickListener(OnTickClient, 20, 750);
				RegisterDelayedCallback((t) =>
				{
					ActivateHorizon(true);
					((ICoreClientAPI)Api).World.PlaySoundAt(vortexSoundLocation, Pos.X, Pos.Y, Pos.Z, null, false);
				}, 750);
			}
		}

		/// <summary>
		/// Manages transitions into the connectedIncoming state
		/// </summary>
		protected void TransitionConnectedIncomingClient()
		{
			if (stargateState != EnumStargateState.ConnectedIncoming)
			{
				// play activation sound
				// register OnTickClient if empty
				RegisterDelayedCallback((t) =>
				{
					ActivateHorizon();
					((ICoreClientAPI)Api).World.PlaySoundAt(vortexSoundLocation, Pos.X, Pos.Y, Pos.Z, null, false);
				}, 750);
			}
		}

		#endregion

		#region Interaction

		protected GuiDialogBlockEntity gateDialog;

		public bool OnRightClickInteraction(IPlayer player)
		{
			ItemSlot usingSlot = player.InventoryManager.ActiveHotbarSlot;
			if (usingSlot != null && usingSlot?.Itemstack?.Item?.FirstCodePart() == "paper")
			{
				if (Api.Side == EnumAppSide.Client)
				{
					(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				} else
				{
					createCartoucheForSlot(usingSlot, player);
				}

				return true;
			}

			if (Api.Side == EnumAppSide.Client)
			{
				toggleInventoryDialogClient(player, (ICoreClientAPI)Api);
			}

			return true;
		}

		private void createCartoucheForSlot(ItemSlot forslot, IPlayer forplayer)
		{
			ItemStack cartouche = new ItemStack(Api.World.GetItem("astriaporta:addressnote"));
			cartouche.Attributes.SetString("gateAddressS", GateAddress.ToString());
			cartouche.StackSize = 1;

			forslot.TakeOut(1);
			forplayer.InventoryManager.TryGiveItemstack(cartouche);
			forslot.MarkDirty();
		}

		protected void toggleInventoryDialogClient(IPlayer byPlayer, ICoreClientAPI capi)
		{
			if (gateDialog == null)
			{
				var check = Lang.GetAllEntries();
				// TODO: get stargate specific name from lang
				gateDialog = new GuiDialogStargate(Block.GetPlacedBlockName(capi.World, Pos), GateAddress.ToString(), Inventory, Pos, capi);
				gateDialog.OnClosed += () =>
				{
					gateDialog = null;
					capi.Network.SendPacketClient(Inventory.Close(byPlayer));
				};

				bool wasSuccess = false;
				wasSuccess = gateDialog.TryOpen();
				capi.Network.SendPacketClient(Inventory.Open(byPlayer));
			} else
			{
				gateDialog.TryClose();
			}
		}

		#endregion

#endregion
	}
}
