using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Snappass.Models;

namespace Snappass.Controllers
{
    public class PasswordController : Controller
    {
        private readonly IMemoryStore _memoryStore;
        private readonly ILogger<PasswordController> _logger;

        public PasswordController(IMemoryStore memoryStore, ILogger<PasswordController> logger)
        {
            _memoryStore = memoryStore;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Preview(string key)
        {
            if (!_memoryStore.Has(key))
            {
                _logger.LogWarning($@"password with key {key} requested, but not found");
                return NotFound();
            }
            string encryptedPassword = _memoryStore.Retrieve(key);
            string decrypted = Encryption.Decrypt(encryptedPassword, key);
            return View("Preview", new PreviewModel { Key = decrypted });
        }
    }
}
