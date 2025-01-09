using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models;

namespace StockManagementWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmCompaniesController : ControllerBase
    {
        private readonly MydbContext _context;

        public SmCompaniesController(MydbContext context)
        {
            _context = context;
        }

        // GET: api/SmCompanies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SmCompany>>> GetSmCompanies()
        {
            return await _context.SmCompanies.ToListAsync();
        }

        // GET: api/SmCompanies/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SmCompany>> GetSmCompany(string id)
        {
            var smCompany = await _context.SmCompanies.FindAsync(id);

            if (smCompany == null)
            {
                return NotFound();
            }

            return smCompany;
        }

        // PUT: api/SmCompanies/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSmCompany(string id, SmCompany smCompany)
        {
            if (id != smCompany.PkCompanyCode)
            {
                return BadRequest();
            }

            _context.Entry(smCompany).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SmCompanyExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        // POST: api/SmCompanies
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SmCompany>> PostSmCompany(SmCompany smCompany)
        {
            _context.SmCompanies.Add(smCompany);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (SmCompanyExists(smCompany.PkCompanyCode))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetSmCompany", new { id = smCompany.PkCompanyCode }, smCompany);
        }

        // DELETE: api/SmCompanies/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSmCompany(string id)
        {
            var smCompany = await _context.SmCompanies.FindAsync(id);
            if (smCompany == null)
            {
                return NotFound();
            }

            _context.SmCompanies.Remove(smCompany);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool SmCompanyExists(string id)
        {
            return _context.SmCompanies.Any(e => e.PkCompanyCode == id);
        }
    }
}
