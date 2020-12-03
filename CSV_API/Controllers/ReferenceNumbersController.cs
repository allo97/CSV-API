using CSV_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;

namespace CSV_API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public class ReferenceNumbersController : ControllerBase
    {
        private readonly ILogger<ReferenceNumbersController> _logger;
        private readonly ICSVService _csvService;

        public ReferenceNumbersController(ILogger<ReferenceNumbersController> logger, ICSVService csvService)
        {
            _logger = logger;
            _csvService = csvService;
        }

        /// <summary>
        /// List Word Cards from MongoDB
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.BadRequest)]
        public IActionResult GetAllMissingNumbers(string pathToManifests, string pathToBillings, string resultsPath)
        {
            try
            {
                var result = _csvService.GetAllMissingNumbers(pathToManifests, pathToBillings);
                _csvService.WriteToCsv(@$"{resultsPath}\allSearchResults.csv", result);
                return Ok(@$"Results has been saved to {resultsPath}\allSearchResults.csv");
            }
            catch (Exception ex)
            {
                var errorMessage = "There was an error while trying to list Word Cards from MongoDB";
                return BadRequest(errorMessage + "\n" + ex);
            }
        }

        /// <summary>
        /// List Word Cards from MongoDB
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.BadRequest)]
        public IActionResult GetMissingNumbersWithoutManifest(string pathToManifests, string pathToBillings, string resultsPath)
        {
            try
            {
                var result = (_csvService.GetAllMissingNumbers(pathToManifests, pathToBillings)).Where(result => result.ManifestName == null);
                _csvService.WriteToCsv(@$"{resultsPath}\searchResultsNoManifest.csv", result);
                return Ok(@$"Results has been saved to {resultsPath}\searchResultsNoManifest.csv");
            }
            catch (Exception ex)
            {
                var errorMessage = "There was an error while trying to list Word Cards from MongoDB";
                return BadRequest(errorMessage + "\n" + ex);
            }
        }

        /// <summary>
        /// List Word Cards from MongoDB
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.BadRequest)]
        public IActionResult GetMissingNumbersWithManifest(string pathToManifests, string pathToBillings, string resultsPath)
        {
            try
            {
                var result = (_csvService.GetAllMissingNumbers(pathToManifests, pathToBillings)).Where(result => result.ManifestReferenceNumber != "" && result.ManifestName != null);
                _csvService.WriteToCsv(@$"{resultsPath}\searchResultsManifestWithRefNumbers.csv", result);
                return Ok(@$"Results has been saved to {resultsPath}\searchResultsManifestWithRefNumbers.csv");
            }
            catch (Exception ex)
            {
                var errorMessage = "There was an error while trying to list Word Cards from MongoDB";
                return BadRequest(errorMessage + "\n" + ex);
            }
        }

        /// <summary>
        /// List Word Cards from MongoDB
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.BadRequest)]
        public IActionResult GetMissingNumbersWithManifestWithoutRefNumbers(string pathToManifests, string pathToBillings, string resultsPath)
        {
            try
            {
                var result = (_csvService.GetAllMissingNumbers(pathToManifests, pathToBillings)).Where(result => result.ManifestName != "" && result.ManifestReferenceNumber == "");
                _csvService.WriteToCsv(@$"{resultsPath}\searchResultsManifestWithRefNumbers.csv", result);
                return Ok(@$"Results has been saved to {resultsPath}\searchResultsManifestWithRefNumbers.csv");
            }
            catch (Exception ex)
            {
                var errorMessage = "There was an error while trying to list Word Cards from MongoDB";
                return BadRequest(errorMessage + "\n" + ex);
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.BadRequest)]
        public IActionResult FixRefNumbers(string pathToManifests, string pathToBillings, string resultsPath)
        {
            try
            {
                _csvService.FixRefNumbers(pathToManifests, pathToBillings, resultsPath);
                return Ok(@$"Fixed BEFs have been saved to {resultsPath}");
            }
            catch (Exception ex)
            {
                var errorMessage = "There was an error while trying to list Word Cards from MongoDB";
                return BadRequest(errorMessage + "\n" + ex);
            }
        }
    }
}
