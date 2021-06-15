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
        [HttpPost]
        public IActionResult Preview(string key)
        {
            (string storageKey, string encryptionKey) = Encryption.ParseToken(key);
            if (!_memoryStore.Has(storageKey))
            {
                _logger.LogWarning($@"password with key {storageKey} requested, but not found");
                return NotFound();
            }
            if (HttpContext.Request.Method == "POST")
            {
                string encryptedPassword = _memoryStore.Retrieve(storageKey);
                string decrypted = Encryption.Decrypt(encryptedPassword, encryptionKey);
                return View("Password", new PreviewModel { Key = decrypted });
            }
                
             return View("Preview");
        }
    }
}
