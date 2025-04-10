using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAssessment.Web.Models
{
    public class OrganizationSubscription
    {
        public int Id { get; set; }
        
        [Required]
        [ForeignKey("Organization")]
        public int OrganizationId { get; set; }
        public virtual User Organization { get; set; }
        
        [Required]
        [ForeignKey("Pricing")]
        public int PricingId { get; set; }
        public virtual Pricing Pricing { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; }
        
        public int CurrentStudentCount { get; set; }
    }
} 