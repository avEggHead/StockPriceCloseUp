public class StockQuote
{
    public decimal Current { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal PreviousClose { get; set; }
    public long Timestamp { get; set; }

    public decimal PercentChangeFromOpen =>
    Open == 0 ? 0 : ((Current - Open) / Open) * 100;
}