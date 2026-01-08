using AstriaPorta.Config;
using AstriaPorta.Systems;
using AstriaPorta.Util;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace AstriaPorta.Content;

public abstract class StargateStateManagerServer : StargateStateManagerBase
{
    protected ICoreServerAPI ServerApi;

    public StargateStateManagerServer() : base()
    {
    }

    protected bool AwaitingRemote = false;
    protected bool IsRemoteNotified = false;
    protected float TickAvg = 0;
    protected int TickTimer = 0;
    protected long TimeoutCallbackId = -1;

    protected bool StableConnection = false;

    public override void Initialize(StargateBase gate)
    {
        base.Initialize(gate);

        ServerApi = gate.Api as ICoreServerAPI;

        var gmInstance = StargateManagerSystem.GetInstance(gate.Api);

        if (!IsRegisteredToGateManager)
        {
            gmInstance.RegisterLoadedGate(Gate, Gate.Pos);
            IsRegisteredToGateManager = true;
        }

        if (State == EnumStargateState.DialingOutgoing || State == EnumStargateState.ConnectedIncoming)
        {
            if (!IsForceLoaded)
            {
                gmInstance.ForceLoadChunk(Gate.Pos);
                IsForceLoaded = true;
            }

            if (State == EnumStargateState.DialingOutgoing && !IsTickListenerRegistered)
            {
                TryRegisterTickListener(OnTick, 20);
            }
        }
        else if (State == EnumStargateState.ConnectedOutgoing && !IsTickListenerRegistered)
        {
            if (!Gate.IsRemoteLoaded)
            {
                RemoteLoadTimeout = StargateConfig.Loaded.MaxTimeoutSeconds;
                gmInstance.LoadRemoteGate(Gate.DialingAddress, Gate);
            }
            TryRegisterTickListener(OnTick, 20);
        }

        if (State != EnumStargateState.Idle)
        {
            if (!IsForceLoaded)
            {
                gmInstance.ForceLoadChunk(Gate.Pos);
                IsForceLoaded = true;
            }
        }

        SyncStateToClients();
    }

    public override void AcceptConnection(byte activeChevrons)
    {
        State = EnumStargateState.ConnectedIncoming;
        TimeOpen = 0f;
        ActiveChevrons = activeChevrons;
        Gate.RegisterDelayedCallback((t) =>
        {
            Gate.ApplyVortexDestruction();
        }, 750);

        SyncStateToClients();
    }

    public override void AcceptIncomingConnection(IStargate caller)
    {
        State = EnumStargateState.DialingIncoming;
        if (!IsForceLoaded)
        {
            StargateManagerSystem.GetInstance(Api).RegisterLoadedGate(Gate, Gate.Pos);
            IsForceLoaded = true;
        }

        SyncStateToClients();
    }

    protected GateStatePacket AssembleStatePacket(uint extraFlags = 0)
    {
        return new GateStatePacket
        {
            ActiveChevrons = ActiveChevrons,
            CurrentAngle = CurrentAngle,
            CurrentGlyph = CurrentGlyph,
            CurrentGlyphIndex = CurrentAddressIndex,
            RemoteAddressBits = DialingAddress?.AddressBits ?? 0,
            RotateCW = RotateCW,
            State = (int)State,
            DialType = (int)CurrentDialSpeed,
            Flags = extraFlags,
        };
    }

    protected virtual void ConfigureDialingSettings()
    {
        State = EnumStargateState.DialingOutgoing;
        CurrentAddressIndex = 0;
        ActiveChevrons = 0;
        RotateCW = true;
        NextGlyph = DialingAddress.AddressCoordinates.Glyphs[CurrentAddressIndex];
        RemoteLoadTimeout = StargateConfig.Loaded.MaxTimeoutSeconds;
    }

