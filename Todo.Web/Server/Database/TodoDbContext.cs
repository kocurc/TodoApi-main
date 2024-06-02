using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Todo.Web.Server.Database;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Shared.Models.Todo> Todos => Set<Shared.Models.Todo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Shared.Models.Todo>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .HasPrincipalKey(u => u.UserName);
        base.OnModelCreating(builder);
    }
}
