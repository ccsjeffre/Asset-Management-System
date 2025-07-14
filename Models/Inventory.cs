using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asset_Management_System.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }
        public int HardId { get; set; }
        [ForeignKey("HardId")]
        public string? HardType { get; set; }
        public virtual Hardware? Hardware { get; set; }
        public int AvailableQuantity { get; set; }
        public int NonFunctionalQuantity { get; set; }
        public int BorrowedQuantity { get; set; }
        public int DeployedQuantity { get; set; }
        public int TotalQuantity { get; set; }
    }
}
