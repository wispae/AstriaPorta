using ProtoBuf;
using AstriaPorta.Util;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content
{
    public enum EnumStargateType
    {
        Milkyway,
        Pegasus,
        Destiny
    }

    public enum EnumStargateState
    {
        Idle,
        DialingIncoming,
        DialingOutgoing,
        ConnectedIncoming,
        ConnectedOutgoing
    }

    public enum EnumDialSpeed
    {
        Fast,
        Slow,
        Instant,
        Default
    }

    // offset numbers by 1FFFF because the game uses some
    // packet ids itself for some purposes
    // (looking at you, InventoryNetworkUtil)
    public enum EnumStargatePacketType
    {
        State = 0x1FFFF,
        Animation = 0x2FFFF,
        PlayerYaw = 0x3FFFF,
        Dial = 0x4FFFF,
        Abort = 0x5FFFF,
        CamoUpdate = 0x6FFFF
    }

    [ProtoContract]
    public struct GateAnimSyncPacket
    {
        [ProtoMember(1)]
        public byte ActiveChevrons { get; set; }
        [ProtoMember(2)]
        public byte CurrentGlyph { get; set; }
        [ProtoMember(3)]
        public int ConnectionSpeed { get; set; }
        [ProtoMember(4)]
        public int GateState { get; set; }
        [ProtoMember(5)]
        public int NextGlyphIndex { get; set; }
        [ProtoMember(6)]
        public bool RotateCW { get; set; }
    }

    [ProtoContract]
    public struct GateStatePacket
    {
        [ProtoMember(1)]
        public byte ActiveChevrons { get; set; }
        [ProtoMember(2)]
        public float CurrentAngle { get; set; }
        [ProtoMember(3)]
        public byte CurrentGlyph { get; set; }
        [ProtoMember(4)]
        public int CurrentGlyphIndex { get; set; }
        [ProtoMember(5)]
        public int ConnectionSpeed { get; set; }
        [ProtoMember(6)]
        public int DialType { get; set; }
        [ProtoMember(7)]
        public ulong RemoteAddressBits { get; set; }
        [ProtoMember(8)]
        public bool RotateCW { get; set; }
        [ProtoMember(9)]
        public bool Rotating { get; set; }
        [ProtoMember(10)]
        public int State { get; set; }
    }

    [ProtoContract]
    public struct PlayerYawPacket
    {
        [ProtoMember(1)]
        public long EntityId { get; set; }
        [ProtoMember(2)]
        public float Yaw { get; set; }
    }

    public interface IStargate
    {
        /// <summary>
        /// This gate's address
        /// </summary>
        public IStargateAddress Address { get; }

        /// <summary>
        /// Whether the gate can currently be broken by normal means
        /// </summary>
        public bool CanBreak { get; }

        /// <summary>
        /// The address of the currently connected gate
        /// </summary>
        public IStargateAddress ConnectedAddress { get; }

        /// <summary>
        /// The address currently being dialed
        /// </summary>
        public IStargateAddress DialingAddress { get; }

        /// <summary>
        /// The number of unique glyphs on the gate's ring
        ///     Milkyway => 39
        ///     Pegasus  => 36
        ///     Destiny  => 36
        /// </summary>
        public int GlyphLength { get; }

        public bool IsForceLoaded { get; set; }

        public BlockPos Pos { get; set; }

        public BlockPos RemotePosition { get; set; }

        /// <summary>
        /// The current state of the gate
        /// </summary>
        public EnumStargateState State { get; }

        /// <summary>
        /// The type of this gate
        /// </summary>
        public EnumStargateType Type { get; }

        /// <summary>
        /// Whether the gate should use quick dial or not (dial without rotating)
        /// </summary>
        public bool UseQuickDial { get; set; }

        /// <summary>
        /// The manager that handles most visual stuff
        /// </summary>
        public StargateVisualManager VisualManager { get; }

        /// <summary>
        /// The sound manager for this gate
        /// </summary>
        public StargateSoundManager SoundManager { get; }

        /// <summary>
        /// The manager that handles state and state transitions
        /// </summary>
        public StargateStateManagerBase StateManager { get; }


        /// <summary>
        /// Accepts a connection and sets the active number of chevrons<br/>
        /// This is used because a gate may get dialed via a 7, 8, or 9-chevron adress
        /// </summary>
        /// <param name="activeChevrons"></param>
        /// <returns></returns>
        public void AcceptConnection(byte activeChevrons);

        /// <summary>
        /// Attempts to register the provided DHD as the controlling DHD for this gate
        /// </summary>
        /// <param name="dhd"></param>
        /// <returns>True when registration succeeds, else false</returns>
        public bool AttemptDhdRegistration(IDialHomeDevice dhd);

        /// <summary>
        /// Checks if the gate can dial the provided address<br/>
        /// Does not take the receiving gate's state into account
        /// </summary>
        /// <remarks>
        /// Will never attempt to load any chunks
        /// </remarks>
        /// <param name="toAddress"></param>
        /// <returns></returns>
        public bool CanDial(IStargateAddress toAddress);

        /// <summary>
        /// Evaluates an incoming connection from a gate and checks if it can connect.<br/>
        /// If so, it will update it's own state accordingly.
        /// </summary>
        /// <param name="fromGate"></param>
        /// <returns></returns>
        public bool EvaluateIncomingConnection(IStargate fromGate);

        /// <summary>
        /// Forcibly disconnects the gate, even if normally you wouldn't be able to
        /// </summary>
        public void ForceDisconnect(bool notifyRemote = true);

        /// <summary>
        /// In order to not keep a reference to the remote gate at all times,
        /// this function retrieves and caches the reference<br/>
        /// It is the responsibility of the caller to set this reference
        /// back to null when they're done with it at the end of the tick
        /// </summary>
        /// <returns></returns>
        IStargate? GetRemoteGate();

        /// <summary>
        /// Checks if the iris is clear for dialing
        /// </summary>
        /// <returns></returns>
        public bool IsIrisClear();

        /// <summary>
        /// Releases the temporary reference to the remote gate<br/>
        /// Use sparingly if possible
        /// </summary>
        void ReleaseRemoteGate();

        /// <summary>
        /// Attempts to disconnect the gate when a connection has been established<br/>
        /// Stops the dialing when the gate is dialing and no connnection has been made yet<br/>
        /// Acts as a no-op if the gate is idle
        /// </summary>
        /// <returns></returns>
        public void TryDisconnect();

        /// <summary>
        /// Attempts to dial the internally set dialing address<br/>
        /// Uses the gate's internally set dialing speed
        /// </summary>
        /// <returns></returns>
        public bool TryDial();

        /// <summary>
        /// Attempts to dial the provided address<br/>
        /// Uses the gate's internally set dialing speed
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool TryDial(IStargateAddress address);

        /// <summary>
        /// Attempts to dial the provided address at the given dialing speed.<br/>
        /// Force loads the receiving gate chunk if it exists.<br/>
        /// Dials the address and fails when address is invalid or destination gate does not exist
        /// </summary>
        /// <remarks>
        /// Always returns <c>true</c> on the client-side
        /// </remarks>
        /// <param name="address"></param>
        /// <param name="dialSpeed"></param>
        /// <returns></returns>
        public bool TryDial(IStargateAddress address, EnumDialSpeed dialSpeed);
    }
}
