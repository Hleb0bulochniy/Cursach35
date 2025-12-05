namespace MS_Back_Auth
{
    [System.Serializable]
    public class RegistrationDTO
    {
        public string userName { get; set; }
        public string email { get; set; }
        public string password1 { get; set; }
        public string password2 { get; set; }
        public RegistrationDTO(string userName, string email, string password1, string password2)
        {
            this.userName = userName;
            this.email = email;
            this.password1 = password1;
            this.password2 = password2;
        }
    }

    [System.Serializable]
    public class LoginDTO
    {
        public string userName { get; set; }
        public string password { get; set; }
        public LoginDTO(string userName, string password)
        {
            this.userName = userName;
            this.password = password;
        }
    }

    [Serializable]
    public class PasswordDTO
    {
        public string password { get; set; }
    }
    public class TokenResponceDTO
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string username { get; set; }
    };

    //для кафки
    [Serializable]
    public class LogModelDTO
    {
        public int userId { get; set; }
        public DateTime dateTime { get; set; }
        public string serviceName { get; set; }
        public string logLevel { get; set; }
        public string eventType { get; set; }
        public string message { get; set; }
        public string details { get; set; }
        public string errorCode { get; set; }
    }

    [Serializable]
    public class UserIdCheckDTO
    {
        public string requestId { get; set; }
        public string requestMessage { get; set; }
        public int? userId { get; set; }
        public int? playerId { get; set; }
        public int? creatorId { get; set; }
        public bool isValid { get; set; }
        public string userName { get; set; }
    }
}
