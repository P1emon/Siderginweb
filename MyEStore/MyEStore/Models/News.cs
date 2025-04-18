namespace MyEStore.Models
{
    // Models/NewsItem.cs
    public class NewsItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public DateTime PublishDate { get; set; }
        public string SourceName { get; set; }
        public string SourceUrl { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public string LogoUrl { get; set; }  // Thêm thuộc tính này
    }

    // Models/NewsSource.cs
    public class NewsSource
    {
        public string Name { get; set; }
        public string RssUrl { get; set; }
        public string BaseUrl { get; set; }
        public string Category { get; set; }
        public string LogoUrl { get; set; }
    }
}
