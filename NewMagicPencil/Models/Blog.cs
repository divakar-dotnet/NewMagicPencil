using System.ComponentModel.DataAnnotations;

namespace NewMagicPencil.Models
{
    public class Blog
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string ShortDescription { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime PostedDate { get; set; }
        public string ImageUrl { get; set; }
        public string Hashtags { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}