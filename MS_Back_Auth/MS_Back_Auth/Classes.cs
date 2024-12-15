namespace MS_Back_Auth
{
    [System.Serializable]
    public class RegistrationClass
    {
        public string userName { get; set; }
        public string email { get; set; }
        public string password1 { get; set; }
        public string password2 { get; set; }
        public RegistrationClass(string userName, string email, string password1, string password2)
        {
            this.userName = userName;
            this.email = email;
            this.password1 = password1;
            this.password2 = password2;
        }
    }

    [System.Serializable]
    public class LoginClass
    {
        public string userName { get; set; }
        public string password { get; set; }
        public LoginClass(string userName, string password)
        {
            this.userName = userName;
            this.password = password;
        }
    }

    [Serializable]
    public class PasswordClass
    {
        public string password { get; set; }
    }
    public class TokenResponceClass
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string username { get; set; }
    };

    //для кафки
    [Serializable]
    public class LogModel
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
    public class UserIdCheckModel
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
