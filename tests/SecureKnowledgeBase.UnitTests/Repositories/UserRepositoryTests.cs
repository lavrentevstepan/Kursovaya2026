using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.UnitTests.Common;
using Xunit;

namespace SecureKnowledgeBase.UnitTests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnSeededUsers()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var repo = new UserRepository(context);
            var users = await repo.GetAllAsync();

            Assert.Equal(3, users.Count);
            Assert.Contains(users, u => u.Username == "admin");
            Assert.Contains(users, u => u.Username == "auditor");
            Assert.Contains(users, u => u.Username == "user");
        }
    }

    [Fact]
    public async Task AddAsync_ShouldInsertNewUser()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var repo = new UserRepository(context);
            var newUser = new User
            {
                Username = "newspecialist",
                DisplayName = "New Specialist",
                PasswordHash = "hashed_pass_123",
                Role = UserRole.KnowledgeAdmin,
                IsActive = true
            };

            await repo.AddAsync(newUser);
            var retrieved = await repo.GetByUsernameAsync("newspecialist");

            Assert.NotNull(retrieved);
            Assert.Equal("New Specialist", retrieved.DisplayName);
            Assert.Equal(UserRole.KnowledgeAdmin, retrieved.Role);
        }
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var repo = new UserRepository(context);
            var user = await repo.GetByUsernameAsync("user");
            Assert.NotNull(user);

            await repo.DeleteAsync(user.Id);
            var retrieved = await repo.GetByUsernameAsync("user");

            Assert.Null(retrieved);
        }
    }
}
