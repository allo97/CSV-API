using CSV_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CSV_API.Services.Interfaces
{
    public interface ICSVService
    {
        Task<IEnumerable<ReferenceNumberInfo>> GetAllMissingNumbers(string pathToManifests, string pathToBillings);
        Task WriteToCsv(string path, IEnumerable<ReferenceNumberInfo> referenceNumberInfos);
        Task FixRefNumbers(string pathToManifests, string pathToBillings, string resultsPath);
        Task GetDuplicatedBarcodes(string pathToBillings, string resultsPath);

    }
}
