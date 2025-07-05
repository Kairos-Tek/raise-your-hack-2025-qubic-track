using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RYH2025_Qubic.Models;
using RYH2025_Qubic.Persistence;

namespace RYH2025_Qubic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContractsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/contracts
        [HttpGet]
        public async Task<ActionResult> GetContracts()
        {
            return Ok();
        }
    }
}


