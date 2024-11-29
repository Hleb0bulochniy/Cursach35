using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MS_Back_Maps.Models
{
    public class Map
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("mapName")]
        public string MapName { get; set; }
        [Column("bombCount")]
        public int BombCount { get; set; }
        [Column("mapSize")]
        public int MapType { get; set; }
        [Column("mapType")]
        public int MapSize { get; set; }

        public ICollection<MapsInUser> MapsInUsers { get; set; } = null!;
    }
}
