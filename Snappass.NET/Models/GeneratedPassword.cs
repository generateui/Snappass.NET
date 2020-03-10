namespace Snappass.Models
{
    public class GeneratedPassword
    {
        public string Key { get; set; }
        public string BaseUri { get; set; }
        public string Uri => $@"{BaseUri}/Password/{Key}";
    }
}
