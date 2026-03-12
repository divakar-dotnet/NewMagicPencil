namespace NewMagicPencil.Models
{
    public class GalleryImage
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public int CategoryImageId { get; set; }       // reference to original image
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public bool IsLiked { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime AddedOn { get; set; } = DateTime.UtcNow;
    }
}