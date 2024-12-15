using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MS_Back_Maps.Models
{
    public class CustomMap
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("mapID")]
        public int MapId { get; set; }

        [Column("creatorId")]
        public int CreatorId { get; set; }
        [Column("creationDate")]
        public DateTime CreationDate { get; set; }
        [Column("ratingSum")]
        public int RatingSum { get; set; }
        [Column("ratingCount")]
        public int RatingCount { get; set; }
        [Column("downloads")]
        public int Downloads { get; set; }
    }
}
