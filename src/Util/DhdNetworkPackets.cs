using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Util
{
	public enum EnumDhdActionType
	{
		Connect,
		Disconnect,
		Couple,
		Decouple
	}

	public struct DhdActionPacket
	{
		public int ActionType { get; set; }
		public byte[] Glyphs { get; set; }
	}
}