    public override void ForceDisconnect(bool notifyRemote = true)
    {
        State = EnumStargateState.Idle;
        ActiveChevrons = 0;
        StableConnection = false;
        TimeOpen = 0f;

        var remoteGate = Gate.GetRemoteGate();

        if (notifyRemote && remoteGate != null)
        {
            remoteGate.ForceDisconnect(false);
        }

        if (IsForceLoaded)
        {
            StargateManagerSystem.GetInstance(Api).ReleaseChunk(Gate.Pos);
            IsForceLoaded = false;
        }

        UnregisterTickListener();
        UnregisterDelayedCallback();

        if (State == EnumStargateState.ConnectedIncoming || State == EnumStargateState.ConnectedOutgoing)
        {
            Gate.SoundManager?.Play(EnumGateSoundLocation.Break);
        }

        SyncStateToClients();
    }

    protected Entity[] GetCollidingEntities()
    {
        var area = StargateVolumeManager.GetTeleportArea(Gate.Block.Shape.rotateY);
        Cuboidf[] areaCol = [area];

        var entities = Api.World.GetIntersectingEntities(Gate.Pos, areaCol, (e) => true);
        return entities;
    }

    protected void HandleDialPacket(byte[] data)
    {
        GateStatePacket packet = SerializerUtil.Deserialize<GateStatePacket>(data);
        StargateAddress address = new StargateAddress();
        address.FromBits(packet.RemoteAddressBits, Gate.Api);
        Gate.TryDial(address, (EnumDialSpeed)packet.DialType);
    }

    protected void HandleAbortPacket()
    {
        Gate.TryDisconnect();
    }

    protected void HandleCamoUpdatePacket(byte[] data)
    {
        TreeAttribute tree = new();
        tree.FromBytes(data);
        ItemStack stack;

        for (int i = 0; i < Gate.Inventory.Count; i++)
        {
            if (tree.HasAttribute("stack" + i))
            {
                stack = tree.GetItemstack("stack" + i);
                stack.ResolveBlockOrItem(Gate.Api.World);
                Gate.Inventory[i].Itemstack = stack;
            }
            else
            {
                if (Gate.Inventory[i].Itemstack != null)
                {
                    Gate.Inventory[i].TakeOutWhole();
                }
            }
        }

        if (tree.HasAttribute("quickdialstate"))
        {
            Gate.UseQuickDial = tree.GetBool("quickdialstate");
        }
    }

    /// <summary>
    /// When the gate fails to activate for whatever reason
    /// </summary>
    /// <param name="delta"></param>
    protected void OnConnectionFailure(float delta)
    {
        State = EnumStargateState.Idle;
        ActiveChevrons = 0;

        var remoteGate = Gate.GetRemoteGate();
        if (remoteGate != null)
        {
            remoteGate.TryDisconnect();
        }

        if (IsForceLoaded)
        {
            StargateManagerSystem.GetInstance(Api).ReleaseChunk(Gate.Pos);
            IsForceLoaded = false;
        }

        Gate.SoundManager?.Play(EnumGateSoundLocation.Fail);
        SyncStateToClients();
    }

    protected void OnConnectionSuccess()
    {
        State = EnumStargateState.ConnectedOutgoing;
        TimeOpen = 0f;
        UnregisterTickListener();
        TryRegisterTickListener(OnTick, 20);
        // Gate.VisualManager.SpawnActivationParticles();

        Gate.RegisterDelayedCallback((t) =>
        {
            Gate.ApplyVortexDestruction();
        }, Gate.SoundManager?.VortexSoundDelay ?? 750);

        var remoteGate = Gate.GetRemoteGate();
        if (remoteGate != null)
        {
            remoteGate.AcceptConnection(ActiveChevrons);
        }

        SyncStateToClients();
    }

