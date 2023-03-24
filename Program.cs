using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using System.Formats.Asn1;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Web;

namespace scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "https://books.toscrape.com/catalogue/category/books/sports-and-games_17/index.html";
            var links = GetBookLinks(url);
            List<Book> books = GetBooks(links);
            ExportToDatabase(books);
        }

        private static void ExportToDatabase(List<Book> books)
        {

            string connectionString = "Server=192.168.178.27,3306;Database=Books;Uid=Scraper;Pwd=123Scraper21!;";

            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            foreach (var item in books)
            {
                MySqlCommand command = new MySqlCommand("INSERT INTO books (Title, Price) VALUES (@Title, @Price)", connection);
                command.Parameters.AddWithValue("@Title", item.Title);
                command.Parameters.AddWithValue("@Price", item.Price);
                command.ExecuteNonQuery();
            }
            connection.Close();

        }

        private static List<Book> GetBooks(List<string> links)
        {
            var books = new List<Book>();
            foreach (var link in links)
            {
                var doc = GetDocument(link);
                var book = new Book();
                book.Title = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//h1").InnerText);
                var xpath = "//*[@class=\"col-sm-6 product_main\"]/*[@class=\"price_color\"]";
                var price_raw = doc.DocumentNode.SelectSingleNode(xpath).InnerText;
                book.Price = ExtractPrice(price_raw);
                books.Add(book);
            }
            return books;
        }

        static double ExtractPrice(string raw)
        {
            var reg = new Regex(@"[\d\.,]+", RegexOptions.Compiled);
            var m = reg.Match(raw);
            if (!m.Success)
                return 0;

            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var priceStr = m.Value.Replace(".", decimalSeparator).Replace(",", decimalSeparator);
            return Convert.ToDouble(priceStr);
        }


        static List<string> GetBookLinks(string url)
        {
            var doc = GetDocument(url);
            var linkNodes = doc.DocumentNode.SelectNodes("//h3/a");

            var baseUri = new Uri(url);
            var links = new List<string>();
            foreach (var node in linkNodes)
            {
                var link = node.Attributes["href"].Value;
                link = new Uri(baseUri, link).AbsoluteUri;
                links.Add(link);
            }
            return links;
        }

        static HtmlDocument GetDocument(string url)
        {
            var web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);
            return doc;
        }


    }
}
