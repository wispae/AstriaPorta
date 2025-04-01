using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AstriaPorta.Content
{
	public class BEBehaviorGatePowerSystem : BlockEntityBehavior, IGatePowerSystem
	{
		protected int pMax = 0;
		protected int power;
		protected bool canRecharge;
		protected EnumGatePowerSystemType systemType;
		private bool fromAttributes = false;

		public int MaxPower
		{
			get
			{
				return pMax;
			}
			set
			{
				pMax = value;
				if (power > pMax) power = pMax;
			}
		}

		public int PowerLevel
		{
			get { return power; }
			set { power = value; }
		}

		public bool IsRechargeable
		{
			get { return canRecharge; }
			set { canRecharge = value; }
		}

		public EnumGatePowerSystemType SystemType
		{
			get { return systemType; }
			set { systemType = value; }
		}

		public BEBehaviorGatePowerSystem(BlockEntity be) : base(be)
		{
		}

		public override void Initialize(ICoreAPI api, JsonObject properties)
		{
			base.Initialize(api, properties);

			if (!fromAttributes)
			{
				pMax = 100000;
				power = 50000;
				canRecharge = true;
				systemType = EnumGatePowerSystemType.Universal;
			}
		}

		public int TryGetPower(int requestedPower)
		{
			int availablePower = (requestedPower < power) ? requestedPower : power;
			power -= requestedPower;
			if (power < 0) power = 0;

			return availablePower;
		}

		public int TryPutPower(int providedPower)
		{
			if (!canRecharge) return providedPower;

			power += providedPower;
			int powerdiff = 0;
			if (power > pMax)
			{
				powerdiff = pMax - power;
				power = pMax;
			}

			return powerdiff;
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			pMax = tree.GetInt("pMax");
			power = tree.GetInt("power");
			canRecharge = tree.GetBool("canRecharge");
			fromAttributes = true;
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			tree.SetInt("pMax", pMax);
			tree.SetInt("power", power);
			tree.SetBool("canRecharge", canRecharge);
		}
	}
}
