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
}
