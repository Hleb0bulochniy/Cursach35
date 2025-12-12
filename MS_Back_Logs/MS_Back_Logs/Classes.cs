namespace MS_Back_Logs
{
    [Serializable]
    public class LogModel
    {
        public int UserId { get; set; }
        public DateTime DateTime { get; set; }
        public string ServiceName { get; set; }
        public string LogLevel { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string ErrorCode { get; set; }
    }
}
