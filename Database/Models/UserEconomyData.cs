namespace TShockEconomyExp.Database.Models
{
    public class UserEconomyData
    {
        public string AccountName { get; set; } = string.Empty;
        public long Balance { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
