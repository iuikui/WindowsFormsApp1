using System;
using System.Drawing;

namespace WindowsFormsApp1
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string CoverImagePath { get; set; }
        public double Progress { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime DateAdded { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public string ShelfName { get; set; }
        public bool IsDeleted { get; set; }

        public Book()
        {
            DateAdded = DateTime.Now;
            Progress = 0;
            IsFavorite = false;
            IsDeleted = false;
            ShelfName = "Default";
        }

        public Book(string title, string author, string coverPath, string filePath)
        {
            Title = title;
            Author = author;
            CoverImagePath = coverPath;
            FilePath = filePath;
            DateAdded = DateTime.Now;
            Progress = 0;
            IsFavorite = false;
            IsDeleted = false;
            ShelfName = "Default";
        }

        public string GetProgressText()
        {
            return $"{Progress:F2}%";
        }
    }
}