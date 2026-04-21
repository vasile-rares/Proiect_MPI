using Microsoft.EntityFrameworkCore;
using Keyless.Domain.Entities;

namespace Keyless.Infrastructure.Context;

public class KeylessDatabaseContext : DbContext
{
	public KeylessDatabaseContext(DbContextOptions<KeylessDatabaseContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users => Set<User>();
	public DbSet<StatisticsGame> StatisticsGames => Set<StatisticsGame>();
	public DbSet<UserStatsAggregate> UserStatsAggregates => Set<UserStatsAggregate>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(user => user.Id);
			entity.Property(user => user.Username).IsRequired();
			entity.Property(user => user.Email).IsRequired();
			entity.Property(user => user.PasswordHash).IsRequired();
			entity.HasQueryFilter(user => user.DeletedAt == null);
		});

		modelBuilder.Entity<StatisticsGame>(entity =>
		{
			entity.HasKey(statisticsGame => statisticsGame.Id);

			// Index to speed up leaderboard queries by date, duration, and user
			entity.HasIndex(statisticsGame => new { statisticsGame.CreatedAt, statisticsGame.DurationInSeconds, statisticsGame.UserId });

			entity.HasQueryFilter(statisticsGame => statisticsGame.DeletedAt == null && statisticsGame.User.DeletedAt == null);

			entity.HasOne(statisticsGame => statisticsGame.User)
				.WithMany()
				.HasForeignKey(statisticsGame => statisticsGame.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserStatsAggregate>(entity =>
		{
			entity.HasKey(agg => agg.UserId);
			entity.HasOne(agg => agg.User)
				.WithMany()
				.HasForeignKey(agg => agg.UserId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasQueryFilter(agg => agg.User.DeletedAt == null);
			entity.HasIndex(agg => agg.UpdatedAt);
		});
	}
}
