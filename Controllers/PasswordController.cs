using Microsoft.AspNetCore.Mvc;
using Snappass.Models;

namespace Snappass.Controllers
{
    public class PasswordController : Controller
    {
        private readonly IMemoryStore _memoryStore;

        public PasswordController(IMemoryStore memoryStore)
        {
            _memoryStore = memoryStore;
        }

        [HttpGet]
        public IActionResult Preview(string key)
        {
            if (!_memoryStore.Has(key))
            {
                return NotFound();
            }
            string encryptedPassword = _memoryStore.Retrieve(key);
            string decrypted = Encryption.Decrypt(encryptedPassword, key);
            return View("Preview", new PreviewModel { Key = decrypted });
        }
    }
}
