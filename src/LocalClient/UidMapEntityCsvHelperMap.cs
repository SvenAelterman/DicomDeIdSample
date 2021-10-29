using CsvHelper.Configuration;
using DicomLib;
using System.Globalization;

namespace LocalClient
{
	internal sealed class UidMapEntityCsvHelperMap : ClassMap<UidMapEntity>
	{
		public UidMapEntityCsvHelperMap()
		{
			AutoMap(CultureInfo.CurrentUICulture);
			Map(e => e.ETag).Ignore();
			Map(e => e.RowKey).Ignore();
			Map(e => e.PartitionKey).Ignore();
			Map(e => e.Timestamp).Ignore();
		}
	}
}
