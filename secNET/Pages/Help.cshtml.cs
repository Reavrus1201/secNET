using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace secNET.Pages
{
    public class HelpModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}