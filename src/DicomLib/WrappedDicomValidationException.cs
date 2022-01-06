using System;
using System.Runtime.Serialization;

namespace DicomLib
{
	[Serializable]
	public class WrappedDicomValidationException : Exception
	{
		public WrappedDicomValidationException()
		{
		}

		public WrappedDicomValidationException(string message) : base(message)
		{
		}

		public WrappedDicomValidationException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected WrappedDicomValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}