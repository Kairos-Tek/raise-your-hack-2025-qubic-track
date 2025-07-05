// Controllers/ContractMethodsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RYH2025_Qubic.Models;
using RYH2025_Qubic.Persistence;

namespace RYH2025_Qubic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractMethodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContractMethodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/contractmethods
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractMethod>>> GetContractMethods()
        {
            return await _context.ContractMethods
                .Include(m => m.Variables)
                .Include(m => m.TestCases)
                    .ThenInclude(tc => tc.TestValues)
                .Include(m => m.TestCases)
                    .ThenInclude(tc => tc.Result)
                .ToListAsync();
        }

        // GET: api/contractmethods/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ContractMethod>> GetContractMethod(string id)
        {
            var method = await _context.ContractMethods
                .Include(m => m.Variables)
                .Include(m => m.TestCases)
                    .ThenInclude(tc => tc.TestValues)
                .Include(m => m.TestCases)
                    .ThenInclude(tc => tc.Result)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (method == null)
            {
                return NotFound();
            }

            return method;
        }

        // GET: api/contractmethods/contract/{contractId}
        [HttpGet("contract/{contractId}")]
        public async Task<ActionResult<IEnumerable<ContractMethod>>> GetMethodsByContract(string contractId)
        {
            return await _context.ContractMethods
                .Where(m => m.ContractId == contractId)
                .Include(m => m.Variables)
                .Include(m => m.TestCases)
                    .ThenInclude(tc => tc.TestValues)
                .Include(m => m.TestCases)
                    .ThenInclude(tc => tc.Result)
                .ToListAsync();
        }

        // POST: api/contractmethods
        [HttpPost]
        public async Task<ActionResult<ContractMethod>> PostContractMethod(ContractMethod method)
        {
            method.Id = Guid.NewGuid().ToString();

            _context.ContractMethods.Add(method);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContractMethod), new { id = method.Id }, method);
        }

        // PUT: api/contractmethods/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContractMethod(string id, ContractMethod method)
        {
            if (id != method.Id)
            {
                return BadRequest();
            }

            _context.Entry(method).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContractMethodExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/contractmethods/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContractMethod(string id)
        {
            var method = await _context.ContractMethods.FindAsync(id);
            if (method == null)
            {
                return NotFound();
            }

            _context.ContractMethods.Remove(method);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ContractMethodExists(string id)
        {
            return _context.ContractMethods.Any(e => e.Id == id);
        }
    }
}