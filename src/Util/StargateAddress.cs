using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Server;

namespace AstriaPorta.Util
{
	/*
	 *  ADDRESS BITS LAYOUT
	 *  
	 *  | PADDING | X COORD | Z COORD | DIMENSION |
	 *  |/////////|---------|---------|-----------|
	 *  |         | 18 bits | 18 bits |  11 bits  |
	 */

	public class StargateAddress : IStargateAddress
	{
		public StargateAddress() {
			AddressLength = EnumAddressLength.Short;
			addressCoordinates = new AddressCoordinates { X = 0, Y = 0, Z = 0, Dimension = 0, AddressBits = 0, Glyphs = new byte[] { } };
			GlyphLength = 36;
			SectorOrigin = (0, 0);
			AddressBits = 0;
			IsValid = false;
		}

		public StargateAddress(EnumAddressLength initialLength)
		{
			AddressLength = initialLength;
			addressCoordinates = new AddressCoordinates { X = 0, Y = 0, Z = 0, Dimension = 0, AddressBits = 0, Glyphs = new byte[] { } };
			GlyphLength = 36;
			SectorOrigin = (0, 0);
			AddressBits = 0;
			IsValid = false;
		}

		public StargateAddress(int worldX, int worldZ, int worldY)
		{
			AddressLength = EnumAddressLength.Short;
			addressCoordinates = new AddressCoordinates
			{
				X = 0,
				Y = 0,
				Z = 0,
				Dimension = 0,
				AddressBits = 0,
				Glyphs = new byte[] { }
			};

			GlyphLength = 36;
			AddressBits = 0;
			IsValid = false;


			XResolution = GlobalConstants.ChunkSize;
			ZResolution = GlobalConstants.ChunkSize;
			VerticalResolution = GlobalConstants.ChunkSize;
		}

		private AddressCoordinates addressCoordinates;

		public EnumAddressLength AddressLength { get; set; }
		public int AddressLengthNum
		{
			get
			{
				switch (AddressLength)
				{
					case EnumAddressLength.Short: return 7;
					case EnumAddressLength.Medium: return 8;
					case EnumAddressLength.Long: return 9;
				}

				return 7;
			}
		}
		public AddressCoordinates AddressCoordinates { get { return addressCoordinates; } }
		public int GateHeight { get { return addressCoordinates.Y; } set { addressCoordinates.Y = value; } }
		public byte GlyphLength {  get; set; }
		public (int X, int Z) SectorOrigin {  get; set; }
		public ulong AddressBits { get; set; }

		public bool IsValid = false;

		public readonly int MaxHorizontalDistance = 249999;
		public readonly int MaxVerticalDistance = 512;
		public readonly int MaxDimension = 1023;
		public readonly int XResolution = GlobalConstants.ChunkSize;
		public readonly int ZResolution = GlobalConstants.ChunkSize;
		public readonly int VerticalResolution = GlobalConstants.ChunkSize;

		public readonly int MaxWorldSizeX = GlobalConstants.MaxWorldSizeXZ;
		public readonly int MaxWorldSizeZ = GlobalConstants.MaxWorldSizeXZ;
		public readonly int MaxWorldSizeY = GlobalConstants.MaxWorldSizeY;

		/// <summary>
		/// Initializes address and address bits from provided block coordinates
		/// </summary>
		/// <remarks>
		/// Checks validity
		/// </remarks>
		/// <param name="x">Gate world X position</param>
		/// <param name="y">Gate world Y position</param>
		/// <param name="z">Gate world Z position</param>
		/// <param name="length">Address Length</param>
		/// <param name="dimension">Gate dimension</param>
		/// <returns></returns>
		public void FromCoordinates(int x, int y, int z, EnumAddressLength length = EnumAddressLength.Short, int dimension = 0, int fromDimension = 0)
		{
			// 403 FORBIDDEN (Mini-dimension)
			if (dimension == 1) dimension = 0;
			else if (dimension > 1023) dimension = 1023;
			byte[] glyphs = { 0, 0, 0, 0, 0, 0, 0 };

			int chunkX, chunkY, chunkZ;
			chunkX = x / XResolution;
			chunkZ = z / ZResolution;
			chunkY = y / VerticalResolution;

			if (CoordinatesValid(chunkX, chunkY, chunkZ, dimension))
			{
				AddressBits = BitsFromCoordinates(chunkX, chunkY, chunkZ, dimension, length);
				glyphs = GlyphsFromBits(AddressBits);

				IsValid = true;
			} else
			{
				IsValid = false;
			}

			if (dimension == fromDimension)
			{
				AddressLength = EnumAddressLength.Short;
			} else
			{
				AddressLength = EnumAddressLength.Long;
			}

			AddressCoordinates coordinates = new AddressCoordinates { X = x, Y = y, Z = z, Dimension = dimension, AddressBits = AddressBits, Glyphs = glyphs, IsValid = IsValid };
			addressCoordinates = coordinates;
		}