    protected void OnGlyphActivated(float delta)
    {
        AwaitingChevronAnimation = false;
        ActiveChevrons++;

        if (CurrentDialSpeed == EnumDialSpeed.Fast)
        {
            Gate.SoundManager?.Play(EnumGateSoundLocation.Lock);
        }

        IStargate remoteGate = Gate.GetRemoteGate();

        if (CurrentAddressIndex == ((int)DialingAddress.AddressLength) - 1)
        {
            if (DialingAddress.AddressBits == Gate.Address.AddressBits)
            {
                Gate.ForceDisconnect();
                return;
            }

            // final glyph activated
            if (remoteGate == null)
            {
                GateLogger.LogAudit(LogLevel.Info, "Final chevron locked, but remote gate not available yet");
                ActiveChevrons--;
                SyncStateToClients();
                if (TimeoutCallbackId == -1)
                {
                    TimeoutCallbackId = Gate.RegisterDelayedCallback(OnRemoteTimeout, (int)(StargateConfig.Loaded.MaxTimeoutSeconds * 1000));
                }

                AwaitingRemote = true;
                TryRegisterDelayedCallback(OnTick, 20);

                return;
            }

            if (!Gate.IsIrisClear() || !remoteGate.IsIrisClear())
            {
                Gate.ForceDisconnect();
                return;
            }

            OnConnectionSuccess();
            return;
        }

        if (remoteGate != null)
        {

            if (!IsRemoteNotified)
            {
                remoteGate.EvaluateIncomingConnection(Gate);
                IsRemoteNotified = true;
            }

            UpdateRemoteChevrons();
        }

        ContinueToNextIndex();

        // assume 1000ms required for rotate start sound to be safe
        var angleDiff = NextGlyph * GlyphAngle - CurrentAngle;
        if (angleDiff < 0) angleDiff += 360;

        if (RotateCW) angleDiff = 360 - angleDiff;
        if (angleDiff/RotationDegPerSecond >= 1f)
        {
            Gate.SoundManager?.Play(EnumGateSoundLocation.RotateStart);
        }
    }

    protected override void OnGlyphReached()
    {
        UnregisterTickListener();
        if (State == EnumStargateState.Idle) return;

        AwaitingChevronAnimation = true;
        TryRegisterDelayedCallback(OnGlyphActivated, (CurrentDialSpeed == EnumDialSpeed.Slow) ? 2000 : 1000);
    }

    protected void OnRemoteTimeout(float delta)
    {
        TimeoutCallbackId = -1;
        if (State == EnumStargateState.DialingOutgoing)
        {
            OnConnectionFailure(delta);
            // Gate.TryDisconnect();
            return;
        }
    }

    public override void ProcessStatePacket(EnumStargatePacketType packetType, byte[] data)
    {
        switch (packetType)
        {
            case EnumStargatePacketType.Dial:
                HandleDialPacket(data);
                break;
            case EnumStargatePacketType.Abort:
                HandleAbortPacket();
                break;
            case EnumStargatePacketType.CamoUpdate:
                HandleCamoUpdatePacket(data);
                break;
        }
    }

