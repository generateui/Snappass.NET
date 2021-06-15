using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Snappass.Models;

namespace Snappass.Controllers
{
    public class ShareController : Controller
    {
        private readonly IMemoryStore _memoryStore;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ShareController(IMemoryStore memoryStore, IHttpContextAccessor httpContextAccessor)
        {
            _memoryStore = memoryStore;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Share() => View("Share");

        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var host = request.Host.ToUriComponent();
            var pathBase = request.PathBase.ToUriComponent();
            return $"{request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        public IActionResult Share(string password, string ttl)
        {
            static TimeToLive Parse(string ttlText) => (ttlText.ToLower()) switch
            {
                "week" => TimeToLive.Week,
                "day" => TimeToLive.Day,
                "hour" => TimeToLive.Hour,
                _ => throw new ArgumentException("Expected week, day or hour"),
            };
            TimeToLive timeToLive = Parse(ttl);
            string storageKey = Guid.NewGuid().ToString("N").ToUpper();
            (string encryptedPassword, string encryptionKey) = Encryption.Encrypt(password);
            _memoryStore.Store(encryptedPassword, storageKey, timeToLive);
            string token = Encryption.CreateToken(storageKey, encryptionKey);
            var model = new GeneratedPassword { Token = token, BaseUri = GetBaseUrl() };
            return View("Shared", model);
        }
    }
}
