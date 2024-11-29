using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MS_Back_Auth.Models
{
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("lastGameType")]
        public int LastGameType { get; set; }

        [Column("LastGameSize")]
        public int LastGameSize { get; set; }

        [Column("LastGameData")]
        public string LastGameData { get; set; }

        [Column("LastGameTime")]
        public int LastGameTime { get; set; }

        [Column("UpdateDate")]
        public DateTime updateDate { get; set; }
    }
}
