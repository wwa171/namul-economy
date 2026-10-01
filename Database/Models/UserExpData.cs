namespace TShockEconomyExp.Database.Models
{
    public class UserExpData
    {
        public string AccountName { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public long TotalExp { get; set; } = 0;
        public DateTime LastUpdated { get; set; }
    }
}
