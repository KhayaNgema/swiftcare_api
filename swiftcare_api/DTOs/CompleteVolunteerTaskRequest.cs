using System.ComponentModel.DataAnnotations;

namespace swiftcare_api.DTOs
{
    public class CompleteVolunteerTaskRequest
    {
        [StringLength(2000)]
        public string? CompletionNotes { get; set; }
    }
}