namespace MS_Back_Logs
{
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
}
