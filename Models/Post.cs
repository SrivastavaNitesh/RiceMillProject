namespace RiceMillProject.Models
{
    public class Post
    {
        public int PostId { get; set; }
        public string PostName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
