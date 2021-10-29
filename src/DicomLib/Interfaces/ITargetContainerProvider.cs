using System.IO;

namespace DicomLib
{
	public interface ITargetContainerProvider
	{
		void Write(string fileName, Stream contents);
	}
}
