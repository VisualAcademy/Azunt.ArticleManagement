using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System;

namespace Azunt.ArticleManagement
{
    public class ArticlesSqliteTableBuilder
    {
        private readonly string _connectionString;
        private readonly ILogger<ArticlesSqliteTableBuilder> _logger;

        public ArticlesSqliteTableBuilder(string connectionString, ILogger<ArticlesSqliteTableBuilder> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public void BuildDatabase()
        {
            try
            {
                EnsureArticlesTable(_connectionString);
                _logger.LogInformation("Articles table processed (SQLite DB)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SQLite DB");
            }
        }

        private void EnsureArticlesTable(string connectionString)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var cmdCheck = new SqliteCommand(@"
                    SELECT COUNT(*) FROM sqlite_master 
                    WHERE type = 'table' AND name = 'Articles';", connection);

                long tableCount = (long)cmdCheck.ExecuteScalar();

                if (tableCount == 0)
                {
                    var cmdCreate = new SqliteCommand(@"
                        CREATE TABLE Articles (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,     -- 일련번호
                            Title TEXT NOT NULL,                      -- 제목
                            Content TEXT NULL,                        -- 내용 (TODO: NOT NULL 고려)
                            IsPinned BOOLEAN DEFAULT 0,               -- 공지글 여부
                            CreatedBy TEXT NULL,                      -- 등록자
                            Created TEXT DEFAULT CURRENT_TIMESTAMP,  -- 생성일
                            ModifiedBy TEXT NULL,                     -- 수정자
                            Modified TEXT NULL                        -- 수정일
                        );
                    ", connection);

                    cmdCreate.ExecuteNonQuery();
                    _logger.LogInformation("Articles table created.");
                }

                var cmdCountRows = new SqliteCommand("SELECT COUNT(*) FROM Articles;", connection);
                long rowCount = (long)cmdCountRows.ExecuteScalar();

                if (rowCount == 0)
                {
                    var cmdInsert = new SqliteCommand(@"
                        INSERT INTO Articles (Title, Content, IsPinned, CreatedBy)
                        VALUES
                            ('Welcome to the Board', 'This is the first announcement.', 1, 'admin'),
                            ('Sample Post', 'Feel free to write articles here.', 0, 'user1');
                    ", connection);

                    int inserted = cmdInsert.ExecuteNonQuery();
                    _logger.LogInformation($"Inserted default articles: {inserted} rows.");
                }
            }
        }

        // Static run method for easy call from Program.cs
        public static void Run(IServiceProvider services)
        {
            try
            {
                var logger = services.GetRequiredService<ILogger<ArticlesSqliteTableBuilder>>();
                var config = services.GetRequiredService<IConfiguration>();

                var connectionString = config.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("DefaultConnection is not configured.");

                var builder = new ArticlesSqliteTableBuilder(connectionString, logger);
                builder.BuildDatabase();
            }
            catch (Exception ex)
            {
                var fallbackLogger = services.GetService<ILogger<ArticlesSqliteTableBuilder>>();
                fallbackLogger?.LogError(ex, "Error running ArticlesSqliteTableBuilder");
            }
        }
    }
}
