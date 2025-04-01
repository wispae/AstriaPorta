using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Datastructures;
using ProtoBuf;

namespace AstriaPorta.Util
{
	public enum EnumAddressLength
	{
		Short = 7,
		Medium = 8,
		Long = 9
	}

	[ProtoContract]
	public struct AddressCoordinates
	{
		[ProtoMember(1)]
		public int X { get; set; }
		[ProtoMember(2)]
		public int Y { get; set; }
		[ProtoMember(3)]
		public int Z { get; set; }
		[ProtoMember(4)]
		public int Dimension { get; set; }
		[ProtoMember(5)]
		public ulong AddressBits { get; set; }
		[ProtoMember(6)]
		public byte[] Glyphs { get; set; }
		[ProtoMember(7)]
		public bool IsValid { get; set; }
	}

	public interface IStargateAddress
	{
		public EnumAddressLength AddressLength { get; set; }
		public int GateHeight { get; set; }
		public byte GlyphLength { get; set; }
		public AddressCoordinates AddressCoordinates { get; }
		public ulong AddressBits { get; }
		public (int X, int Z) SectorOrigin { get; set; }

		public void FromCoordinates(int x, int y, int z, EnumAddressLength length = EnumAddressLength.Short, int dimension = 0, int fromDimension = 0);
		public int GetAddressResolution(int mapSize, bool latitude);
		public void FromTreeAttributes(ITreeAttribute tree);
		public void ToTreeAttributes(ITreeAttribute tree);
	}
}
