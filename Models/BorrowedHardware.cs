namespace Asset_Management_System.Models
{
    public class BorrowedHardware
    {
        public int BorrowedHardwareId { get; set; }

        public int BorrowersId { get; set; }
        public Borrower? Borrower { get; set; }

        public int HardId { get; set; }
        public Hardware? Hardware { get; set; }
    }

}
