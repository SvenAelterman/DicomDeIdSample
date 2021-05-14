using DicomLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
	public class ConsoleVerboseWriter : IVerboseWriter
	{
		public void Write(string value)
		{
			Console.WriteLine(value);
		}
	}
}
