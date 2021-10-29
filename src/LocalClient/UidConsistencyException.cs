using System;
using System.Runtime.Serialization;

namespace LocalClient
{
	[Serializable]
	internal class UidConsistencyException : Exception
	{
		public UidConsistencyException()
		{
		}

		public UidConsistencyException(string message) : base(message)
		{
		}

		public UidConsistencyException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected UidConsistencyException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}