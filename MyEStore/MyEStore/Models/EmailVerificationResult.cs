namespace MyEStore.Models
{
    public class EmailVerificationResult
    {
        public string Email { get; set; }
        public string Deliverability { get; set; }
        public bool IsValidFormat { get; set; }
        public bool IsFreeEmail { get; set; }
        public bool IsDisposableEmail { get; set; }
        public bool IsRoleEmail { get; set; }
        public string QualityScore { get; set; }
    }

}
