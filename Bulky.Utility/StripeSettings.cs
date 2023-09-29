using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Utility
{
	public class StripeSettings
	{
		// same name with appsettings.json 
        public string SecretKey { get; set; }
		public string PublishableKey { get; set; }

	}
}
