using System;
using System.Runtime.Serialization;

namespace LocalClient
{
	[Serializable]
	internal class MissingPatientIdInIdMapException : Exception
	{
		public MissingPatientIdInIdMapException()
		{
		}

		public MissingPatientIdInIdMapException(string message) : base(message)
		{
		}

		public MissingPatientIdInIdMapException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected MissingPatientIdInIdMapException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}