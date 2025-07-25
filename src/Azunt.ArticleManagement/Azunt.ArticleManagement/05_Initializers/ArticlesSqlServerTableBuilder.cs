using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Azunt.ArticleManagement
{
    public class ArticlesSqlServerTableBuilder
    {
        private readonly string _masterConnectionString;
        private readonly ILogger<ArticlesSqlServerTableBuilder> _logger;

        public ArticlesSqlServerTableBuilder(string masterConnectionString, ILogger<ArticlesSqlServerTableBuilder> logger)
        {
            _masterConnectionString = masterConnectionString;
            _logger = logger;
        }

        public void BuildTenantDatabases()
        {
            var tenantConnectionStrings = GetTenantConnectionStrings();

            foreach (var connStr in tenantConnectionStrings)
            {
                try
                {
                    EnsureArticlesTable(connStr);
                    _logger.LogInformation($"Articles table processed (tenant DB): {connStr}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{connStr}] Error processing tenant DB");
                }
            }
        }

        public void BuildMasterDatabase()
        {
            try
            {
                EnsureArticlesTable(_masterConnectionString);
                _logger.LogInformation("Articles table processed (master DB)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing master DB");
            }
        }

        private List<string> GetTenantConnectionStrings()
        {
            var result = new List<string>();

            using (var connection = new SqlConnection(_masterConnectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT ConnectionString FROM dbo.Tenants", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var connectionString = reader["ConnectionString"]?.ToString();
                        if (!string.IsNullOrEmpty(connectionString))
                        {
                            result.Add(connectionString);
                        }
                    }
                }
            }

            return result;
        }

        private void EnsureArticlesTable(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var cmdCheck = new SqlCommand(@"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'Articles'", connection);

                int tableCount = (int)cmdCheck.ExecuteScalar();

                if (tableCount == 0)
                {
                    var cmdCreate = new SqlCommand(@"
                        CREATE TABLE [dbo].[Articles]
                        (
                            [Id] INT NOT NULL PRIMARY KEY IDENTITY(1, 1),        -- 일련번호
                            [Title] NVARCHAR(255) NOT NULL,                      -- 제목
                            [Content] NVARCHAR(MAX) NULL,                        -- 내용 => TODO: Not Null 고려
                            [IsPinned] BIT NULL DEFAULT(0),                      -- 공지글 여부
                            [CreatedBy] NVARCHAR(255) NULL,                      -- 등록자
                            [Created] DATETIME DEFAULT(GETDATE()),               -- 생성일
                            [ModifiedBy] NVARCHAR(255) NULL,                     -- 수정자
                            [Modified] DATETIME NULL                             -- 수정일
                        );
                    ", connection);

                    cmdCreate.ExecuteNonQuery();
                    _logger.LogInformation("Articles table created.");
                }

                // Insert default rows if the table is empty
                var cmdCountRows = new SqlCommand("SELECT COUNT(*) FROM [dbo].[Articles]", connection);
                int rowCount = (int)cmdCountRows.ExecuteScalar();

                if (rowCount == 0)
                {
                    var cmdInsertDefaults = new SqlCommand(@"
                        INSERT INTO [dbo].[Articles] (Title, Content, IsPinned, CreatedBy)
                        VALUES
                            (N'Welcome to the Board', N'This is the first announcement.', 1, N'(System)'),
                            (N'Sample Post', N'Feel free to write articles here.', 0, N'(System)');
                    ", connection);

                    int inserted = cmdInsertDefaults.ExecuteNonQuery();
                    _logger.LogInformation($"Inserted default articles: {inserted} rows.");
                }
            }
        }

        public static void Run(IServiceProvider services, bool forMaster, string? optionalConnectionString = null)
        {
            try
            {
                var logger = services.GetRequiredService<ILogger<ArticlesSqlServerTableBuilder>>();
                var config = services.GetRequiredService<IConfiguration>();

                string connectionString = !string.IsNullOrWhiteSpace(optionalConnectionString)
                    ? optionalConnectionString
                    : config.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");

                var builder = new ArticlesSqlServerTableBuilder(connectionString, logger);

                if (forMaster)
                {
                    builder.BuildMasterDatabase();
                }
                else
                {
                    builder.BuildTenantDatabases();
                }
            }
            catch (Exception ex)
            {
                var fallbackLogger = services.GetService<ILogger<ArticlesSqlServerTableBuilder>>();
                fallbackLogger?.LogError(ex, "Error while processing Articles table.");
            }
        }
    }
}
