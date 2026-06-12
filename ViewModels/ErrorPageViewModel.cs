namespace MoodBite.ViewModels
{
    public class ErrorPageViewModel
    {
        public int StatusCode { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = "alert-triangle";
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
    }
}
