using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Config
{
	[ProtoContract]
	public class StargateConfig
	{
		public static StargateConfig Loaded { get; set; } = new StargateConfig();

		[ProtoMember(1)]
		public bool DhdDestructable { get; set; } = true;
		[ProtoMember(2)]
		public int DhdMiningTier { get; set; } = 4;
		[ProtoMember(3)]
		public bool StargateDestructable { get; set; } = true;
		[ProtoMember(4)]
		public int StargateMiningTier { get; set; } = 5;
		[ProtoMember(5)]
		public bool VortexDestroys { get; set; } = true;
		[ProtoMember(6)]
		public bool VortexKills { get; set; } = true;
		[ProtoMember(7)]
		public float MaxTimeoutSeconds { get; set; } = 10f;
		[ProtoMember(8)]
		public int MinRangeChunksMilkyway { get; set; } = 0;
		[ProtoMember(9)]
		public int MaxRangeChunksMilkyway { get; set; } = 262144;
		[ProtoMember(10)]
		public float MaxConnectionDurationSecondsMilkyway { get; set; } = 60f;
		[ProtoMember(11)]
		public float DialSpeedDegreesPerSecondMilkyway { get; set; } = 80f;
		[ProtoMember(12)]
		public bool AllowQuickDial { get; set; } = true;
	}
}
