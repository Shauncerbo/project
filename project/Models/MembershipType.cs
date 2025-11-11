namespace project.Models
{
    public class MembershipType
    {
        public int MembershipTypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
    }
}