    /// <summary>
    /// Checks for, and teleports entities that collide with the event horizon<br/>
    /// Teleports and rotates players<br/>
    /// Teleports, preserves and rotates momentum for other entities
    /// </summary>
    protected void ProcessCollidingEntities()
    {
        Entity[] travelers = GetCollidingEntities();
        if (travelers.Length == 0) return;

        var remoteGate = Gate.GetRemoteGate() as StargateBase;
        if (remoteGate == null) return;

        float originYaw;
        float rotateLocal = Gate.Block.Shape.rotateY;
        float rotateRemote = remoteGate.Block.Shape.rotateY;
        float rotateRadLocal = rotateLocal * GameMath.DEG2RAD;
        float thetaf = ((rotateRemote - rotateLocal + 540) % 360) * GameMath.DEG2RAD;
        float costhetaf = MathF.Cos(thetaf);
        float sinthetaf = MathF.Sin(thetaf);

        double offsetX, offsetY, offsetZ, offsetOriginX, offsetOriginZ;

        foreach (Entity traveler in travelers)
        {
            originYaw = (traveler.SidedPos.Yaw + thetaf) % GameMath.TWOPI;

            offsetY = traveler.Pos.Y - Gate.Pos.Y;
            offsetOriginX = Gate.Pos.X - traveler.Pos.X + 0.5f;
            offsetOriginZ = Gate.Pos.Z - traveler.Pos.Z + 0.5f;

            offsetX = offsetOriginX * costhetaf + offsetOriginZ * sinthetaf;
            offsetZ = offsetOriginZ * costhetaf - offsetOriginX * sinthetaf;

            if (Gate.Block.Shape.rotateY % 180 == 0)
            {
                if (remoteGate.Block.Shape.rotateY % 180 == 0) offsetX *= -1;
                else offsetZ *= -1;
            }
            else
            {
                if (remoteGate.Block.Shape.rotateY % 180 == 0) offsetZ *= -1;
                else offsetX *= -1;
            }

            traveler.SidedPos.Yaw = originYaw;
            traveler.SidedPos.HeadYaw = originYaw;

            traveler.TeleportToDouble(remoteGate.Pos.X + offsetX + 0.5f, remoteGate.Pos.Y + offsetY, remoteGate.Pos.Z + offsetZ + 0.5f);

            UpdateTravelerMotion(traveler, originYaw, rotateLocal, rotateRemote, remoteGate);
        }

        if (travelers.Length > 0)
        {
            Gate.SoundManager?.Play(EnumGateSoundLocation.Enter);
            remoteGate.SoundManager?.Play(EnumGateSoundLocation.Enter);
        }
    }

