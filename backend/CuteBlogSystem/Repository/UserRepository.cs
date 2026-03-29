using CuteBlogSystem.Entity;
using System;
using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class UserRepository
    {
        private readonly ILogger<UserRepository> _logger;
        private readonly MyDbContext _dbContext;
        public UserRepository(MyDbContext dbContext, ILogger<UserRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        // 添加用户
        public async Task<bool> AddUserAsync(User user)
        {
            try
            {
                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加用户失败");
                return false;
            }
        }

        // 根据用户名获取用户
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }

        // 根据邮箱获取用户
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        // 根据ID获取用户
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _dbContext.Users.FindAsync(id);
        }

        // 更新用户信息
        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息失败");
                return false;
            }
        }

        // 根据用户id查询用户是否存在
        public async Task<bool> UserExistsByIdAsync(int userId)
        {
            return await _dbContext.Users.AnyAsync(u => u.Id == userId);
        }
    }
}
