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
        public async Task<ActionResult<IEnumerable<Contract>>> GetContracts()
        {
            return await _context.Contracts
                .Include(c => c.Methods)
                    .ThenInclude(m => m.Variables)
                .Include(c => c.Methods)
                    .ThenInclude(m => m.TestCases)
                        .ThenInclude(tc => tc.TestValues)
                .Include(c => c.Methods)
                    .ThenInclude(m => m.TestCases)
                        .ThenInclude(tc => tc.Result)
                .ToListAsync();
        }

        // GET: api/contracts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Contract>> GetContract(string id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Methods)
                    .ThenInclude(m => m.Variables)
                .Include(c => c.Methods)
                    .ThenInclude(m => m.TestCases)
                        .ThenInclude(tc => tc.TestValues)
                .Include(c => c.Methods)
                    .ThenInclude(m => m.TestCases)
                        .ThenInclude(tc => tc.Result)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound();
            }

            return contract;
        }

        // POST: api/contracts
        [HttpPost]
        public async Task<ActionResult<Contract>> PostContract(Contract contract)
        {
            contract.Id = Guid.NewGuid().ToString();
            contract.CreatedAt = DateTime.UtcNow;

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContract), new { id = contract.Id }, contract);
        }

        // PUT: api/contracts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContract(string id, Contract contract)
        {
            if (id != contract.Id)
            {
                return BadRequest();
            }

            _context.Entry(contract).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContractExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/contracts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContract(string id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ContractExists(string id)
        {
            return _context.Contracts.Any(e => e.Id == id);
        }
    }
}


