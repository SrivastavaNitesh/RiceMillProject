using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models;

public class AdminOperationViewModel
{
    [Required] public string Role { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public List<SelectListItem> Operators { get; set; } = [];
}
