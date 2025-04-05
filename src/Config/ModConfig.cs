using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Config
{
	public class StargateConfig
	{
		public static StargateConfig Loaded { get; set; } = new StargateConfig();

		public bool DhdDestructable { get; set; } = true;
		public int DhdMiningTier { get; set; } = 4;
		public bool StargateDestructable { get; set; } = true;
		public int StargateMiningTier { get; set; } = 5;
		public bool VortexDestroys { get; set; } = true;
		public bool VortexKills { get; set; } = true;
		public float MaxTimeoutSeconds { get; set; } = 10f;
		public int MinRangeChunksMilkyway { get; set; } = 0;
		public int MaxRangeChunksMilkyway { get; set; } = 262144;
		public float MaxConnectionDurationSecondsMilkyway { get; set; } = 60f;
		public float DialSpeedDegreesPerSecondMilkyway { get; set; } = 80f;
		public bool AllowQuickDial { get; set; } = true;
	}
}
