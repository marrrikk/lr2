namespace funny.Models
{
    public class ConnectDialogPizzaVM : ConnectDialog
    {
        public long Id { get; set; }
        public string? Size { get; set; }
        public string[]? Options { get; set; }
        public string? Thickness { get; set; }
    }
}
