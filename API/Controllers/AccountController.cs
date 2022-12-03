
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Controllers
{

    public class AccountController : BaseApiController
    {
         private readonly DataContext _context;

        public AccountController(DataContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<AppUser>> Login(LogInDto logInDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserName == logInDto.UserName);
            if (user == null) return Unauthorized();
            using var hmac = new HMACSHA512(user.SaltHash);
            var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(logInDto.Password));

            if (Enumerable.SequenceEqual(passwordHash, user.PasswordHash))
                return user;

            return Unauthorized("Invalid password");
        }


        [HttpPost]
        [Route("register")]
        public async Task<ActionResult<AppUser>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.UserName)) return BadRequest("UserName taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = registerDto.UserName.ToLower(),
                PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(registerDto.Password)),
                SaltHash = hmac.Key
            };
            await _context.AddAsync(user);
            _context.SaveChanges();
            return Ok(user);
        }



        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(u =>u.UserName == username.ToLower());
        }
    }
}