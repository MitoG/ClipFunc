using System.ComponentModel.DataAnnotations;

namespace ClipFunc.DataContext.Models;

public class BaseModel
{
    [Required] public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    [Required] public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}