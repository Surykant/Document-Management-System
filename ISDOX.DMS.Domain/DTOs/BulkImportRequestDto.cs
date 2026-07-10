using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ISDOX.DMS.Domain.DTOs
{
    public class BulkImportRequestDto
    {
        [Required]
        public IFormFile ZipFile { get; set; } = null!; 

        [Required]
        public IFormFile? CsvFile { get; set; } 

        [Required]
        public Guid? FolderId { get; set; } 
    }
}
