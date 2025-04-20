using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;
using MyEStore.Models;
using System.ServiceModel.Syndication;

namespace MyEStore.Servicess
{
    public class RssFeedService
    {
        private List<NewsSource> _newsSources;

        public RssFeedService()
        {
            _newsSources = new List<NewsSource>
            {
                new NewsSource {
                    Name = "Sức Khỏe & Đời Sống",
                    RssUrl = "https://suckhoedoisong.vn/rss/home.rss",
                    BaseUrl = "https://suckhoedoisong.vn",
                    Category = "Y tế",
                    LogoUrl = "/images/logos/skds.png"
                },
                new NewsSource {
                    Name = "VnExpress - Sức Khỏe",
                    RssUrl = "https://vnexpress.net/rss/suc-khoe.rss",
                    BaseUrl = "https://vnexpress.net",
                    Category = "Sức khỏe",
                    LogoUrl = "/images/logos/vnexpress.png"
                },
                new NewsSource {
                    Name = "Tuổi Trẻ - Y Tế",
                    RssUrl = "https://tuoitre.vn/rss/suc-khoe.rss",
                    BaseUrl = "https://tuoitre.vn",
                    Category = "Y tế",
                    LogoUrl = "/images/logos/tuoitre.png"
                }
            };
        }

        public async Task<List<NewsItem>> GetAllNewsItemsAsync()
        {
            var allNews = new List<NewsItem>();
            var tasks = new List<Task<List<NewsItem>>>();

            foreach (var source in _newsSources)
            {
                tasks.Add(GetNewsFromSourceAsync(source));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var newsItems in results)
            {
                allNews.AddRange(newsItems);
            }

            // Sắp xếp tin mới nhất lên đầu
            return allNews.OrderByDescending(n => n.PublishDate).ToList();
        }

        public async Task<List<NewsItem>> GetNewsFromSourceAsync(NewsSource source)
        {
            var newsItems = new List<NewsItem>();

            try
            {
                using (var reader = XmlReader.Create(source.RssUrl))
                {
                    var feed = SyndicationFeed.Load(reader);

                    foreach (var item in feed.Items)
                    {
                        string imageUrl = string.Empty;

                        // 1. Kiểm tra enclosure (thường chứa hình ảnh)
                        var enclosure = item.Links.FirstOrDefault(l => l.RelationshipType == "enclosure");
                        if (enclosure != null)
                        {
                            imageUrl = enclosure.Uri.ToString();
                        }

                        // 2. Kiểm tra các phần tử mở rộng
                        if (string.IsNullOrEmpty(imageUrl))
                        {
                            // Định nghĩa namespace media
                            XNamespace mediaNamespace = "http://search.yahoo.com/mrss/";

                            foreach (var extension in item.ElementExtensions)
                            {
                                if (extension.OuterName == "thumbnail" || extension.OuterName == "content" || extension.OuterName == "image")
                                {
                                    try
                                    {
                                        // Sử dụng GetObject<XElement>() để lấy XElement từ extension
                                        var xElement = extension.GetObject<XElement>();

                                        // Lấy giá trị thuộc tính url hoặc src
                                        var urlAttr = xElement.Attribute("url")?.Value;
                                        var srcAttr = xElement.Attribute("src")?.Value;

                                        if (!string.IsNullOrEmpty(urlAttr))
                                        {
                                            imageUrl = urlAttr;
                                            break;
                                        }
                                        else if (!string.IsNullOrEmpty(srcAttr))
                                        {
                                            imageUrl = srcAttr;
                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Lỗi xử lý phần tử mở rộng: {ex.Message}");
                                        continue;
                                    }
                                }
                            }
                        }

                        // 3. Tìm trong nội dung mô tả
                        if (string.IsNullOrEmpty(imageUrl) && item.Summary != null)
                        {
                            imageUrl = ExtractImageUrl(item.Summary.Text);
                        }

                        // 4. Tìm trong nội dung mở rộng (content:encoded)
                        if (string.IsNullOrEmpty(imageUrl))
                        {
                            foreach (var extension in item.ElementExtensions)
                            {
                                if (extension.OuterName == "encoded")
                                {
                                    try
                                    {
                                        var content = extension.GetObject<XElement>().Value;
                                        if (!string.IsNullOrEmpty(content))
                                        {
                                            imageUrl = ExtractImageUrl(content);
                                            if (!string.IsNullOrEmpty(imageUrl))
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Lỗi xử lý content:encoded: {ex.Message}");
                                    }
                                }
                            }
                        }

                        var newsItem = new NewsItem
                        {
                            Title = item.Title.Text,
                            Description = CleanHtml(item.Summary?.Text ?? ""),
                            Link = item.Links.FirstOrDefault()?.Uri.AbsoluteUri,
                            PublishDate = item.PublishDate.DateTime,
                            SourceName = source.Name,
                            SourceUrl = source.BaseUrl,
                            Category = source.Category,
                            ImageUrl = imageUrl,
                            LogoUrl = source.LogoUrl
                        };

                        newsItems.Add(newsItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy tin từ {source.Name}: {ex.Message}");
            }

            return newsItems;
        }

        private string CleanHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Loại bỏ HTML tags
            var text = Regex.Replace(html, "<.*?>", string.Empty);

            // Trim và giới hạn độ dài
            text = text.Trim();
            if (text.Length > 200)
            {
                text = text.Substring(0, 197) + "...";
            }

            return text;
        }

        private string ExtractImageUrl(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
                // Phương pháp 1: Tìm trong thẻ img thông thường
                var imgMatch = Regex.Match(html, @"<img.+?src=[""'](.+?)[""'].*?>", RegexOptions.IgnoreCase);
                if (imgMatch.Success && !string.IsNullOrEmpty(imgMatch.Groups[1].Value))
                    return imgMatch.Groups[1].Value;

                // Phương pháp 2: Tìm URL hình ảnh trong nội dung CDATA
                var cdataMatch = Regex.Match(html, @"<!\[CDATA\[(.*?)\]\]>", RegexOptions.Singleline);
                if (cdataMatch.Success)
                {
                    var cdataContent = cdataMatch.Groups[1].Value;
                    var cdataImgMatch = Regex.Match(cdataContent, @"<img.+?src=[""'](.+?)[""'].*?>", RegexOptions.IgnoreCase);
                    if (cdataImgMatch.Success && !string.IsNullOrEmpty(cdataImgMatch.Groups[1].Value))
                        return cdataImgMatch.Groups[1].Value;
                }

                // Phương pháp 3: Tìm URL trực tiếp (một số nguồn chỉ cung cấp URL)
                var urlMatch = Regex.Match(html, @"https?://[^\s""'<>]+\.(jpg|jpeg|png|gif)", RegexOptions.IgnoreCase);
                if (urlMatch.Success)
                    return urlMatch.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi trích xuất URL hình ảnh: {ex.Message}");
            }

            // Ảnh mặc định nếu không tìm thấy
            return "/images/default-news.jpg";
        }
    }
}
