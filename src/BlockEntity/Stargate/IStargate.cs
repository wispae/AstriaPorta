using ProtoBuf;
using System.Collections.Generic;
using System.Text;
using AstriaPorta.Util;

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
        public byte CurrentGlyph { get; set; }
        [ProtoMember(2)]
        public int NextGlyphIndex { get; set; }
        [ProtoMember(3)]
        public byte ActiveChevrons { get; set; }
        [ProtoMember(4)]
        public int GateState { get; set; }
        [ProtoMember(5)]
        public int ConnectionSpeed { get; set; }
        [ProtoMember(6)]
        public bool RotateCW { get; set; }
    }

    [ProtoContract]
    public struct GateStatePacket
    {
        [ProtoMember(1)]
        public int State { get; set; }
        [ProtoMember(2)]
        public int DialType { get; set; }
        [ProtoMember(3)]
        public byte CurrentGlyph { get; set; }
        [ProtoMember(4)]
        public byte ActiveChevrons { get; set; }
        [ProtoMember(5)]
        public int CurrentGlyphIndex { get; set; }
        [ProtoMember(6)]
        public int ConnectionSpeed { get; set; }
        [ProtoMember(7)]
        public bool RotateCW { get; set; }
        [ProtoMember(8)]
        public bool Rotating { get; set; }
        [ProtoMember(9)]
        public float CurrentAngle { get; set; }
        [ProtoMember(10)]
        public ulong RemoteAddressBits { get; set; }
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
        public StargateAddress GateAddress { get; }

        public StargateAddress DialingAddress { get; }

        public EnumStargateType StargateType { get; }

        public EnumStargateState StargateState { get; }

        public void TryDial(StargateAddress address, EnumDialSpeed dialType);

        public void TryDisconnect();

        // public float TimeOpen { get; set; }

        // public int EnergyBuffer { get; set; }

        // public (int X, int Y, int Z) GatePos { get; }
    }
}
