
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asset_Management_System.Models
{
    public class BorrowerDTO
    {
        //public int HardId { get; set; }
        //[ForeignKey("HardId")]
        //public virtual Hardware? Hardware { get; set; }
        [Required(ErrorMessage = "Borrower's Name is required"), MaxLength(100)]
        public string? BorrowersName { get; set; }
        [Required(ErrorMessage = "Borrower's Department is required"), MaxLength(100)]
        public string? Department { get; set; }
        [Required(ErrorMessage = "Borrow Purpose is required"), MaxLength(100)]
        public string? BorrowPurpose { get; set; }
        public string BorrowStatus { get; set; } = "Pending";
        public DateTime BorrowedOn { get; set; }
        [Required(ErrorMessage = "Expected Return Date s required")]
        public DateTime? ReturnOn { get; set; }
        //public string? ApprovedBy { get; set; }
        [Required(ErrorMessage = "Releaser's Name is required"), MaxLength(100)]
        public string? ReleasedBy { get; set; }
        [Required(ErrorMessage = "Receiver's Name is required"), MaxLength(100)]
        public string? ReceivedBy { get; set; }

        public List<int>? HardIds { get; set; }
    }
}
