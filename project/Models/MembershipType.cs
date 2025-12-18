namespace project.Models
{
    public class MembershipType
    {
        public int MembershipTypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public string? Description { get; set; }
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedDate { get; set; }
    }
}