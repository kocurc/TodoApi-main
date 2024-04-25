using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Users;

namespace TodoApi;

public class TodoDbContext : IdentityDbContext<TodoUser>
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<Todos.Todo> Todos => Set<Todos.Todo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Todos.Todo>()
               .HasOne<TodoUser>()
               .WithMany()
               .HasForeignKey(t => t.OwnerId)
               .HasPrincipalKey(u => u.UserName);

        base.OnModelCreating(builder);
    }
}
