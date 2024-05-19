using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Todo.Web.Server.Users;

namespace Todo.Web.Server.Database;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : IdentityDbContext<TodoUser>(options)
{
    public DbSet<Todos.Todo> Todos => Set<Todos.Todo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Todo.Web.Server.Todos.Todo>()
            .HasOne<TodoUser>()
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .HasPrincipalKey(u => u.UserName);

        base.OnModelCreating(builder);
    }
}