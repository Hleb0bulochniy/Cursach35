using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MS_Back_Maps.Models
{
    public class CustomMap
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("mapName")]
        public string MapName { get; set; }
        [Column("bombCount")]
        public int BombCount { get; set; }
        [Column("mapSize")]
        public int MapSize { get; set; }
        [Column("mapType")]
        public int MapType { get; set; }
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
        [Column("about")]
        public string About { get; set; }

        public ICollection<MapsInUser> MapsInUsers { get; set; } = null!;
    }
}