		[Obsolete]
		public void FromByteAddress(byte[] address)
		{
			AddressCoordinates coordinates = new AddressCoordinates { X = 0, Y = 0, Z = 0, Dimension = 0, AddressBits = 0, IsValid = true, Glyphs = address };
			addressCoordinates = coordinates;
		}

		/// <summary>
		/// Initializes address and address bits from provided glyphs<br/>
		/// </summary>
		/// <remarks>
		/// Checks validity
		/// </remarks>
		/// <param name="glyphs"></param>
		public void FromGlyphs(byte[] glyphs, int fromDimension = 0)
		{
			ulong addressBits = BitsFromGlyphs(glyphs);
			AddressBits = addressBits;
			int x, y, z, dim;
			x = XFromBits(addressBits) * XResolution;
			y = YFromBits(addressBits, glyphs.Length - 7) * VerticalResolution;
			z = ZFromBits(addressBits) * ZResolution;
			dim = DimensionFromBits(addressBits, glyphs.Length - 7);

			IsValid = ValidityFromBits(addressBits);
			if (!IsValid)
			{
				IsValid = CoordinatesValid(x, y, z, dim);
			}

			if (fromDimension == dim)
			{
				AddressLength = EnumAddressLength.Short;
			} else
			{
				AddressLength = EnumAddressLength.Long;
			}

			addressCoordinates = new AddressCoordinates
			{
				X = x,
				Y = y,
				Z = z,
				Dimension = dim,
				Glyphs = glyphs,
				IsValid = IsValid
			};
		}

