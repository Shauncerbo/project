namespace project.Models
{
    public class MemberTrainer
    {
        public int MemberTrainerID { get; set; }
        public int MemberID { get; set; }
        public int TrainerID { get; set; }

        public Member? Member { get; set; }
        public Trainer? Trainer { get; set; }
    }
}