    private void UpdateTravelerMotion(Entity traveler, float originYaw, float rotatelocal, float rotateremote, StargateBase remoteGate)
    {
        if (rotatelocal % 180 == 0)
        {
            rotatelocal = (rotatelocal + 180) % 360;
        }
        if (rotateremote % 180 == 0)
        {
            rotateremote = (rotateremote + 180) % 360;
        }

        rotatelocal = (rotatelocal + 270) % 360;
        rotateremote = (rotateremote + 270) % 360;
        float thetaf = (Math.Abs(rotatelocal - rotateremote) + 180) % 360;
        float costhetaf = MathF.Cos(thetaf * GameMath.DEG2RAD);
        float sinthetaf = MathF.Sin(thetaf * GameMath.DEG2RAD);

        float motionX, motionY, motionZ;

        if (traveler is EntityPlayer)
        {
            ((EntityPlayer)traveler).BodyYawServer = originYaw;
            PlayerYawPacket p = new()
            {
                EntityId = traveler.EntityId,
                Yaw = originYaw
            };

            Gate.RegisterDelayedCallback((t) =>
            {
                if (remoteGate == null) return;
                (remoteGate.StateManager as StargateStateManagerServer)?.SyncStateToClients();
                (Api as ICoreServerAPI).Network.BroadcastBlockEntityPacket(remoteGate.Pos, (int)EnumStargatePacketType.PlayerYaw, p);

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

    /// <summary>
    /// Sends the yaw packet that rotates the player camera on their end<br/>
    /// Fixes the player facing the wrong direction after teleporting
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="yaw"></param>
    protected void SendYawPacket(long entityId, float yaw)
    {
        PlayerYawPacket packet = new PlayerYawPacket
        {
            EntityId = entityId,
            Yaw = yaw,
        };
        ServerApi.Network.BroadcastBlockEntityPacket(Gate.Pos, (int)EnumStargatePacketType.PlayerYaw, packet);
    }

    protected void SetActiveChevrons(byte activeChevrons)
    {
        ActiveChevrons = activeChevrons;
        SyncStateToClients();
    }

    /// <summary>
    /// Synchronizes server state only to specified client
    /// </summary>
    /// <param name="player"></param>
    protected virtual void SyncStateToPlayer(IPlayer player, uint extraFlags = 0)
    {
        if (player.ClientId == 0) return;

        IServerPlayer splayer = ServerApi.Server.Players.FirstOrDefault(
            (sp) => sp.ClientId == player.ClientId, null);

        if (splayer == null || splayer.ConnectionState != EnumClientState.Connected) return;

        GateStatePacket packet = AssembleStatePacket(extraFlags);
        ServerApi.Network.SendBlockEntityPacket(splayer, Gate.Pos, (int)EnumStargatePacketType.State, packet);
    }

    /// <summary>
    /// Synchronizes server state to all clients
    /// </summary>
    protected virtual void SyncStateToClients(uint extraFlags = 0)
    {
        GateStatePacket packet = AssembleStatePacket(extraFlags);
        ServerApi.Network.BroadcastBlockEntityPacket(Gate.Pos, (int)EnumStargatePacketType.State, packet);
    }

    protected virtual void TickConnectedOutgoing(float delta)
    {
        TickTimer++;
        TickAvg += delta;
        if (TickTimer > 60)
        {
            TickTimer = 0;
            TickAvg = 0;
        }

        if (!StableConnection)
        {
            var remoteGate = Gate.GetRemoteGate();
            if (remoteGate == null)
            {
                // todo: shouldn't remoteloadtimeout be in milliseconds???
                RemoteLoadTimeout -= delta;

                if (RemoteLoadTimeout <= 0)
                {
                    Gate.TryDisconnect();
                }
            }

            StableConnection = true;
        }

        ProcessCollidingEntities();
        TimeOpen += delta;

        if (TimeOpen > StargateConfig.Loaded.GetMaxConnectionDuration(Gate.Type))
        {
            GateLogger.LogAudit(LogLevel.Info, "Wormhole has been open for max duration, shutting down connection");
            Gate.TryDisconnect();
        }
    }

    protected virtual void TickDialingOutgoing(float delta)
    {
        if (CurrentDialSpeed == EnumDialSpeed.Slow)
        {
            NextAngle(delta);
        }
        else
        {
            OnGlyphReached();
        }
    }

    public override bool TryDial(IStargateAddress address, EnumDialSpeed speed)
    {
        GateLogger.LogAudit(LogLevel.Info, $"Started dial to {address} with coordinates ({address.AddressCoordinates.X},{address.AddressCoordinates.Y},{address.AddressCoordinates.Z})");

        // RotationDegPerSecond = StargateConfig.Loaded.DialSpeedDegreesPerSecondMilkyway;
        if (State != EnumStargateState.Idle)
        {
            return false;
        }

        if (speed == EnumDialSpeed.Default)
        {
            speed = (UseQuickDial && StargateConfig.Loaded.AllowQuickDial) ? EnumDialSpeed.Fast : EnumDialSpeed.Slow;
        }
        CurrentDialSpeed = speed;

        Gate.ReleaseRemoteGate();
        DialingAddress = address;

        if (Gate.WillDialSucceed(address))
        {
            StargateManagerSystem.GetInstance(Api).LoadRemoteGate(address, Gate);
            if (!IsForceLoaded)
            {
                StargateManagerSystem.GetInstance(Api).ForceLoadChunk(Gate.Pos);
                IsForceLoaded = true;
            }
        }

        ConfigureDialingSettings();

        UnregisterTickListener();
        TryRegisterTickListener(OnTick, 20);

        SyncStateToClients();
        return true;
    }

    public virtual void TryFinishConnection()
    {
        IStargate remote = Gate.GetRemoteGate();
        if (remote == null) return;

        if (TimeoutCallbackId != -1)
        {
            Gate.UnregisterDelayedCallback(TimeoutCallbackId);
        }

        ActiveChevrons++;
        OnConnectionSuccess();
    }

    protected void UpdateRemoteChevrons()
    {
        var remoteStateManager = Gate.GetRemoteGate().StateManager as StargateStateManagerServer;
        if (remoteStateManager == null) return;

        remoteStateManager.SetActiveChevrons(ActiveChevrons);
    }
}