namespace Snappass.Models
{
    public class GeneratedPassword
    {
        public string Token { get; set; }
        public string BaseUri { get; set; }
        public string Uri => $@"{BaseUri}/Password/{Token}";
    }
}
