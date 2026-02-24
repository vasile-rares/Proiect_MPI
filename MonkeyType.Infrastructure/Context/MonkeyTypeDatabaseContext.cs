using Microsoft.EntityFrameworkCore;
using MonkeyType.Domain.Entities;

namespace MonkeyType.Infrastructure.Context;

public class MonkeyTypeDatabaseContext : DbContext
{
	public MonkeyTypeDatabaseContext(DbContextOptions<MonkeyTypeDatabaseContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users => Set<User>();
	public DbSet<StatisticsGame> StatisticsGames => Set<StatisticsGame>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(user => user.Id);
			entity.Property(user => user.Username).IsRequired();
			entity.Property(user => user.PasswordHash).IsRequired();
			entity.Property(user => user.EmailConfirmationToken).IsRequired();
		});

		modelBuilder.Entity<StatisticsGame>(entity =>
		{
			entity.HasKey(statisticsGame => statisticsGame.Id);
			entity.Property(statisticsGame => statisticsGame.Type).IsRequired();
			entity.Property(statisticsGame => statisticsGame.Mode).IsRequired();

			entity.HasOne<User>()
				.WithMany()
				.HasForeignKey(statisticsGame => statisticsGame.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}
