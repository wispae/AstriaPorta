using AstriaPorta.Config;
using AstriaPorta.Util;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace AstriaPorta.Content;

public abstract class StargateStateManagerClient : StargateStateManagerBase
{
    protected ICoreClientAPI ClientAPI;

    public StargateStateManagerClient() : base()
    {
    }

    public override void Initialize(StargateBase gate)
    {
        base.Initialize(gate);

        ClientAPI = gate.Api as ICoreClientAPI;

        if (State == EnumStargateState.DialingOutgoing || State == EnumStargateState.ConnectedOutgoing)
        {
            TryRegisterTickListener(OnTick, 20);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        Gate.SoundManager.Dispose();
        Gate.VisualManager.Dispose();
    }

    public override void AcceptConnection(byte activeChevrons)
    {
        throw new NotImplementedException();
    }

    public override void AcceptIncomingConnection(IStargate caller)
    {
        throw new NotImplementedException();
    }

    protected void DialServerGate(IStargateAddress address, EnumDialSpeed speed)
    {
        GateStatePacket packet = new GateStatePacket
        {
            RemoteAddressBits = address.AddressBits,
            DialType = (int)speed
        };

        ClientAPI.Network.SendBlockEntityPacket(Gate.Pos, (int)EnumStargatePacketType.Dial, packet);
    }

    protected abstract void OnGlyphDown(float _);

    protected override void OnGlyphReached()
    {
        UnregisterTickListener();
        if (State == EnumStargateState.Idle) return;

        AwaitingChevronAnimation = true;

        CallbackId = Gate.RegisterDelayedCallback(OnGlyphDown, 1000);

        Gate.SoundManager.PauseRotateSound();
        AwaitingChevronAnimation = true;

        if (CurrentDialSpeed == EnumDialSpeed.Fast) return;

        Gate.VisualManager.ActivateLockChevron();
        Gate.SoundManager.Play(EnumGateSoundLocation.Lock);
        Gate.RegisterDelayedCallback((t) =>
        {
            Gate.SoundManager.Play(EnumGateSoundLocation.Release);
        }, 500);

        ContinueToNextIndex();
    }

    /// <summary>
    /// Processes a packet sent from the server and redirects to the correct handler
    /// </summary>
    /// <param name="packetType"></param>
    /// <param name="data"></param>
    public override void ProcessStatePacket(EnumStargatePacketType packetType, byte[] data)
    {
        switch (packetType)
        {
            case EnumStargatePacketType.State:
                ProcessStatePacket(data);
                break;
            case EnumStargatePacketType.PlayerYaw:
                ProcessYawPacket(data);
                break;
        }
    }

    /// <summary>
    /// Evaluates a received state packet and updates the state of the gate accordingly
    /// </summary>
    /// <param name="data"></param>
    protected void ProcessStatePacket(byte[] data)
    {
        GateStatePacket packet = SerializerUtil.Deserialize<GateStatePacket>(data);
        EnumStargateState newState = (EnumStargateState)packet.State;
        DialingAddress = new StargateAddress();
        DialingAddress.FromBits(packet.RemoteAddressBits, Gate.Api);

#if DEBUG
        Gate.Api.Logger.Debug($"Received server packet, new state: {newState}");
#endif

        RotateCW = packet.RotateCW;
        ActiveChevrons = packet.ActiveChevrons;
        CurrentGlyph = packet.CurrentGlyph;
        CurrentAddressIndex = packet.CurrentGlyphIndex;
        CurrentAngle = packet.CurrentAngle;
        CurrentDialSpeed = (EnumDialSpeed)packet.DialType;
        NextGlyph = DialingAddress.AddressCoordinates.Glyphs[CurrentAddressIndex];

        switch (newState)
        {
            case EnumStargateState.Idle:
                TransitionToIdle();
                break;
            case EnumStargateState.DialingIncoming:
                TransitionToDialingIncoming();
                break;
            case EnumStargateState.DialingOutgoing:
                TransitionToDialingOutgoing(packet.Flags);
                break;
            case EnumStargateState.ConnectedIncoming:
                TransitionToConnectedIncoming();
                break;
            case EnumStargateState.ConnectedOutgoing:
                TransitionToConnectedOutgoing();
                break;
        }

        bool shouldUpdateChevronGlow = true;
        if (State != EnumStargateState.Idle && newState == EnumStargateState.Idle)
        {
            shouldUpdateChevronGlow = false;
        }

        State = newState;
        if (shouldUpdateChevronGlow)
        {
            Gate.VisualManager.UpdateChevronGlow(ActiveChevrons);
        }
        Gate.VisualManager.UpdateRendererState(CurrentAngle);
    }

    /// <summary>
    /// Rotates the local player's camera yaw
    /// </summary>
    /// <param name="data"></param>
    protected void ProcessYawPacket(byte[] data)
    {
        PlayerYawPacket packet = SerializerUtil.Deserialize<PlayerYawPacket>(data);

        Gate.VisualManager.OnYawPacket(ActiveChevrons, CurrentAngle);

        Entity ent = Gate.Api.World.GetEntityById(packet.EntityId);
        if (ent == null || !(ent is EntityPlayer)) return;

        EntityPlayer ep = ent as EntityPlayer;
        if ((Gate.Api as ICoreClientAPI).World.Player.Entity.EntityId != ep.EntityId) return;

        (Gate.Api as ICoreClientAPI).World.Player.CameraYaw = packet.Yaw;
    }

    protected virtual void TickConnectedOutgoing(float delta)
    {
        TimeOpen += delta;
        if (TimeOpen >= MaxConnectionDuration)
        {
            TimeOpen = MaxConnectionDuration;
        }
    }

    protected virtual void TickDialingOutgoing(float delta)
    {
        if (CurrentDialSpeed == EnumDialSpeed.Slow)
        {
            Gate.SoundManager.StartRotateSound();
            NextAngle(delta);
        }
        else
        {
            OnGlyphReached();
        }
        Gate.VisualManager.UpdateRendererState(CurrentAngle);
    }

    /// <summary>
    /// Manages transitions into the dialingIncoming state
    /// </summary>
    protected void TransitionToDialingIncoming()
    {
        Gate.VisualManager.UpdateChevronGlow(ActiveChevrons);
    }

    /// <summary>
    /// Manages transitions into the dialingOutgoing state
    /// </summary>
    protected virtual void TransitionToDialingOutgoing(uint extraFlags)
    {
        if (State == EnumStargateState.Idle)
        {
            // start dialing outgoing wormhole
            // register OnTickClient if empty
        }

        // always attempt to re-register as it needs to be restarted when the server
        // signals that the glyph activation is completed

        TryRegisterTickListener(OnTick, 20);
    }

    /// <summary>
    /// Manages transitions into the connectedIncoming state
    /// </summary>
    protected void TransitionToConnectedIncoming()
    {
        if (State == EnumStargateState.ConnectedIncoming) return;

        Gate.RegisterDelayedCallback((t) =>
        {
            Gate.VisualManager.ActivateHorizon();
            Gate.SoundManager.Play(EnumGateSoundLocation.Vortex);
        }, 750);
    }

    /// <summary>
    /// Manages transitions into the connectedOutgoing state
    /// </summary>
    protected void TransitionToConnectedOutgoing()
    {
        if (State == EnumStargateState.ConnectedOutgoing) return;

        Gate.SoundManager.Play(EnumGateSoundLocation.Warning);

        // play activation sound
        // register OnTick if empty
        if (TickListenerId == -1) TickListenerId = Gate.RegisterGameTickListener(OnTick, 20, 750);
        Gate.RegisterDelayedCallback((t) =>
        {
            Gate.VisualManager.ActivateHorizon(true);
            Gate.SoundManager.Play(EnumGateSoundLocation.Vortex);
        }, Gate.SoundManager.VortexSoundDelay);
    }

    /// <summary>
    /// Manages transitions into the idle state
    /// </summary>
    protected void TransitionToIdle()
    {
        if (State == EnumStargateState.Idle) return;
        ICoreClientAPI capi = Gate.Api as ICoreClientAPI;
        TimeOpen = 0f;

        switch (State)
        {
            case EnumStargateState.DialingIncoming:
                break;

            case EnumStargateState.ConnectedIncoming:
            case EnumStargateState.ConnectedOutgoing:
                Gate.SoundManager?.Play(EnumGateSoundLocation.Break);
                break;

            default:
                Gate.SoundManager.StopAllSounds();
                break;
        }

        if (State == EnumStargateState.ConnectedIncoming || State == EnumStargateState.ConnectedOutgoing)
        {
            Gate.RegisterDelayedCallback((t) => Gate.VisualManager.SpawnDeactivationParticles(), 1500);
            Gate.RegisterDelayedCallback((t) => Gate.VisualManager.DeactivateHorizon(), 2000);
            Gate.RegisterDelayedCallback((t) => Gate.VisualManager.UpdateChevronGlow(0), 2000);
        }
        else
        {
            Gate.VisualManager.UpdateChevronGlow(0);
        }

            UnregisterTickListener();
        UnregisterDelayedCallback();
    }

    public override bool TryDial(IStargateAddress address, EnumDialSpeed speed)
    {
        // RotationDegPerSecond = StargateConfig.Loaded.DialSpeedDegreesPerSecondMilkyway;
        DialServerGate(address, speed);
        return true;
    }
}