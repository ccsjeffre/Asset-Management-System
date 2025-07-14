
namespace Asset_Management_System.Models
{
    public class InventoryDTO
    {
        public string? HardType { get; set; }
        public int AvailableQuantity { get; set; }
        public int NonFunctionalQuantity { get; set; }
        public int BorrowedQuantity { get; set; }
        public int DeployedQuantity { get; set; }
        public int TotalQuantity { get; set; }
    }


}
