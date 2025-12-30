using AstriaPorta.Config;
using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AstriaPorta.Content
{
    public abstract class StargateBase : BlockEntity, IStargate, IBlockEntityInteractable
    {
        public StargateBase()
        {
            Address = new StargateAddress();
            InitializeInventory();
        }

        protected IDialHomeDevice ControllingDhd;
        protected GuiDialogStargate GateDialog;
        protected InventoryGeneric inventory;
        protected IStargate RemoteGate;
        public BlockPos RemotePosition { get; set; }
        protected bool RemoteSought;

        /// <inheritdoc/>
        public IStargateAddress Address { get; protected set; }

        /// <inheritdoc/>
        public IStargateAddress DialingAddress
        {
            get
            {
                return StateManager.DialingAddress;
            }
            protected set
            {
                StateManager.DialingAddress = value;
            }
        }

        /// <inheritdoc/>
        public IStargateAddress ConnectedAddress { get; protected set; }

        public InventoryBase Inventory => inventory;

        /// <inheritdoc/>
        public EnumStargateState State => StateManager.State;

        /// <inheritdoc/>
        public EnumStargateType Type { get; protected set; }

        /// <inheritdoc/>
        public int GlyphLength => Type switch
        {
            EnumStargateType.Milkyway => 39,
            EnumStargateType.Pegasus => 36,
            EnumStargateType.Destiny => 36,
            _ => 36
        };

        /// <inheritdoc/>
        public bool UseQuickDial
        {
            get
            {
                return StateManager.UseQuickDial;
            }
            set
            {
                StateManager.UseQuickDial = value;
            }
        }

        /// <inheritdoc/>
        public bool CanBreak
        {
            get
            {
                return State != EnumStargateState.ConnectedOutgoing && State != EnumStargateState.ConnectedIncoming;
            }
        }

        new public BlockPos Pos
        {
            get
            {
                return base.Pos;
            }
            set
            {
                base.Pos = value;
            }
        }

        public bool IsRemoteLoaded => RemoteGate != null;

        public bool IsForceLoaded
        {
            get
            {
                return StateManager.IsForceLoaded;
            }
            set
            {
                StateManager.IsForceLoaded = value;
            }
        }

        /// <summary>
        /// The area that is affected by the gate's opening vortex
        /// </summary>
        public Cuboidi VortexArea
        {
            get
            {
                return StargateVolumeManager.Instance.GetVortexVolume(Block.Shape.rotateY);
            }
        }

        /// <summary>
        /// The area that is checked for iris state validity
        /// </summary>
        public Cuboidi IrisArea
        {
            get
            {
                return StargateVolumeManager.Instance.GetIrisVolume(Block.Shape.rotateY);
            }
        }

        /// <inheritdoc/>
        public StargateVisualManager VisualManager { get; protected set; }

        /// <inheritdoc/>
        public StargateSoundManager SoundManager { get; protected set; }

        /// <inheritdoc/>
        public StargateStateManagerBase StateManager { get; protected set; }

        // Called when:
        //      BE spawned
        //      BE loaded from chunk (but fromTree first)
        //
        // NOT called when:
        //      BE dropped by schematic placement
        //      But we do call it manually
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            Type = (Block.Variant["gatetype"] ?? "milkyway") switch
            {
                "milkyway" => EnumStargateType.Milkyway,
                "pegasus" => EnumStargateType.Pegasus,
                "destiny" => EnumStargateType.Destiny,
                _ => EnumStargateType.Milkyway
            };
            Address.FromCoordinates(Pos.X, Pos.Y, Pos.Z, api);

            StateManager?.Initialize(this);
            VisualManager?.Initialize();

            Inventory.Pos = Pos;
            Inventory.LateInitialize($"stargate-{Pos.X}/{Pos.Y}/{Pos.Z}", api);

            if (api.Side == EnumAppSide.Client)
            {
                Inventory.SlotModified += OnCamoSlotModified;
            }
        }

        /// <inheritdoc/>
        public void AcceptConnection(byte activeChevrons)
        {
            StateManager.AcceptConnection(activeChevrons);
        }

        public void ApplyVortexDestruction()
        {
            if (Api.Side == EnumAppSide.Client) return;

            IBlockAccessor accessor = (Api as ICoreServerAPI).World.BlockAccessor;
            Cuboidi positions;

            positions = VortexArea;

            if (StargateConfig.Loaded.VortexDestroys)
            {
                if (positions == null) return;
                for (int x = positions.MinX; x < positions.MaxX; x++)
                {
                    for (int z = positions.MinZ; z < positions.MaxZ; z++)
                    {
                        for (int y = positions.MinY; y < positions.MaxY; y++)
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

        /// <inheritdoc/>
        public bool AttemptDhdRegistration(IDialHomeDevice dhd)
        {
            if (ControllingDhd != null && ControllingDhd == dhd) return true;

            CheckDhdStillValid();

            if (ControllingDhd != null) return false;
            ControllingDhd = dhd;

            return true;
        }

        /// <inheritdoc/>
        public bool CanDial(IStargateAddress toAddress)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if the currently controlling DHD is still valid
        /// </summary>
        public void CheckDhdStillValid()
        {
            if (ControllingDhd == null) return;

            if (Api.World.BlockAccessor.GetBlockEntity(ControllingDhd.Pos) is not IDialHomeDevice)
            {
                ControllingDhd = null;
            }
        }

        protected void CreateCartoucheForSlot(ItemSlot forSlot, IPlayer forPlayer)
        {
            ItemStack cartouche = new ItemStack(Api.World.GetItem("astriaporta:addressnote"));
            cartouche.Attributes.SetString("gateAddressS", Address.ToString());
            cartouche.Attributes.SetBool("noConsumeOnCrafting", true);
            cartouche.StackSize = 1;

            forSlot.TakeOut(1);
            forPlayer.InventoryManager.TryGiveItemstack(cartouche);
            forSlot.MarkDirty();
        }

        protected virtual void DestroyGate()
        {
            StateManager.Dispose();
            SoundManager?.Dispose();
            VisualManager?.Dispose();

            if (GetRemoteGate() != null && Api.Side == EnumAppSide.Server) RemoteGate.ForceDisconnect();
            GateDialog?.Dispose();
        }

        /// <summary>
        /// Sends a message to the server side gate to initiate
        /// the closing of the gate
        /// </summary>
        private void DisconnectServerGate()
        {
            (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, (int)EnumStargatePacketType.Abort);
        }

        protected bool DoNormalInteraction(IPlayer player)
        {
            ItemSlot usingSlot = player.InventoryManager.ActiveHotbarSlot;
            if (usingSlot != null && usingSlot.Itemstack?.Item?.FirstCodePart() == "paper")
            {
                if (Api.Side == EnumAppSide.Client)
                {
                    (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                }
                else
                {
                    CreateCartoucheForSlot(usingSlot, player);
                }

                return true;
            }

            if (Api.Side == EnumAppSide.Client)
            {
                ToggleInventoryDialog(player, Api as ICoreClientAPI);
            }

            return true;
        }

        protected bool DoShiftInteraction(IPlayer player)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool EvaluateIncomingConnection(IStargate fromGate)
        {
            if (State != EnumStargateState.Idle) return false;
            if (!IsIrisClear()) return false;

            RemotePosition = fromGate.Pos;
            DialingAddress = fromGate.Address;

            StateManager.AcceptIncomingConnection(fromGate);
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            StateManager.FromTreeAttributes(tree, worldAccessForResolve);

            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventoryCamo"));
            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }

            if (tree.HasAttribute("remotePositionX"))
            {
                RemotePosition = tree.GetBlockPos("remotePosition");
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            StateManager.ToTreeAttributes(tree);

            if (RemotePosition != null)
            {
                tree.SetBlockPos("remotePosition", RemotePosition);
            }

            ITreeAttribute invtree = new TreeAttribute();
            inventory.ToTreeAttributes(invtree);
            tree["inventoryCamo"] = invtree;
        }

        /// <inheritdoc/>
        public void ForceDisconnect(bool notifyRemote = true)
        {
            StateManager.ForceDisconnect(notifyRemote);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine("Address: " + Address.ToString());

#if DEBUG
            switch (State)
            {
                case EnumStargateState.DialingOutgoing:
                    dsc.AppendLine("Dialing " + DialingAddress);
                    dsc.AppendLine("Next glyph: " + StateManager.NextGlyph);
                    break;
                case EnumStargateState.DialingIncoming:
                    dsc.AppendLine("Being dialed by remote gate");
                    break;
                case EnumStargateState.ConnectedOutgoing:
                    dsc.AppendLine("Going to " + DialingAddress);
                    dsc.AppendLine("Time until deactivation: " + (60f - StateManager.TimeOpen) + " s");
                    break;
                case EnumStargateState.ConnectedIncoming:
                    dsc.AppendLine("Receive from " + DialingAddress);
                    break;
                case EnumStargateState.Idle:
                    dsc.AppendLine("Gate Idle");
                    break;
            }

            dsc.AppendLine("orientation: " + Block.Shape.rotateY);
#endif
            switch (State)
            {
                case EnumStargateState.DialingOutgoing:
                    dsc.AppendLine(Lang.Get("astriaporta:stargate-blockinfo-dialingoutgoing") + DialingAddress);
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
        }

#nullable enable
        /// <inheritdoc/>
        public IStargate? GetRemoteGate()
        {
            if (RemotePosition != null)
            {
                if (RemoteSought)
                {
                    return RemoteGate;
                }

                var gate = Api.World.BlockAccessor.GetBlockEntity(RemotePosition) as IStargate;
                RemoteGate = gate;
                RemoteSought = true;
                return RemoteGate;
            }

            return null;
        }
#nullable disable

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

        protected virtual void InitializeInventory()
        {
            inventory = new InventoryGeneric(5, null, null, null);
            inventory.SlotModified += OnInventoryChanged;
        }

        /// <inheritdoc/>
        public bool IsIrisClear()
        {
            var offsets = IrisArea;
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

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);

            DestroyGate();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            StateManager.Dispose();
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            DestroyGate();
        }

        protected void OnCamoSlotModified(int slotId)
        {
            MarkDirty(true);
        }

        protected void OnInventoryChanged(int slotId)
        {
            MarkDirty();
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
        {
            base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);

            if (!resolveImports) return;

            Inventory.Pos = Pos;
            foreach (ItemSlot slot in inventory)
            {
                if (slot.Itemstack != null)
                {
                    if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForNewMappings))
                    {
                        slot.Itemstack = null;
                    }
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

        public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
        {
            base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);

            Initialize(api);
        }

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

            var packetType = (EnumStargatePacketType)packetid;
            StateManager.ProcessStatePacket(packetType, data);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            StateManager.ProcessStatePacket((EnumStargatePacketType)packetid, data);
        }

        /// <summary>
        /// Yes, this Interface does, indeed, support interactions
        /// </summary>
        /// <returns></returns>
        public bool OnRightClickInteraction(IPlayer player)
        {
            if (player.Entity.Controls.ShiftKey)
            {
                return DoShiftInteraction(player);
            }

            return DoNormalInteraction(player);
        }

        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);

            foreach (ItemSlot slot in inventory)
            {
                ItemStack itemstack = slot.Itemstack;
                if (itemstack != null)
                {
                    itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
                }
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            var capi = Api as ICoreClientAPI;

            var camoDirection = new Vec3f(0, 0, 0);
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

        /// <inheritdoc/>
        public void ReleaseRemoteGate()
        {
            RemoteGate = null;
            RemoteSought = false;
        }

        protected void ToggleInventoryDialog(IPlayer byPlayer, ICoreClientAPI capi)
        {
            if (GateDialog != null)
            {
                GateDialog.TryClose();
                return;
            }

            GateDialog = new GuiDialogStargate(Block.GetPlacedBlockName(capi.World, Pos), Address.ToString(), inventory, Pos, capi, UseQuickDial);
            GateDialog.OnQuickDialToggledClient = (b) => { UseQuickDial = b; };
            GateDialog.OnClosed += () =>
            {
                GateDialog = null;
                capi.Network.SendPacketClient(inventory.Close(byPlayer));
            };

            bool wasSuccess = false;
            wasSuccess = GateDialog.TryOpen();
            capi.Network.SendPacketClient(inventory.Open(byPlayer));
        }

        /// <inheritdoc/>
        public void TryDisconnect()
        {
            if (Api.Side == EnumAppSide.Client)
            {
                DisconnectServerGate();
            }
            else
            {
                ForceDisconnect();
            }
        }

        /// <inheritdoc/>
        public bool TryDial()
        {
            return TryDial(DialingAddress, EnumDialSpeed.Default);
        }

        /// <inheritdoc/>
        public bool TryDial(IStargateAddress address)
        {
            return TryDial(address, EnumDialSpeed.Default);
        }

        /// <inheritdoc/>
        public virtual bool TryDial(IStargateAddress address, EnumDialSpeed dialSpeed)
        {
            return StateManager.TryDial(address, dialSpeed);
        }

        /// <summary>
        /// Checks if the dial to the provided gate address will succeed<br/>
        /// Takes the current state of the receiving gate into account
        /// </summary>
        /// <remarks>
        /// May attempt to load remote chunks
        /// </remarks>
        /// <param name="toAddress"></param>
        /// <returns></returns>
        public bool WillDialSucceed(IStargateAddress toAddress)
        {
            return StateManager.WillDialSucceed(toAddress);
        }
    }
}
