﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Common.Todos;
using TodoApi.Common.Users;

namespace TodoApi.Common;

public class TodoDbContext : IdentityDbContext<TodoUser>
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Todo>()
               .HasOne<TodoUser>()
               .WithMany()
               .HasForeignKey(t => t.OwnerId)
               .HasPrincipalKey(u => u.UserName);

        base.OnModelCreating(builder);
    }
}
