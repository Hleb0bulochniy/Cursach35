using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MS_Back_Logs.Models
{
    public class Log
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("userId")]
        public int UserId { get; set; }
        [Column("dateTime")]
        public DateTime DateTime { get; set; }
        [Column("serviceName")]
        public string ServiceName { get; set; }
        [Column("logLevel")]
        public string LogLevel { get; set; }
        [Column("eventType")]
        public string EventType { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("details")]
        public string Details { get; set; }
        [Column("errorCode")]
        public string ErrorCode { get; set; }
    }
}
