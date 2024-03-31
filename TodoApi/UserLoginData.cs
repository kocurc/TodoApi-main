namespace TodoApi
{
    public class UserLoginData
    {
        public UserLoginData(string password, string userName)
        {
            Password = password;
            UserName = userName;
        }

        public string UserName { get; init; }
        public string Password { get; init; }
    }
}
