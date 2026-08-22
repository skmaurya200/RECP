using System.ComponentModel.DataAnnotations;

namespace Rec_Partapgarh.Models
{
    public class ManagerLoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(200)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }
    }
}
