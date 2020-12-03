using CSV_API.Models;
using System.Collections.Generic;

namespace CSV_API.Services.Interfaces
{
    public interface ICSVService
    {
        IEnumerable<ReferenceNumberInfo> GetAllMissingNumbers(string pathToManifests, string pathToBillings);
        void WriteToCsv(string path, IEnumerable<ReferenceNumberInfo> referenceNumberInfos);
        void FixRefNumbers(string pathToManifests, string pathToBillings, string resultsPath);
        void GetDuplicatedBarcodes(string pathToBillings, string resultsPath);

    }
}
