using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content
{
	public class StargateClientDialingManager : StargateDialingManager
	{
		public override event EventHandler OnActivationFailed;
		public override event EventHandler OnActivationSuccess;

		public override void OnTargetGlyphReached()
		{
			throw new NotImplementedException();
		}

		public override void OnDialingSequenceCompletion()
		{
			throw new NotImplementedException();
		}

		public override void OnActivationFailure(float delta)
		{
			throw new NotImplementedException();
		}

		public override void OnGlyphActivationCompleted(float delta)
		{
			throw new NotImplementedException();
		}
	}
}
