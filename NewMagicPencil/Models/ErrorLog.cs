using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewMagicPencil.Models
{
    [Table("ErrorLogs")]
    public class ErrorLog
    {
        [Key]
        public int Id { get; set; }

        public string ErrorMessage { get; set; }

        [MaxLength(300)]
        public string PageName { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;
    }
}