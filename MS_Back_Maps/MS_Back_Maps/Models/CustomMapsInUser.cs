using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MS_Back_Maps.Models
{
    public class CustomMapsInUser
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("mapsInUsersMapID")]
        public int MapsInUsersMapId { get; set; }

        [Column("isAdded")]
        public bool IsAdded { get; set; }

        [Column("isFavourite")]
        public bool IsFavourite { get; set; }

        [Column("rate")]
        public int Rate { get; set; }
    }
}
