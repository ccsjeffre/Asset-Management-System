using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asset_Management_System.Models
{
    public class HardwareDTO
    {
        [Required, MaxLength(100)]
        public string? HardType { get; set; }
        [Required, MaxLength(100)]
        public string? HardLocation { get; set; }
        public string? HardStickerNum { get; set; }
        [Required, MaxLength(100)]
        public string? HardBrand { get; set; }
        [Required, MaxLength(100)]
        public string HardStatus { get; set; } = "Functional";
        [Required]
        public DateTime DateAcquisition { get; set; }

        //public int InventoryId { get; set; }
        //[ForeignKey("InventoryId")]
        //public virtual Inventory? Inventory { get; set; }
    }
}
