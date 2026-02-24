using Microsoft.EntityFrameworkCore;
using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Infrastructure.Context;

namespace MonkeyType.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MonkeyTypeDatabaseContext _context;

        public UserRepository(MonkeyTypeDatabaseContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
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

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}