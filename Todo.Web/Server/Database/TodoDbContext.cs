using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Todo.Web.Server.Database;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Todos.Todo> Todos => Set<Todos.Todo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Todos.Todo>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .HasPrincipalKey(u => u.UserName);
        base.OnModelCreating(builder);
    }
}
