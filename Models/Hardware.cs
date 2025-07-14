using System.ComponentModel.DataAnnotations;

namespace Asset_Management_System.Models
{
    public class Hardware
    {
        [Key]
        public int HardId { get; set; }
        [Required, MaxLength(100)]
        public string? HardType { get; set; }
        [Required, MaxLength(100)]
        public string? HardLocation { get; set; }
        public string? HardStickerNum { get; set; }
        [Required, MaxLength(100)]
        public string? HardBrand { get; set; }
        [Required, MaxLength(100)]
        public string HardStatus { get; set; } = "Functional";
        public DateTime DateAcquisition { get; set; }
    }
}
