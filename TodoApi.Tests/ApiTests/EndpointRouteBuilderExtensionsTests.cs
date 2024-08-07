using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Todo.Web.Shared.Models;
using Xunit;

namespace Todo.Tests.ApiTests;

public class EndpointRouteBuilderExtensionsTests
{
    [Fact]
    public async Task GetTodos()
    {
        const string userId = "34";
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        await application.CreateUserAsync(userId);

        db.Todos.Add(new Web.Shared.Models.Todo { Title = "Thing one I have to do", OwnerId = userId });

        await db.SaveChangesAsync();

        var client = application.CreateClient(userId);
        var todos = await client.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var todo = Assert.Single(todos);

        Assert.Equal("Thing one I have to do", todo.Title);
    }

    [Fact]
    public async Task GetTodosWithoutDbUser()
    {
        const string userId = "34";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();

        var client = application.CreateClient(userId);
        var response = await client.GetAsync("/todos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostTodos()
    {
        const string userId = "34";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();

        await application.CreateUserAsync(userId);

        var client = application.CreateClient(userId);
        var response = await client.PostAsJsonAsync("/todos", new TodoItem { Title = "I want to do this thing tomorrow" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todos = await client.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var todo = Assert.Single(todos);

        Assert.Equal("I want to do this thing tomorrow", todo.Title);
        Assert.False(todo.IsComplete);
    }

    [Fact]
    public async Task DeleteTodos()
    {
        const string userId = "34";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        await application.CreateUserAsync(userId);

        db.Todos.Add(new Web.Shared.Models.Todo { Title = "I want to do this thing tomorrow", OwnerId = userId });

        await db.SaveChangesAsync();

        var client = application.CreateClient(userId);
        var todo = db.Todos.FirstOrDefault();

        Assert.NotNull(todo);
        Assert.Equal("I want to do this thing tomorrow", todo.Title);
        Assert.False(todo.IsComplete);

        var response = await client.DeleteAsync($"/todos/{todo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        todo = db.Todos.FirstOrDefault();

        Assert.Null(todo);
    }

    [Fact]
    public async Task CanOnlyGetTodosPostedBySameUser()
    {
        const string userId0 = "34";
        const string userId1 = "35";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();

        await application.CreateUserAsync(userId0);
        await application.CreateUserAsync(userId1);

        db.Todos.Add(new Web.Shared.Models.Todo { Title = "I want to do this thing tomorrow", OwnerId = userId0 });

        await db.SaveChangesAsync();

        var client0 = application.CreateClient(userId0);
        var client1 = application.CreateClient(userId1);
        var todos0 = await client0.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos0);

        var todos1 = await client1.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos1);
        Assert.Empty(todos1);

        var todo = Assert.Single(todos0);

        Assert.Equal("I want to do this thing tomorrow", todo.Title);
        Assert.False(todo.IsComplete);

        var todo0 = await client0.GetFromJsonAsync<TodoItem>($"/todos/{todo.Id}");

        Assert.NotNull(todo0);

        var response = await client1.GetAsync($"/todos/{todo.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static readonly string[] expected = new[] { "The Title field is required." };

    [Fact]
    public async Task PostingTodoWithoutTitleReturnsProblemDetails()
    {
        const string userId = "34";
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        var client = application.CreateClient(userId);
        var response = await client.PostAsJsonAsync("/todos", new TodoItem { });

        await application.CreateUserAsync(userId);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.NotEmpty(problemDetails.Errors);
        Assert.Equal(expected, problemDetails.Errors["Title"]);
    }

    [Fact]
    public async Task CannotDeleteUnownedTodos()
    {
        const string userId0 = "34";
        const string userId1 = "35";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();

        await application.CreateUserAsync(userId0);
        await application.CreateUserAsync(userId1);

        db.Todos.Add(new Web.Shared.Models.Todo { Title = "I want to do this thing tomorrow", OwnerId = userId0 });

        await db.SaveChangesAsync();

        var client0 = application.CreateClient(userId0);
        var client1 = application.CreateClient(userId1);
        var todos = await client0.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var todo = Assert.Single(todos);

        Assert.Equal("I want to do this thing tomorrow", todo.Title);
        Assert.False(todo.IsComplete);

        var response = await client1.DeleteAsync($"/todos/{todo.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var undeletedTodo = db.Todos.FirstOrDefault();

        Assert.NotNull(undeletedTodo);
        Assert.Equal(todo.Title, undeletedTodo.Title);
        Assert.Equal(todo.Id, undeletedTodo.Id);
    }

    [Fact]
    public async Task AdminCanDeleteUnownedTodos()
    {
        const string userId = "34";
        const string adminUserId = "35";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();

        await application.CreateUserAsync(userId);
        await application.CreateUserAsync(adminUserId);

        db.Todos.Add(new Web.Shared.Models.Todo { Title = "I want to do this thing tomorrow", OwnerId = userId });

        await db.SaveChangesAsync();

        var client = application.CreateClient(userId);
        var adminClient = application.CreateClient(adminUserId, isAdmin: true);
        var todos = await client.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var todo = Assert.Single(todos);

        Assert.Equal("I want to do this thing tomorrow", todo.Title);
        Assert.False(todo.IsComplete);

        var response = await adminClient.DeleteAsync($"/todos/{todo.Id}");
        var undeletedTodo = db.Todos.FirstOrDefault();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(undeletedTodo);
    }

    [Fact]
    public async Task CanUpdateOwnedTodos()
    {
        const string ownerId = "34";
        const string userId = "34";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();

        await application.CreateUserAsync(userId);

        db.Todos.Add(new Web.Shared.Models.Todo { Title = "I want to do this thing tomorrow", OwnerId = ownerId });

        await db.SaveChangesAsync();

        var client = application.CreateClient(userId);
        var todos = await client.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var todo = Assert.Single(todos);

        todo.IsComplete = true;

        var response = await client.PutAsJsonAsync($"todos/{todo.Id}", todo);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        todos = await client.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var updatedTodo = Assert.Single(todos);

        Assert.NotNull(updatedTodo);
        Assert.True(updatedTodo.IsComplete);
    }

    [Fact]
    public async Task AdminCanUpdateUnownedTodos()
    {
        const string userId = "34";
        const string adminUserId = "35";

        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();

        await application.CreateUserAsync(userId);
        await application.CreateUserAsync(adminUserId);

        db.Todos.Add(new Web.Shared.Models.Todo { Title = "I want to do this thing tomorrow", OwnerId = userId });

        await db.SaveChangesAsync();

        var client = application.CreateClient(userId);
        var adminClient = application.CreateClient(adminUserId, isAdmin: true);

        var todos = await client.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var todo = Assert.Single(todos);

        todo.IsComplete = true;

        var response = await adminClient.PutAsJsonAsync($"/todos/{todo.Id}", todo);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        todos = await client.GetFromJsonAsync<List<TodoItem>>("/todos");

        Assert.NotNull(todos);

        var updatedTodo = Assert.Single(todos);

        Assert.NotNull(updatedTodo);
        Assert.True(updatedTodo.IsComplete);
    }
}
