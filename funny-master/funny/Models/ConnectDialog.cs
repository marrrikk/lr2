using System.ComponentModel.DataAnnotations;

namespace funny.Models;

public class ConnectDialog
{
    public string[]? RequiredTask { get; set; }
    [Required]
    public string Name { get; set; } = "";
    [Required]
    [RegularExpression(@"[+0-9() \-]{7,25}")]
    public string Phone { get; set; } = "";
}
