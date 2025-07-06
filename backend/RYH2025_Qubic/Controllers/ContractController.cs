using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RYH2025_Qubic.Dtos;
using RYH2025_Qubic.Models;
using RYH2025_Qubic.Persistence;
using RYH2025_Qubic.Services;
using System.Diagnostics.Contracts;
using System.Linq;

namespace RYH2025_Qubic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private IConfiguration _configuration { get; set; }
        private ApplicationDbContext _dbContext { get; set; }
        public ContractsController(IConfiguration configuration, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }


        [HttpPost("analyze")]
        public async Task<ActionResult> AnalyzeContract([FromForm] AnalyzeContractRequest request)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new { message = "Invalid file" });
                }

                
                string fileContent;
                using (var reader = new StreamReader(request.File.OpenReadStream()))
                {
                    fileContent = await reader.ReadToEndAsync();
                }

                var contractAnalyzer = new QubicContractService(_configuration);
                var result = await contractAnalyzer.ProcessContractCompleteAsync(fileContent,
                    new Dtos.ProcessingOptions()
                    {
                        GenerateCode = false,
                        PerformSecurityAudit = true,
                    });

                _dbContext.ContractAnalyses.Add(result);
                await _dbContext.SaveChangesAsync(); 

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/contracts
        [HttpGet]
        public async Task<ActionResult> GetContracts()
        {
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetContract([FromRoute] Guid id)
        {
            //return Ok(_dbContext.ContractAnalyses
            //                    .Include(x => x.Methods)
            //                    .ThenInclude(x => x.SecurityTestCases)
            //                    .FirstOrDefault(x => x.Id == id));
            return Ok(_dbContext.ContractAnalyses
                                .Include(x => x.SecurityAudit)
                                .ThenInclude(x => x.Vulnerabilities)
                                .Include(x => x.SecurityAudit)
                                .ThenInclude(x => x.OverallRisk)
                                .Include(x => x.SecurityAudit)
                                .ThenInclude(x => x.SecurityTests)
                                .Include(x => x.Methods)
                                .FirstOrDefault(x => x.Id == id));
        }
    }
}


