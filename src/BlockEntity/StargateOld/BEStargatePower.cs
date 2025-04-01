using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content
{
	public abstract partial class BEStargate
	{
		protected IGatePowerSystem internalPowerSystem;
		protected IGatePowerSystem localPowerSystem;

		protected int energyPerKm = 1;
		protected int energyInterdimensional = 100;

		/// <summary>
		/// The Gate's internal power system.
		/// </summary>
		public IGatePowerSystem InternalPowerSystem { get { return internalPowerSystem; } }
		
		/// <summary>
		/// Attempts to register a local power system, fails when a
		/// power system with charge is already present.
		/// </summary>
		/// <param name="powerSystem"></param>
		/// <returns></returns>
		public bool RegisterLocalPowerSystem(IGatePowerSystem powerSystem)
		{
			if (localPowerSystem == null || localPowerSystem.PowerLevel == 0)
			{
				localPowerSystem = powerSystem;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Attempts to take power from the local power system if available,
		/// will attempt to supplement the power from the internal system otherwise.
		/// </summary>
		/// <param name="power"></param>
		/// <returns>The power taken from the power system(s)</returns>
		protected int TakePower(int power)
		{
			if (localPowerSystem != null)
			{
				power -= localPowerSystem.TryGetPower(power);
				if (power > 0)
				{
					power -= internalPowerSystem.TryGetPower(power);
				}
			} else
			{
				power -= internalPowerSystem.TryGetPower(power);
			}

			return power;
		}

		protected void ProcessConnectionEnergy()
		{
			int distance = Pos.ManhattenDistance(remoteGate.Pos) / 1000;
			int power = distance * energyPerKm;

			if (Pos.dimension != remoteGate.Pos.dimension) power += energyInterdimensional;

			power = TakePower(power);
			if (power > 0)
			{
				// We ran out of power -> disconnect gate
				ConnectionAborted();
			}
		}
	}
}