		/// <summary>
		/// Checks validity of the provided coordinates
		/// </summary>
		/// <param name="x">Chunk X</param>
		/// <param name="y">Chunk Y</param>
		/// <param name="z">Chunk Z</param>
		/// <param name="dim">Dimension</param>
		/// <returns></returns>
		public bool CoordinatesValid(int x, int y, int z, int dim)
		{
			x *= GlobalConstants.ChunkSize;
			y *= GlobalConstants.ChunkSize;
			z *= GlobalConstants.ChunkSize;
			if (x < 0 || y < 0 || z < 0 || x > GlobalConstants.MaxWorldSizeXZ || y > GlobalConstants.MaxWorldSizeY || z > GlobalConstants.MaxWorldSizeXZ || dim < 0 || dim > 1023)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks validity of address stored in the address bits
		/// </summary>
		/// <param name="bits"></param>
		/// <returns></returns>
		public bool CoordinatesValid(ulong bits)
		{
			int x, y, z, dim, length;
			length = (int)((bits >> 62) & 0x03);
			x = XFromBits(bits) * GlobalConstants.ChunkSize;
			y = YFromBits(bits, length) * GlobalConstants.ChunkSize;
			z = ZFromBits(bits) * GlobalConstants.ChunkSize;
			dim = DimensionFromBits(bits, length);

			return CoordinatesValid(x, y, z, dim);
		}

		public int GetDistanceTo(IStargateAddress remoteAddress)
		{
			int deltaX = remoteAddress.AddressCoordinates.X - AddressCoordinates.X;
			int deltaZ = remoteAddress.AddressCoordinates.Z - AddressCoordinates.Z;

			return (int)Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
		}

		public int GetAddressResolution(int mapSize, bool latitude)
		{
			return mapSize / (36 * 36 * 36);
		}

		/// <summary>
		/// Creates address bits from provided chunk coordinates<br/>
		/// Ignores y or dim if length does not match
		/// </summary>
		/// <param name="x">Chunk X</param>
		/// <param name="y">Chunk Y</param>
		/// <param name="z">Chunk Z</param>
		/// <param name="dim">Dimension</param>
		/// <param name="length">Address length</param>
		/// <returns></returns>
		private ulong BitsFromCoordinates(int x, int y, int z, int dim, EnumAddressLength length)
		{
			ulong addressBits = 0;

			if (dim < 0) dim = 0;
			else if (dim > 1023) dim = 1023;

			ulong xBits, zBits, dimBits;

			xBits = (ulong)(x & 0x00_03_FF_FF);
			zBits = (ulong)(z & 0x00_03_FF_FF);
			dimBits = (ulong)(dim & 0x07_FF);

			addressBits += (xBits << 29);
			addressBits += (zBits << 11);
			addressBits += dimBits;

			return addressBits;
		}

		/// <summary>
		/// Initializes address from provided address bits
		/// </summary>
		/// <param name="addressBits"></param>
		public void FromBits(ulong addressBits, int fromDimension = 0)
		{
			int length = (int)(addressBits >> 62) & 0x3;

			AddressBits = addressBits;
			addressCoordinates.Glyphs = GlyphsFromBits(addressBits);
			addressCoordinates.AddressBits = addressBits;
			addressCoordinates.X = XFromBits(addressBits) * XResolution;
			addressCoordinates.Y = YFromBits(addressBits, length);
			addressCoordinates.Z = ZFromBits(addressBits) * ZResolution;
			addressCoordinates.Dimension = DimensionFromBits(addressBits, length);

			if (fromDimension == addressCoordinates.Dimension)
			{
				AddressLength = EnumAddressLength.Short;
			} else
			{
				AddressLength = EnumAddressLength.Long;
			}

			IsValid = CoordinatesValid(AddressCoordinates.X, addressCoordinates.Y, addressCoordinates.Z, addressCoordinates.Dimension);
		}

		public int XFromBits(ulong addressBits)
		{
			return (int)((addressBits >> 29) & 0x03_FF_FF);
		}

		public int YFromBits(ulong addressBits, int length)
		{
			return 0;
		}

		public int ZFromBits(ulong addressBits)
		{
			return (int)((addressBits >> 11) & 0x03_FF_FF);
		}

		public int DimensionFromBits(ulong addressBits, int length)
		{
			return (int)(addressBits & 0x07_FF);
		}
		public bool ValidityFromBits(ulong addressBits)
		{
			return true;
		}

		// WARNING
		// does NOT check for validity of the address (validity bit)
		// check and set afterwards if you need it!

		/// <summary>
		/// Calculates the address bits from the provided glyphs<br/>
		/// Automatically detects address length
		/// </summary>
		/// <remarks>
		/// Does not check for address validity
		/// </remarks>
		/// <param name="glyphs"></param>
		/// <param name="dimension">Dimension of the dialer</param>
		/// <returns>A uint64 containing the position and metadata</returns>
		public ulong BitsFromGlyphs(byte[] glyphs, int dimension = 0)
		{
			ulong addressBits = 0;

			ulong coordinateBits = 0;
			// isolate coordinates (first 7 glyphs)
			for (int i = 0; i < 7; i++)
			{
				coordinateBits += glyphs[i] * (ulong)Math.Pow(36, 6 - i);
			}

			ulong dimBits = 0;
			for (int i = 7; i < 9; i++)
			{
				if (glyphs.Length <= i)
				{
					continue;
				}
				dimBits += glyphs[i] * (ulong)Math.Pow(36, 8 - i);
			}

			addressBits += coordinateBits << 11;
			addressBits += dimBits;
			return addressBits;
		}

		public byte[] GlyphsFromBits(ulong addressBits)
		{
			addressBits &= 0x00_00_7F_FF__FF_FF_FF_FF;

			byte[] address;
			ulong coordinateBits = (addressBits >> 11) & 0x0F__FF_FF_FF_FF;
			ulong dimBits = addressBits & 0x07_FF;
			ulong pow;

			address = new byte[9];

			for (int i = 0; i < 7; i++)
			{
				pow = (ulong)MathF.Pow(36, 6 - i);
				address[i] = (byte)MathF.Floor(coordinateBits / pow);
				coordinateBits = coordinateBits % pow;
			}

			for (int i = 7; i < 9; i++)
			{
				pow = (ulong)MathF.Pow(36, 8 - i);
				address[i] = (byte)MathF.Floor(dimBits / pow);
				dimBits = dimBits % pow;
			}

			return address;
		}

		[Obsolete]
		private byte grabGlyphPosition(int pos, int length, int division)
		{
			return (byte)(((float)pos / length) * (division));
		}

		[Obsolete]
		private int transformPosition(int pos, int origin, int division, int toOrigin)
		{
			return (Math.Abs(origin) > Math.Abs(toOrigin)) ? (origin - toOrigin) : (toOrigin - origin);
		}

		[Obsolete]
		private List<byte> createGlyphList(int count, bool includeOrigin)
		{
			int originModifier = (byte)(includeOrigin ? 1 : 0);

			List<byte> freeGlyphs = new List<byte>(count + originModifier - 1);
			
			for (int i = 0; i < freeGlyphs.Capacity; i++)
			{
				freeGlyphs.Add((byte)(i+originModifier));
			}

			return freeGlyphs;
		}

		public void FromTreeAttributes(ITreeAttribute tree)
		{
			string tp = "s";
			if (AddressLength == EnumAddressLength.Medium) tp = "m";
			else if (AddressLength == EnumAddressLength.Long) tp = "l";

			AddressLength = (EnumAddressLength)tree.GetInt(tp + "addressLength");
			GlyphLength = (byte)tree.GetInt(tp + "glyphLength");
			
			int X = tree.GetInt(tp + "addressX");
			int Z = tree.GetInt(tp + "addressZ");
			int D = tree.GetInt(tp + "addressDim");
			byte[] address = tree.GetBytes(tp + "address");

			int sectorX = tree.GetInt(tp + "sectorX");
			int sectorZ = tree.GetInt(tp + "sectorZ");

			addressCoordinates.X = X;
			addressCoordinates.Z = Z;
			addressCoordinates.Dimension = D;
			addressCoordinates.Glyphs = address;

			SectorOrigin = (sectorX, sectorZ);

			IsValid = tree.GetBool(tp + "isValid");
			AddressBits = ulong.Parse(tree.GetString(tp + "addressBits"), System.Globalization.NumberStyles.HexNumber);
		}

		public void ToTreeAttributes(ITreeAttribute tree)
		{
			// type prefix to distinguish between address types
			string tp = "s";
			if (AddressLength == EnumAddressLength.Medium) tp = "m";
			else if (AddressLength == EnumAddressLength.Long) tp = "l";

			tree.SetInt(tp + "addressLength", (int)AddressLength);
			tree.SetInt(tp + "glyphLength", GlyphLength);

			tree.SetInt(tp + "addressX", addressCoordinates.X);
			tree.SetInt(tp + "addressZ", addressCoordinates.Z);
			tree.SetInt(tp + "addressDim", addressCoordinates.Dimension);
			tree.SetBytes(tp + "address", addressCoordinates.Glyphs);

			tree.SetInt(tp + "sectorX", SectorOrigin.X);
			tree.SetInt(tp + "sectorZ", SectorOrigin.Z);

			tree.SetBool(tp + "isValid", IsValid);
			tree.SetString(tp + "addressBits", AddressBits.ToString("X"));
		}

		public override string ToString()
		{
			if (AddressCoordinates.Glyphs.Length == 0) return string.Empty;

			string addressString = "" + GlyphToChar(AddressCoordinates.Glyphs[0]);
			for (int i = 1;  i < AddressCoordinates.Glyphs.Length; i++)
			{
				addressString += "-" + GlyphToChar(AddressCoordinates.Glyphs[i]);
			}

			return addressString;
		}

		public char GlyphToChar(byte glyph)
		{
			return "0123456789abcdefghijklmnopqrstuvwxyz"[glyph % 36];
		}
	}
}
