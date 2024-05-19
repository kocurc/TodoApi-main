using Microsoft.AspNetCore.Identity;

namespace Todo.Web.Server.Users;

// This is our TodoUser, we can modify this if we need to add custom properties to the user
public class TodoUser : IdentityUser { }

// This is the DTO used to exchange username and password details to the created user and token endpoints