using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asset_Management_System.Models
{
    public class Deployment
    {
        [Key]
        public int DeployId { get; set; }
        [Required]
        public string? DeployHardware { get; set; }
        [Required]
        public string? DeployArea { get; set; }
        [Required]
        public string? DeployRequestorName { get; set; }
        [Required]
        public DateTime? DeployDate { get; set; }
        public string DeployStatus { get; set; } = "Pending";
        [Required]
        public string? DeployPurpose { get; set; }
        public string? ApprovedBy { get; set; }
        [Required(ErrorMessage = "Releaser's Name is required"), MaxLength(100)]
        public string? ReleasedBy { get; set; }
        [Required(ErrorMessage = "Receiver's Name is required"), MaxLength(100)]
        public string? ReceivedBy { get; set; }

        public int HardId { get; set; }

        [ForeignKey("HardId")]
        public virtual Hardware? Hardware { get; set; }
    }
}
