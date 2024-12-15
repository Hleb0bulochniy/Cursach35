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
        [Column("mapType")]
        public int MapType { get; set; }
        [Column("mapSize")]
        public int MapSize { get; set; }
        [Column("isCustom")]
        public bool IsCustom { get; set; }
        [Column("about")]
        public string About { get; set; }

        public ICollection<MapsInUser> MapsInUsers { get; set; } = null!;
        public ICollection<CustomMap> CustomMaps { get; set; } = null!;
    }
}
