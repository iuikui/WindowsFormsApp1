using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1.Data
{
    public class DataManager
    {
        private static DataManager instance;
        private int currentUserId = 1; // Mặc định user admin (MaNguoiDung = 1)

        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataManager();
                return instance;
            }
        }

        private DataManager()
        {
            // Test connection
            if (!DatabaseConnection.Instance.TestConnection())
            {
                MessageBox.Show(
                    "Không thể kết nối đến database!\nVui lòng kiểm tra SQL Server và connection string.",
                    "Lỗi kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Lấy tất cả sách của user hiện tại (không bao gồm sách đã xóa)
        public List<Book> GetAllBooks()
        {
            List<Book> books = new List<Book>();
            string query = @"
                SELECT s.MaSach, s.TieuDe, s.MoTa, s.DuongDanAnhBia, 
                       s.DuongDanFile, s.DinhDang, s.TongSoTrang, s.TrangHienTai,
                       s.XepHang, s.YeuThich, s.NgayThem
                FROM Sach s
                WHERE s.MaNguoiDung = @MaNguoiDung
                AND NOT EXISTS (SELECT 1 FROM ThungRac tr WHERE tr.MaSach = s.MaSach)
                ORDER BY s.NgayThem DESC";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                double progress = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) &&
                                    !reader.IsDBNull(reader.GetOrdinal("TrangHienTai")))
                                {
                                    int totalPages = reader.GetInt32(reader.GetOrdinal("TongSoTrang"));
                                    int currentPage = reader.GetInt32(reader.GetOrdinal("TrangHienTai"));
                                    if (totalPages > 0)
                                        progress = (double)currentPage / totalPages * 100;
                                }

                                books.Add(new Book
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("MaSach")),
                                    Title = reader.GetString(reader.GetOrdinal("TieuDe")),
                                    Author = GetBookAuthors(reader.GetInt32(reader.GetOrdinal("MaSach"))),
                                    CoverImagePath = reader.IsDBNull(reader.GetOrdinal("DuongDanAnhBia")) ? null : reader.GetString(reader.GetOrdinal("DuongDanAnhBia")),
                                    FilePath = reader.GetString(reader.GetOrdinal("DuongDanFile")),
                                    FileType = reader.IsDBNull(reader.GetOrdinal("DinhDang")) ? "" : reader.GetString(reader.GetOrdinal("DinhDang")),
                                    Progress = progress,
                                    TotalPages = reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) ? 0 : reader.GetInt32(reader.GetOrdinal("TongSoTrang")),
                                    CurrentPage = reader.IsDBNull(reader.GetOrdinal("TrangHienTai")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangHienTai")),
                                    IsFavorite = reader.GetBoolean(reader.GetOrdinal("YeuThich")),
                                    DateAdded = reader.GetDateTime(reader.GetOrdinal("NgayThem")),
                                    IsDeleted = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy danh sách sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return books;
        }

        // Lấy danh sách tác giả của một sách
        private string GetBookAuthors(int bookId)
        {
            string query = @"
                SELECT STRING_AGG(tg.TenTacGia, ', ')
                FROM Sach_TacGia stg
                LEFT JOIN TacGia tg ON stg.MaTacGia = tg.MaTacGia
                WHERE stg.MaSach = @MaSach";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        object result = cmd.ExecuteScalar();
                        return (result != null && result != DBNull.Value) ? result.ToString() : "Unknown";
                    }
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        // Lấy sách yêu thích
        public List<Book> GetFavoriteBooks()
        {
            return GetAllBooks().Where(b => b.IsFavorite).ToList();
        }

        // Lấy sách đã xóa (từ ThungRac)
        public List<Book> GetDeletedBooks()
        {
            List<Book> books = new List<Book>();
            string query = @"
                SELECT s.MaSach, s.TieuDe, s.MoTa, s.DuongDanAnhBia, 
                       s.DuongDanFile, s.DinhDang, s.TongSoTrang, s.TrangHienTai,
                       s.XepHang, s.YeuThich, s.NgayThem
                FROM Sach s
                INNER JOIN ThungRac tr ON s.MaSach = tr.MaSach
                WHERE s.MaNguoiDung = @MaNguoiDung
                ORDER BY s.NgayThem DESC";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                double progress = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) &&
                                    !reader.IsDBNull(reader.GetOrdinal("TrangHienTai")))
                                {
                                    int totalPages = reader.GetInt32(reader.GetOrdinal("TongSoTrang"));
                                    int currentPage = reader.GetInt32(reader.GetOrdinal("TrangHienTai"));
                                    if (totalPages > 0)
                                        progress = (double)currentPage / totalPages * 100;
                                }

                                books.Add(new Book
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("MaSach")),
                                    Title = reader.GetString(reader.GetOrdinal("TieuDe")),
                                    Author = GetBookAuthors(reader.GetInt32(reader.GetOrdinal("MaSach"))),
                                    CoverImagePath = reader.IsDBNull(reader.GetOrdinal("DuongDanAnhBia")) ? null : reader.GetString(reader.GetOrdinal("DuongDanAnhBia")),
                                    FilePath = reader.GetString(reader.GetOrdinal("DuongDanFile")),
                                    FileType = reader.IsDBNull(reader.GetOrdinal("DinhDang")) ? "" : reader.GetString(reader.GetOrdinal("DinhDang")),
                                    Progress = progress,
                                    TotalPages = reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) ? 0 : reader.GetInt32(reader.GetOrdinal("TongSoTrang")),
                                    CurrentPage = reader.IsDBNull(reader.GetOrdinal("TrangHienTai")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangHienTai")),
                                    IsFavorite = reader.GetBoolean(reader.GetOrdinal("YeuThich")),
                                    DateAdded = reader.GetDateTime(reader.GetOrdinal("NgayThem")),
                                    IsDeleted = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy danh sách sách đã xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return books;
        }

        // Tìm kiếm sách
        public List<Book> SearchBooks(string query)
        {
            query = query.ToLower();
            return GetAllBooks().Where(b =>
                b.Title.ToLower().Contains(query) ||
                b.Author.ToLower().Contains(query))
                .ToList();
        }

        // Thêm sách mới
        public void AddBook(Book book)
        {
            string query = @"
                INSERT INTO Sach (MaNguoiDung, TieuDe, MoTa, DuongDanFile, DinhDang, 
                                  TongSoTrang, TrangHienTai, YeuThich, NgayThem)
                VALUES (@MaNguoiDung, @TieuDe, @MoTa, @DuongDanFile, @DinhDang, 
                        @TongSoTrang, @TrangHienTai, @YeuThich, @NgayThem);
                
                SELECT CAST(SCOPE_IDENTITY() as int);";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        cmd.Parameters.AddWithValue("@TieuDe", book.Title);
                        cmd.Parameters.AddWithValue("@MoTa", (object)book.Author ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DuongDanFile", book.FilePath);
                        cmd.Parameters.AddWithValue("@DinhDang", (object)book.FileType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TongSoTrang", book.TotalPages);
                        cmd.Parameters.AddWithValue("@TrangHienTai", book.CurrentPage);
                        cmd.Parameters.AddWithValue("@YeuThich", book.IsFavorite);
                        cmd.Parameters.AddWithValue("@NgayThem", DateTime.Now);

                        int newId = (int)cmd.ExecuteScalar();
                        book.Id = newId;

                        // Thêm tác giả nếu có
                        if (!string.IsNullOrEmpty(book.Author))
                        {
                            AddOrGetAuthor(book.Author, newId, conn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Thêm hoặc lấy tác giả
        private void AddOrGetAuthor(string authorName, int bookId, SqlConnection conn)
        {
            // Kiểm tra tác giả đã tồn tại chưa
            string checkQuery = "SELECT MaTacGia FROM TacGia WHERE TenTacGia = @TenTacGia";
            int authorId = 0;

            using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@TenTacGia", authorName);
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    authorId = (int)result;
                }
                else
                {
                    // Thêm tác giả mới
                    string insertQuery = "INSERT INTO TacGia (TenTacGia) VALUES (@TenTacGia); SELECT CAST(SCOPE_IDENTITY() as int);";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@TenTacGia", authorName);
                        authorId = (int)insertCmd.ExecuteScalar();
                    }
                }
            }

            // Liên kết sách với tác giả
            string linkQuery = "INSERT INTO Sach_TacGia (MaSach, MaTacGia) VALUES (@MaSach, @MaTacGia)";
            using (SqlCommand cmd = new SqlCommand(linkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@MaSach", bookId);
                cmd.Parameters.AddWithValue("@MaTacGia", authorId);
                cmd.ExecuteNonQuery();
            }
        }

        // Cập nhật sách
        public void UpdateBook(Book book)
        {
            string query = @"
                UPDATE Sach 
                SET TrangHienTai = @TrangHienTai,
                    YeuThich = @YeuThich
                WHERE MaSach = @MaSach";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", book.Id);
                        cmd.Parameters.AddWithValue("@TrangHienTai", book.CurrentPage);
                        cmd.Parameters.AddWithValue("@YeuThich", book.IsFavorite);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Đưa sách vào thùng rác
        public void DeleteBook(int bookId)
        {
            string query = @"
                INSERT INTO ThungRac (MaSach) 
                SELECT @MaSach 
                WHERE NOT EXISTS (SELECT 1 FROM ThungRac WHERE MaSach = @MaSach)";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Khôi phục sách từ thùng rác
        public void RestoreBook(int bookId)
        {
            string query = "DELETE FROM ThungRac WHERE MaSach = @MaSach";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khôi phục sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Xóa vĩnh viễn sách (xóa từ ThungRac và Sach)
        public void PermanentlyDeleteBook(int bookId)
        {
            string query = @"
                DELETE FROM ThungRac WHERE MaSach = @MaSach;
                DELETE FROM Sach WHERE MaSach = @MaSach";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa vĩnh viễn sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Toggle yêu thích
        public void ToggleFavorite(int bookId)
        {
            string query = "UPDATE Sach SET YeuThich = ~YeuThich WHERE MaSach = @MaSach";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật yêu thích: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Lấy danh sách shelf
        public List<string> GetShelves()
        {
            return new List<string> { "All Books", "Favorites" };
        }

        public void AddShelf(string shelfName)
        {
            // Chưa implement
        }

        public void RemoveShelf(string shelfName)
        {
            // Chưa implement
        }

        // Đặt user hiện tại
        public void SetCurrentUser(int userId)
        {
            currentUserId = userId;
        }
    }
}