namespace nhitomi.Database
{
    public interface IDbSupportsAvailability
    {
        double Availability { get; set; }
        double TotalAvailability { get; set; }
    }
}