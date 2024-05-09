using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ganss.Xss;
using Todo.Web.Shared;

namespace Todo.Web.Client
{
    public class TodoClient(HttpClient httpClient, HtmlSanitizer htmlSanitizer)
    {
        private readonly HtmlSanitizer _htmlSanitizer = htmlSanitizer;
        private readonly HttpClient _httpClient = httpClient;

        public async Task<TodoItem?> AddTodoAsync(string? title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return null;
            }

            TodoItem? createdTodo = null;

            var response = await _httpClient.PostAsJsonAsync("todos", new TodoItem { Title = title });

            if (response.IsSuccessStatusCode)
            {
                createdTodo = await response.Content.ReadFromJsonAsync<TodoItem>();
            }

            return createdTodo;
        }

        public async Task<bool> DeleteTodoAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"todos/{id}");

            return response.IsSuccessStatusCode;
        }

        public async Task<(HttpStatusCode, List<TodoItem>?)> GetTodosAsync()
        {
            var response = await _httpClient.GetAsync("todos");
            var statusCode = response.StatusCode;
            List<TodoItem>? todos = null;

            if (response.IsSuccessStatusCode)
            {
                todos = await response.Content.ReadFromJsonAsync<List<TodoItem>>();
            }

            return (statusCode, todos);
        }

        public async Task<bool> LoginAsync(string? userName, string? password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            var sanitizedUserName = _htmlSanitizer.Sanitize(userName);
            var sanitizedPassword = _htmlSanitizer.Sanitize(password);
            var userInfo = new UserInfo() { Username = sanitizedUserName, Password = sanitizedPassword };
            var response = await _httpClient.PostAsJsonAsync("auth/login", userInfo);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateUserAsync(string? username, string? password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("auth/register", new UserInfo { Username = username, Password = password });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> LogoutAsync()
        {
            var response = await _httpClient.PostAsync("auth/logout", content: null);
            return response.IsSuccessStatusCode;
        }
    }
}
