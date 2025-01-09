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
    public class SmUsersController : ControllerBase
    {
        private readonly MydbContext _context;

        public SmUsersController(MydbContext context)
        {
            _context = context;
        }

        // GET: api/SmUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SmUser>>> GetSmUsers()
        {
            return await _context.SmUsers.ToListAsync();
        }

        // GET: api/SmUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SmUser>> GetSmUser(int id)
        {
            var smUser = await _context.SmUsers.FindAsync(id);

            if (smUser == null)
            {
                return NotFound();
            }

            return smUser;
        }

        // PUT: api/SmUsers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSmUser(int id, SmUser smUser)
        {
            if (id != smUser.PkUserCode)
            {
                return BadRequest();
            }

            _context.Entry(smUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SmUserExists(id))
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

        // POST: api/SmUsers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SmUser>> PostSmUser(SmUser smUser)
        {
            _context.SmUsers.Add(smUser);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (SmUserExists(smUser.PkUserCode))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetSmUser", new { id = smUser.PkUserCode }, smUser);
        }

        // DELETE: api/SmUsers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSmUser(int id)
        {
            var smUser = await _context.SmUsers.FindAsync(id);
            if (smUser == null)
            {
                return NotFound();
            }

            _context.SmUsers.Remove(smUser);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool SmUserExists(int id)
        {
            return _context.SmUsers.Any(e => e.PkUserCode == id);
        }
    }
}
