using CsvHelper.Configuration.Attributes;
using System.Collections.Generic;

namespace CSV_API.Models
{
    public class ReferenceNumberInfo
    {
        public string ManifestName { get; set; }
        public string ManifestReferenceNumber { get; set; }
        public string OriginalBarcode { get; set; }
        public string BEFName { get; set; }
        public string BEFReferenceNumber { get; set; }
    }

    public class BarcodeDuplicateInfo
    {
        public string AccountNumber { get; set; }
        public string Barcode { get; set; }
        public string BEFName { get; set; }
    }

    public class BEFRecord
    {
        public string OriginalBarcode { get; set; }
        public string ReferenceNumber { get; set; }
        public string AccountNumber { get; set; }
    }

    public class BEF
    {
        public string BEFName { get; set; }
        public List<BEFRecord> BEFRecords { get; set; }
    }

    public class BefToMap
    {
        public string BEFName { get; set; }
        public List<ValueToMap> ValuesToMap { get; set; }
    }

    public class ValueToMap
    {
        public string OriginalBarcode { get; set; }
        public string ManifestReferenceNumber { get; set; }
    }

    public class ManifestRecord
    {
        [Index(1)]
        public string Barcode { get; set; }
        [Index(2)]
        public string ReferenceNumber { get; set; }
    }

    public class Manifest
    {
        public string ManifestName { get; set; }
        public List<ManifestRecord> ManifestRecords { get; set; }
    }
}
