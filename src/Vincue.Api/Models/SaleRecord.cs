namespace Vin.Api.Models;

public class SaleRecord
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int DaysOnLot { get; set; }
    public DateTime SoldDate { get; set; }
}
