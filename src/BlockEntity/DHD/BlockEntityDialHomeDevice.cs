using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
    public class BlockEntityDialHomeDevice : BlockEntity, IBlockEntityInteractable
    {
        private static Cuboidi searchArea = null;

        private BlockEntityStargate connectedGate;
        private BlockPos connectedPos;

        private bool isActive = false;

        public string LinkedAddress
        {
            get
            {
                return connectedGate?.GateAddress.ToString() ?? Lang.Get("astriaporta:astriaporta-dhd-no-gate-linked");
            }
        }

        public bool IsGateOpen
        {
            get
            {
                return connectedGate?.StargateState == EnumStargateState.ConnectedOutgoing || connectedGate?.StargateState == EnumStargateState.ConnectedIncoming;
            }
        }

        public bool CanCloseGate
        {
            get
            {
                return IsGateOpen || connectedGate?.StargateState == EnumStargateState.DialingOutgoing;
            }
        }

        public bool CanBreak
        {
            get
            {
                return connectedGate?.StargateState != EnumStargateState.ConnectedOutgoing && connectedGate?.StargateState != EnumStargateState.DialingOutgoing;
            }
        }

        public Cuboidi SearchArea
        {
            get
            {
                return searchArea;
            }
        }

        public BlockEntityDialHomeDevice() : base()
        {
            if (searchArea == null)
            {
                searchArea = new Cuboidi(-7, -2, -7, 7, 2, 7);
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine($"Linked to: {LinkedAddress}");
        }

        /// <summary>
        /// Finds the closest gate to this DHD<br/>
        /// Expensive, use as little as possible
        /// </summary>
        /// <returns></returns>
        protected BlockEntityStargate FindClosestGate()
        {
            Cuboidi searchOffsets = SearchArea;

            List<BlockEntityStargate> targetBlocks = new List<BlockEntityStargate>();
            BlockEntityStargate foundBE = null;

            for (int x = searchOffsets.MinX; x <= searchOffsets.MaxX; x++)
            {
                for (int z = searchOffsets.MinZ; z <= searchOffsets.MaxZ; z++)
                {
                    for (int y = searchOffsets.MinY; y <= searchOffsets.MaxY; y++)
                    {
                        foundBE = Api.World.BlockAccessor.GetBlockEntity<BlockEntityStargate>(Pos.AddCopy(x, y, z));
                        if (foundBE != null)
                        {
                            targetBlocks.Add(foundBE);
                        }
                    }
                }
            }

            foundBE = targetBlocks.Count > 0 ? targetBlocks[0] : null;
            foreach (BlockEntityStargate be in targetBlocks)
            {
                if (Pos.ManhattenDistance(be.Pos) < Pos.ManhattenDistance(foundBE.Pos))
                {
                    foundBE = be;
                }
            }

            return foundBE as BlockEntityStargate;
        }

        /// <summary>
        /// Attempts to register this DHD to the specified gate
        /// </summary>
        /// <param name="gate"></param>
        /// <returns></returns>
        public bool RegisterToGate(BlockEntityStargate gate)
        {
            bool success = false;
            success = gate.AttemptDhdRegistration(this);

            if (!success) return false;

            connectedGate = gate;
            connectedPos = gate.Pos;

            return true;
        }

        protected void ReconnectGate(float delta)
        {
            if (connectedPos is null || connectedPos.X == -1) return;

            if (Api.World.BlockAccessor.GetChunkAtBlockPos(connectedPos) == null)
            {
                Api.Event.RegisterCallback(ReconnectGate, 5000);
                return;
            }

            BlockEntity foundEntity = Api.World.BlockAccessor.GetBlockEntity(connectedPos);

            if (foundEntity == null || foundEntity is not BlockEntityStargate) return;

            BlockEntityStargate connectedGate = foundEntity as BlockEntityStargate;
            if (connectedGate.AttemptDhdRegistration(this))
            {
                this.connectedGate = connectedGate;
                if (isActive && (connectedGate.StargateState == EnumStargateState.Idle))
                {
                    isActive = false;
                    // TODO:
                    //		Update visual state to inactive
                    //		Also need to create model
                }
            }
        }

        /// <summary>
        /// Opens the DHD GUI
        /// </summary>
        /// <param name="player">The player performing</param>
        /// <returns></returns>
        public bool OnRightClickInteraction(IPlayer player)
        {
            if (connectedGate == null) CoupleDhd();

            if (player.Entity.Controls.ShiftKey)
            {
                return doShiftInteraction(player);
            }

            return doNormalInteraction(player);
        }

        protected bool doNormalInteraction(IPlayer player)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                GuiDialogDhd dhdGui = new GuiDialogDhd("Dial Home Device", Api as ICoreClientAPI, this, false);
                dhdGui.OnAddressChanged = OnAddressChanged;
                dhdGui.OnAddressConfirmed = OnDhdConfirmed;

                dhdGui.OnClosed += () => { dhdGui.Dispose(); dhdGui = null; };

                dhdGui.TryOpen();

                return true;
            }

            return false;
        }

        protected bool doShiftInteraction(IPlayer player)
        {
            TryCloseGate();
            return false;
        }

        protected void OnAddressChanged(StargateAddress address)
        {
            if (connectedGate != null && address.IsValid)
            {
                connectedGate.TryDial(address, EnumDialSpeed.Default);
            }
        }

        protected void OnDhdConfirmed(StargateAddress address)
        {
#if DEBUG
            Api.Logger.Debug(address.ToString());
#endif
        }

        public string CoupleDhd()
        {
            BlockEntityStargate cgate = FindClosestGate();
            if (cgate != null)
            {
                RegisterToGate(cgate);
                return cgate.GateAddress.ToString();
            }

            return "";
        }

        public void DialDhd(StargateAddress address)
        {
            if (connectedGate == null)
            {
                if (CoupleDhd() == string.Empty) return;
            }

            connectedGate.TryDial(address, EnumDialSpeed.Default);
        }

        public void TryCloseGate()
        {
            if (connectedPos == null) return;
            if (connectedGate == null)
            {
                connectedGate = FindClosestGate();
                if (connectedGate == null)
                {
                    connectedPos = null;
                    return;
                }
            }

            connectedGate.TryDisconnect();
        }

        public void DisconnectGate()
        {
            if (connectedPos == null) return;

            if (Api.Side == EnumAppSide.Client)
            {
                ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(connectedPos, 2, null);
            }
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.Api = api;

            if (connectedPos is null || connectedPos.X == -1)
            {
                CoupleDhd();
            }
            else
            {
                ReconnectGate(0);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
        }

        public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
        {
            base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
            this.Api = api;

            BlockEntityStargate connectedGate = FindClosestGate();
            if (connectedGate != null && connectedGate.AttemptDhdRegistration(this))
            {
                this.connectedGate = connectedGate;
                connectedPos = connectedGate.Pos;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            if (connectedPos == null) connectedPos = new BlockPos(-1, -1, -1, Pos.dimension);
            connectedPos.X = tree.GetAsInt("gateX", -1);
            connectedPos.Y = tree.GetAsInt("gateY", -1);
            connectedPos.Z = tree.GetAsInt("gateZ", -1);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetInt("gateX", connectedGate == null ? -1 : connectedGate.Pos.X);
            tree.SetInt("gateY", connectedGate == null ? -1 : connectedGate.Pos.Y);
            tree.SetInt("gateZ", connectedGate == null ? -1 : connectedGate.Pos.Z);
        }
    }
}
