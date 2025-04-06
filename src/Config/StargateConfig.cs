using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Config
{
	[ProtoContract]
	public class StargateConfig
	{
		private int minRangeChunksMilkyway;
		private int maxRangeChunksMilkyway;
		private float maxConnectionDurationSecondsMilkyway;
		private float dialSpeedDegreesPerSecondMilkyway;

		public static StargateConfig Loaded { get; set; } = new StargateConfig();

		[ProtoMember(1), DefaultValue(true)]
		public bool DhdDestructable { get; set; } = true;
		[ProtoMember(2)]
		public int DhdMiningTier { get; set; } = 4;
		[ProtoMember(3), DefaultValue(true)]
		public bool StargateDestructable { get; set; } = true;
		[ProtoMember(4)]
		public int StargateMiningTier { get; set; } = 5;
		[ProtoMember(5), DefaultValue(true)]
		public bool VortexDestroys { get; set; } = true;
		[ProtoMember(6), DefaultValue(true)]
		public bool VortexKills { get; set; } = true;
		[ProtoMember(7)]
		public float MaxTimeoutSeconds { get; set; } = 5f;
		[ProtoMember(8)]
		public int MinRangeChunksMilkyway
		{
			get => minRangeChunksMilkyway;
			set
			{
				if (value > maxRangeChunksMilkyway) value = maxRangeChunksMilkyway - 1;
				if (value < 0) value = 0;
				if (value > 262144) value = 262144;
				minRangeChunksMilkyway = value;
			}
		}
		[ProtoMember(9)]
		public int MaxRangeChunksMilkyway
		{
			get => maxRangeChunksMilkyway;
			set
			{
				if (value < minRangeChunksMilkyway) value = minRangeChunksMilkyway + 1;
				if (value < 0) value = 0;
				if (value > 262144) value = 262144;
				maxRangeChunksMilkyway = value;
			}
		}
		[ProtoMember(10)]
		public float MaxConnectionDurationSecondsMilkyway
		{
			get => maxConnectionDurationSecondsMilkyway;
			set
			{
				if (value < 10f) value = 10f;
				if (value > 180f) value = 180f;
				maxConnectionDurationSecondsMilkyway = value;
			}
		}
		[ProtoMember(11)]
		public float DialSpeedDegreesPerSecondMilkyway
		{
			get => dialSpeedDegreesPerSecondMilkyway;
			set
			{
				if (value < 20f) value = 20f;
				if (value > 180f) value = 180f;
			}
		}
		[ProtoMember(12), DefaultValue(true)]
		public bool AllowQuickDial { get; set; } = true;
	}
}
