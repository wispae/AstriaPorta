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

    public enum EnumConnectionSpeed
    {
        Fast,
        Slow,
        Instant
    }

	public enum EnumStargatePacketType
	{
		State,
		Animation,
		PlayerYaw,
		Dial,
		Abort,
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
        public byte CurrentGlyph { get; set; }
        [ProtoMember(3)]
        public byte ActiveChevrons { get; set; }
        [ProtoMember(4)]
        public int CurrentGlyphIndex { get; set; }
        [ProtoMember(5)]
        public int ConnectionSpeed { get; set; }
        [ProtoMember(6)]
        public bool RotateCW { get; set; }
		[ProtoMember(7)]
		public bool Rotating { get; set; }
        [ProtoMember(8)]
        public float CurrentAngle { get; set; }
        [ProtoMember(9)]
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
		public IStargateAddress AddressShort { get; }
		public IStargateAddress AddressMedium { get; }
		public IStargateAddress AddressLong { get; }
		public IStargateAddress DialingAddress { get; set; }

		public EnumStargateType StargateType { get; }

		public EnumStargateState StargateState { get; }

        // public float TimeOpen { get; set; }

        // public int EnergyBuffer { get; set; }

        // public (int X, int Y, int Z) GatePos { get; }
    }
}
