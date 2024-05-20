using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Todo.Web.Server.Filters;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Todo.Web.Server.Authentication;
using Todo.Web.Server.Database;
using CurrentUser = Todo.Web.Server.Authorization.CurrentUser;
using Todo.Web.Server.Todos;

namespace Todo.Web.Server.Extensions;

public static class IEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTodos(this IEndpointRouteBuilder routes, string todoUrl)
    {
        var group = routes.MapGroup("/todos");
        var transformBuilder = routes.ServiceProvider.GetRequiredService<ITransformBuilder>();
        var transform = transformBuilder.Create(b =>
        {
            b.AddRequestTransform(async c =>
            {
                var accessToken = await c.HttpContext.GetTokenAsync(TokenNames.AccessToken);

                c.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            });
        });

        group.RequireAuthorization();
        group.MapForwarder("{*path}", todoUrl, new ForwarderRequestConfig(), transform);
        group.WithTags("Todos");
        group.RequireAuthorization(pb => pb.RequireCurrentUser())
            .AddOpenApiSecurityRequirement();
        group.RequirePerUserRateLimit();
        group.WithParameterValidation(typeof(TodoItem));
        group.MapGet("/", async (TodoDbContext db, CurrentUser owner) =>
        {
            return await db.Todos.Where(todo => todo.OwnerId == owner.Id).Select(t => t.AsTodoItem()).AsNoTracking().ToListAsync();
        });
        group.MapGet("/{id:int}", async Task<Results<Ok<TodoItem>, NotFound>> (TodoDbContext db, int id, CurrentUser owner) =>
        {
            return await db.Todos.FindAsync(id) switch
            {
                Todos.Todo todo when todo.OwnerId == owner.Id || owner.IsAdmin => TypedResults.Ok(todo.AsTodoItem()),
                _ => TypedResults.NotFound()
            };
        });
        group.MapPost("/", async Task<Created<TodoItem>> (TodoDbContext db, TodoItem newTodo, CurrentUser owner) =>
        {
            var todo = new Todos.Todo
            {
                Title = newTodo.Title,
                OwnerId = owner.Id
            };

            db.Todos.Add(todo);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/todos/{todo.Id}", todo.AsTodoItem());
        });
        group.MapPut("/{id}", async Task<Results<Ok, NotFound, BadRequest>> (TodoDbContext db, int id, TodoItem todo, CurrentUser owner) =>
        {
            if (id != todo.Id)
            {
                return TypedResults.BadRequest();
            }

            var rowsAffected = await db.Todos.Where(t => t.Id == id && (t.OwnerId == owner.Id || owner.IsAdmin))
                .ExecuteUpdateAsync(updates =>
                    updates.SetProperty(t => t.IsComplete, todo.IsComplete)
                        .SetProperty(t => t.Title, todo.Title));

            return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
        });
        group.MapDelete("/{id:int}", async Task<Results<NotFound, Ok>> (TodoDbContext db, int id, CurrentUser owner) =>
        {
            var rowsAffected = await db.Todos.Where(t => t.Id == id && (t.OwnerId == owner.Id || owner.IsAdmin))
                .ExecuteDeleteAsync();

            return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
        });

        return group;
    }
}
