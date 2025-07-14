using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asset_Management_System.Models
{
    public class DeploymentDTO
    {
        public int HardId { get; set; }

        public virtual Hardware? Hardware { get; set; }

        [Required]
        public string? DeployArea { get; set; }

        [Required]
        public string? DeployRequestorName { get; set; }

        [Required]
        public DateTime? DeployDate { get; set; }

        public string DeployStatus { get; set; } = "Pending";

        [Required]
        public string? DeployPurpose { get; set; }

        [Required(ErrorMessage = "Releaser's Name is required"), MaxLength(100)]
        public string? ReleasedBy { get; set; }

        [Required(ErrorMessage = "Receiver's Name is required"), MaxLength(100)]
        public string? ReceivedBy { get; set; }
    }
}
