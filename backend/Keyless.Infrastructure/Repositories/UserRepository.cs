using Microsoft.EntityFrameworkCore;
using Keyless.Domain.Entities;
using Keyless.Domain.IRepositories;
using Keyless.Infrastructure.Context;

namespace Keyless.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly KeylessDatabaseContext _context;

        public UserRepository(KeylessDatabaseContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}