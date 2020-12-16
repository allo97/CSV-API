using CSV_API.Models;
using CSV_API.Services.Interfaces;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSV_API.Services.Implementations
{
    public class CSVService : ICSVService
    {
        public async Task<IEnumerable<ReferenceNumberInfo>> GetAllMissingNumbers(string pathToManifests, string pathToBillings)
        {
            List<string> manifestsPath = Directory.EnumerateFiles(@$"{pathToManifests}", "*.csv").ToList();
            List<string> billingPaths = Directory.EnumerateFiles(@$"{pathToBillings}", "*.csv").ToList();

            // Wrong BEF

            var newWrongBefs = (await LoadBefs(billingPaths)).Select(bef => new BEF
            {
                BEFName = bef.BEFName,
                BEFRecords = bef.BEFRecords.FindAll(x => !x.ReferenceNumber.StartsWith("AD"))
            }).AsParallel().ToList();

            // Manifests
            var manifests = await LoadManifests(manifestsPath);

            var results = newWrongBefs.SelectMany(bef =>
            {
                return bef.BEFRecords.Select(async befRecord =>
                {
                    (var manifestName, var referenceNumber) = await SearchBarcode(manifests, befRecord);

                    return new ReferenceNumberInfo
                    {
                        ManifestName = manifestName,
                        ManifestReferenceNumber = referenceNumber,
                        OriginalBarcode = befRecord.OriginalBarcode,
                        BEFName = bef.BEFName,
                        BEFReferenceNumber = befRecord.ReferenceNumber
                    };
                }).AsParallel();
            });

            return await Task.WhenAll(results);
        }

        public async Task FixRefNumbers(string pathToManifests, string pathToBillings, string resultsPath)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var recordsToMap = (await GetAllMissingNumbers(pathToManifests, pathToBillings)).Where(result => result.ManifestReferenceNumber != "" && result.ManifestName != null).ToList();
            var newRecordsToMap = TransformRecords(recordsToMap);
            Debug.WriteLine(sw.ElapsedMilliseconds);
            await MapAndUpdateBefs(newRecordsToMap, pathToBillings, resultsPath);
            Debug.WriteLine(sw.ElapsedMilliseconds);
        }

        public async Task GetDuplicatedBarcodes(string pathToBillings, string resultsPath)
        {
            List<string> billingPaths = Directory.EnumerateFiles(pathToBillings, "*.csv").ToList();

            var befs = await LoadBefs(billingPaths);

            var results = new List<BarcodeDuplicateInfo>();

            var allBefRecords = befs.SelectMany(x => x.BEFRecords);

            var duplicates = allBefRecords.GroupBy(x => x.OriginalBarcode).Where(g => g.Count() > 1).Select(g => g.First()).ToList();

            duplicates.ForEach(duplicate =>
            {
                SearchBEFName(duplicate.OriginalBarcode, befs).ForEach(x => results.Add(new BarcodeDuplicateInfo
                {
                    Barcode = duplicate.OriginalBarcode,
                    BEFName = x,
                    AccountNumber = duplicate.AccountNumber
                }));
            });

            await WriteDuplicatesToCsv(resultsPath, results);
        }

        public async Task WriteToCsv(string path, IEnumerable<ReferenceNumberInfo> referenceNumberInfos)
        {
            using (var writer = new StreamWriter($"{path}"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Configuration.ShouldQuote = (field, context) => true;
                await csv.WriteRecordsAsync(referenceNumberInfos);
            }
            Debug.WriteLine("data has been saved to csv");
        }

        private Task<(string, string)> SearchBarcode(List<Manifest> manifests, BEFRecord befRecord)
        {
            return Task.Run(() =>
            {
                ManifestRecord manifestRecord;

                foreach (var manifest in manifests)
                {
                    manifestRecord = manifest.ManifestRecords.FirstOrDefault(manifestRecord => manifestRecord.Barcode == befRecord.OriginalBarcode);
                    if (manifestRecord != null)
                        return (manifest.ManifestName, manifestRecord.ReferenceNumber);
                }
                return (null, null);
            });
        }

        private Task<List<Manifest>> LoadManifests(List<string> manifestsPath)
        {
            return Task.Run(() =>
            {
                return manifestsPath.Select(manifestPath =>
                {
                    using (var reader = new StreamReader(manifestPath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Configuration.HasHeaderRecord = false;
                        return new Manifest { ManifestName = Path.GetFileName(manifestPath), ManifestRecords = csv.GetRecords<ManifestRecord>().ToList() };
                    }
                }).AsParallel().ToList();
            });
        }

        private Task<List<BEF>> LoadBefs(List<string> billingPaths)
        {
            var loadBefsTask = Task.Run(() =>
            {
                return billingPaths.Select(billingPath =>
                {
                    using (var reader = new StreamReader(billingPath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.Replace(" ", "");
                        return new BEF
                        {
                            BEFName = Path.GetFileName(billingPath),
                            BEFRecords = csv.GetRecords<BEFRecord>().ToList()
                        };
                    }
                }).AsParallel().ToList();
            });

            return loadBefsTask;
        }

        private List<BefToMap> TransformRecords(List<ReferenceNumberInfo> recordsToTransform)
        {
            return recordsToTransform.Select(x => x.BEFName).Distinct().Select(befName => new BefToMap
            {
                BEFName = befName,
                ValuesToMap = recordsToTransform.Where(x => x.BEFName == befName).Select(x => new ValueToMap
                {
                    OriginalBarcode = x.OriginalBarcode,
                    ManifestReferenceNumber = x.ManifestReferenceNumber
                }).AsParallel().ToList()
            }).ToList();
        }

        private async Task MapAndUpdateBefs(List<BefToMap> befsToMap, string pathToBillings, string resultsPath)
        {
           var updatedBefs = befsToMap.Select(async recordToMap =>
           {
               var oldBef = await LoadFileDynamic(@$"{pathToBillings}\{recordToMap.BEFName}");

               var updatedBef = oldBef.Select(record =>
               {
                   var originalBarcode = (string)((IDictionary<string, object>)record)["Original Barcode"];

                   var value = recordToMap.ValuesToMap.Find(x => x.OriginalBarcode == originalBarcode);

                   if (value != null)
                       ((IDictionary<string, object>)record)["Reference Number"] = value.ManifestReferenceNumber;

                   return record;
               }).AsParallel().ToList();
              
               await WriteBEFToCsv(@$"{resultsPath}\{recordToMap.BEFName}", updatedBef);
           });

            await Task.WhenAll(updatedBefs);
        }

        private async Task<List<dynamic>> LoadFileDynamic(string path)
        {
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return await csv.GetRecordsAsync<dynamic>().ToListAsync();
            }
        }

        private async Task WriteBEFToCsv(string path, List<dynamic> updatedBef)
        {
            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Configuration.ShouldQuote = (field, context) =>
                {
                    return context.Record.Count != 19 && context.Record.Count > 13 && context.Row > 1 ? false : true;
                };
                await csv.WriteRecordsAsync(updatedBef);
            }
        }

        private List<string> SearchBEFName(string duplicatedBarcode, List<BEF> befs)
        {
            var BefNames = new List<string>();
            befs.ForEach(bef =>
            {
                if (bef.BEFRecords.Select(x => x.OriginalBarcode).AsParallel().Contains(duplicatedBarcode))
                    BefNames.Add(bef.BEFName);
            });

            return BefNames;
        }

        private async Task WriteDuplicatesToCsv(string path, List<BarcodeDuplicateInfo> referenceNumberInfos)
        {
            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Configuration.ShouldQuote = (field, context) => true;
                await csv.WriteRecordsAsync(referenceNumberInfos);
            }
        }
    }
}
