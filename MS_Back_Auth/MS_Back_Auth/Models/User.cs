using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MS_Back_Auth.Models
{
    public class User
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("playerId")]
        public int? PlayerId { get; set; } = null!;
        [Column("creatorId")]
        public int? CreatorId { get; set; } = null!;

        [Column("username")]
        public string Username { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string Password { get; set; }
    }
}
