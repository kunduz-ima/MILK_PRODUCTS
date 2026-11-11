using System.ComponentModel.DataAnnotations;

namespace MILK_PRODUCTS.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username must be less than 50 characters")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Password is required")]
        [StringLength(50, ErrorMessage = "Password must be less than 50 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

}
