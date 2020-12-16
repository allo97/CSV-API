using CSV_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CSV_API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public class BarcodesController : ControllerBase
    {
        private readonly ILogger<BarcodesController> _logger;
        private readonly ICSVService _csvService;

        public BarcodesController(ILogger<BarcodesController> logger, ICSVService csvService)
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
        public async Task<IActionResult> GetDuplicatedBarcodes(string pathToBillings, string resultsPath)
        {
            try
            {
                await _csvService.GetDuplicatedBarcodes(@$"{pathToBillings}", @$"{resultsPath}\duplicatedBarcodes.csv");
                return Ok(@$"Results has been saved to {resultsPath}\duplicatedBarcodes.csv");
            }
            catch (Exception ex)
            {
                var errorMessage = "There was an error while trying to list Word Cards from MongoDB";
                return BadRequest(errorMessage + "\n" + ex);
            }
        }
    }
}
