using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content
{
	[Flags]
	public enum EnumGatePowerSystemType
	{
		Consumer = 0b0000_0001,
		Producer = 0b0000_0010,
		Universal = Consumer | Producer,
		Other = 0b0000_0100
	}

	public interface IGatePowerSystem
	{
		/// <summary>
		/// Maximum power the system can store
		/// </summary>
		public int MaxPower { get; set; }

		/// <summary>
		/// The current remaining power level
		/// </summary>
		public int PowerLevel { get; set; }

		/// <summary>
		/// Whether the power system can be recharged with external power
		/// </summary>
		public bool IsRechargeable { get; set; }

		/// <summary>
		/// The type of power system this is<br/>
		/// Consumers take power, but can't provide (Not curently used)<br/>
		/// Producers producer power, but can't take any (DHDs)<br/>
		/// Universal systems can provide and take power (Gates)
		/// </summary>
		public EnumGatePowerSystemType SystemType
		{
			get; set;
		}

		/// <summary>
		/// Takes requested power from the internal power level and returns
		/// the requested power, or the remaining power. Whichever is greater
		/// </summary>
		/// <param name="requestedPower"></param>
		/// <returns>The taken power</returns>
		public int TryGetPower(int requestedPower);

		/// <summary>
		/// Attempts to recharge the power provided with the provided amount of power<br/>
		/// Only recharges up to the maximum power and returns the leftover power
		/// </summary>
		/// <param name="providedPower"></param>
		/// <returns>The leftover power</returns>
		public int TryPutPower(int providedPower);
	}
}
