namespace Rec_Partapgarh.Models
{
    public class ManagerUserViewModel
    {
        public int ManagerUserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public bool IsActive { get; set; }
    }
}
