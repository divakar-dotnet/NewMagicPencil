using System;
using System.Collections.Generic;

namespace NewMagicPencil.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; }

        public ICollection<CategoryImage> CategoryImages { get; set; } = new List<CategoryImage>();
    }
}