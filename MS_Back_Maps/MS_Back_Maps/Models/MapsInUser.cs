using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MS_Back_Maps.Models
{
    [Index(nameof(UserId), nameof(MapId), IsUnique = true)]
    public class MapsInUser
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("userID")]
        public int UserId { get; set; }

        public Map Map { get; set; } = null!;

        [Column("mapID")]
        public int MapId { get; set; }

        [Column("gamesSum")]
        public int GamesSum { get; set; }

        [Column("wins")]
        public int Wins { get; set; }

        [Column("loses")]
        public int Loses { get; set; }

        [Column("openedTiles")]
        public int OpenedTiles { get; set; }

        [Column("openedNumberTiles")]
        public int OpenedNumberTiles { get; set; }

        [Column("openedBlankTiles")]
        public int OpenedBlankTiles { get; set; }

        [Column("flagsSum")]
        public int FlagsSum { get; set; }

        [Column("flagsOnBombs")]
        public int FlagsOnBombs { get; set; }

        [Column("timeSpentSum")]
        public int TimeSpentSum { get; set; }

        [Column("averageTime")]
        public int AverageTime { get; set; }

        [Column("lastGameData")]
        public string LastGameData { get; set; }

        [Column("lastGameTime")]
        public int LastGameTime { get; set; }
    }
}
