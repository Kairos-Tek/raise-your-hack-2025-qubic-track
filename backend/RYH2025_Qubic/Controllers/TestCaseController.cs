using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RYH2025_Qubic.Models;
using RYH2025_Qubic.Persistence;

namespace RYH2025_Qubic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestCasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestCasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/testcases
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TestCase>>> GetTestCases()
        {
            return await _context.TestCases
                .Include(tc => tc.TestValues)
                .Include(tc => tc.Result)
                .ToListAsync();
        }

        // GET: api/testcases/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TestCase>> GetTestCase(string id)
        {
            var testCase = await _context.TestCases
                .Include(tc => tc.TestValues)
                .Include(tc => tc.Result)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (testCase == null)
            {
                return NotFound();
            }

            return testCase;
        }

        // GET: api/testcases/method/{methodId}
        [HttpGet("method/{methodId}")]
        public async Task<ActionResult<IEnumerable<TestCase>>> GetTestCasesByMethod(string methodId)
        {
            return await _context.TestCases
                .Where(tc => tc.MethodId == methodId)
                .Include(tc => tc.TestValues)
                .Include(tc => tc.Result)
                .ToListAsync();
        }

        // POST: api/testcases
        [HttpPost]
        public async Task<ActionResult<TestCase>> PostTestCase(TestCase testCase)
        {
            testCase.Id = Guid.NewGuid().ToString();

            _context.TestCases.Add(testCase);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTestCase), new { id = testCase.Id }, testCase);
        }

        // PUT: api/testcases/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTestCase(string id, TestCase testCase)
        {
            if (id != testCase.Id)
            {
                return BadRequest();
            }

            _context.Entry(testCase).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TestCaseExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/testcases/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestCase(string id)
        {
            var testCase = await _context.TestCases.FindAsync(id);
            if (testCase == null)
            {
                return NotFound();
            }

            _context.TestCases.Remove(testCase);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TestCaseExists(string id)
        {
            return _context.TestCases.Any(e => e.Id == id);
        }
    }
}