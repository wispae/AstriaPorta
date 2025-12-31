using AstriaPorta.Util;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AstriaPorta.Content;

public class BlockEntityDialHomeDevice : BlockEntity, IBlockEntityInteractable, IDialHomeDevice
{
    private static Cuboidi searchArea = null;

    private IStargate connectedGate;
    private BlockPos connectedPos;

    private bool isActive = false;

    public string LinkedAddress
    {
        get
        {
            return connectedGate?.Address.ToString() ?? Lang.Get("astriaporta:astriaporta-dhd-no-gate-linked");
        }
    }

    public bool IsGateOpen
    {
        get
        {
            return connectedGate?.State == EnumStargateState.ConnectedOutgoing || connectedGate?.State == EnumStargateState.ConnectedIncoming;
        }
    }

    public bool CanCloseGate
    {
        get
        {
            return IsGateOpen || connectedGate?.State == EnumStargateState.DialingOutgoing;
        }
    }

    public bool CanBreak
    {
        get
        {
            return connectedGate?.State != EnumStargateState.ConnectedOutgoing && connectedGate?.State != EnumStargateState.DialingOutgoing;
        }
    }

    public Cuboidi SearchArea
    {
        get
        {
            return searchArea;
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
    protected IStargate FindClosestGate()
    {
        Cuboidi searchOffsets = SearchArea;

        List<IStargate> targetBlocks = new List<IStargate>();
        IStargate foundGate = null;

        for (int x = searchOffsets.MinX; x <= searchOffsets.MaxX; x++)
        {
            for (int z = searchOffsets.MinZ; z <= searchOffsets.MaxZ; z++)
            {
                for (int y = searchOffsets.MinY; y <= searchOffsets.MaxY; y++)
                {
                    foundGate = Api.World.BlockAccessor.GetBlockEntity<StargateBase>(Pos.AddCopy(x, y, z));
                    if (foundGate != null)
                    {
                        targetBlocks.Add(foundGate);
                    }
                }
            }
        }

        foundGate = targetBlocks.Count > 0 ? targetBlocks[0] : null;
        foreach (var be in targetBlocks)
        {
            if (Pos.ManhattenDistance(be.Pos) < Pos.ManhattenDistance(foundGate.Pos))
            {
                foundGate = be;
            }
        }

        return foundGate;
    }

    /// <summary>
    /// Attempts to register this DHD to the specified gate
    /// </summary>
    /// <param name="gate"></param>
    /// <returns></returns>
    public bool RegisterToGate(IStargate gate)
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

        if (foundEntity == null || foundEntity is not IStargate) return;

        var connectedGate = foundEntity as IStargate;
        if (connectedGate.AttemptDhdRegistration(this))
        {
            this.connectedGate = connectedGate;
            if (isActive && (connectedGate.State == EnumStargateState.Idle))
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

    protected void OnAddressChanged(IStargateAddress address)
    {
        if (connectedGate != null && address.IsValid)
        {
            connectedGate.TryDial(address, EnumDialSpeed.Default);
        }
    }

    protected void OnDhdConfirmed(IStargateAddress address)
    {
#if DEBUG
        Api.Logger.Debug(address.ToString());
#endif
    }

    public string CoupleDhd()
    {
        IStargate cgate = FindClosestGate();
        if (cgate != null)
        {
            if (!RegisterToGate(cgate)) return "";
            return cgate.Address.ToString();
        }

        return "";
    }

    public void DialDhd(IStargateAddress address)
    {
        if (connectedGate == null)
        {
            if (string.IsNullOrEmpty(CoupleDhd())) return;
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

        IStargate connectedGate = FindClosestGate();
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