using DicomLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Function_01
{
	public class VerboseLogger : IVerboseWriter
	{
		ILogger _logger;

		public VerboseLogger(ILogger logger) 
		{
			_logger = logger;
		}

		public void Write(string value)
		{
			_logger.LogInformation(value);
		}
	}
}
