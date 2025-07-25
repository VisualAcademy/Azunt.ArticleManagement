using Dul.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;

namespace Azunt.ArticleManagement._01_Models
{
    /// <summary>
    /// [2] Model Class: Article 모델 클래스 == Articles 테이블과 일대일로 매핑 
    /// Article, ArticleModel, ArticleViewModel, ArticleBase, ArticleDto, ArticleEntity, ArticleObject, ...
    /// </summary>
    [Table("Articles")]
    public class Article : AuditableBase
    {
        /// <summary>
        /// 일련 번호(Serial Number)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 제목
        /// </summary>
        //[Required]
        [MaxLength(255)]
        [Required(ErrorMessage = "제목을 입력하세요.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 내용
        /// </summary>
        [Required(ErrorMessage = "내용을 입력하세요.")]
        public string Content { get; set; } = ""; 

        /// <summary>
        /// 공지글로 올리기
        /// </summary>
        public bool IsPinned { get; set; } = false;
    }

    /// <summary>
    /// Repository Interface
    /// </summary>
    public interface IArticleRepository
    {
        Task<Article> AddArticleAsync(Article model);                           // 입력
        Task<List<Article>> GetArticlesAsync();                                 // 출력
        Task<Article> GetArticleByIdAsync(int id);                              // 상세
        Task<Article> EditArticleAsync(Article model);                          // 수정(Update...)
        Task DeleteArticleAsync(int id);                                        // 삭제
        Task<PagingResult<Article>> GetAllAsync(int pageIndex, int pageSize);   // 페이징
    }

    /// <summary>
    /// DbContext Class
    /// </summary>
    public class ArticleAppDbContext : DbContext
    {
        // Install-Package Microsoft.EntityFrameworkCore
        // Install-Package Microsoft.EntityFrameworkCore.SqlServer
        // Install-Package Microsoft.EntityFrameworkCore.Tools
        // Install-Package Microsoft.EntityFrameworkCore.InMemory
        // Install-Package System.Configuration.ConfigurationManager
        // Install-Package Microsoft.Data.SqlClient

        public ArticleAppDbContext()
        {
            // Empty
            // ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public ArticleAppDbContext(DbContextOptions<ArticleAppDbContext> options)
            : base(options)
        {
            // 공식과 같은 코드
            // ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 닷넷 프레임워크 기반에서 호출되는 코드 영역(.NET 6 이후로는 사용하지 않음): 
            // App.config 또는 Web.config의 연결 문자열 사용
            // 직접 데이터베이스 연결문자열 설정 가능
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = ConfigurationManager.ConnectionStrings[
                    "ConnectionString"].ConnectionString;
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Articles 테이블의 Created 열은 자동으로 GetDate() 제약 조건을 부여하기 
            modelBuilder.Entity<Article>().Property(m => m.Created).HasDefaultValueSql("GetDate()");
        }

        //[!] ArticleApp 솔루션 관련 모든 테이블에 대한 참조 
        public DbSet<Article> Articles { get; set; }
    }

    /// <summary>
    /// Repository Class: ADO.NET or Dapper or Entity Framework Core 
    /// </summary>
    public class ArticleRepository : IArticleRepository
    {
        private readonly ArticleAppDbContext _context;

        public ArticleRepository(ArticleAppDbContext context) => this._context = context;

        // 입력
        public async Task<Article> AddArticleAsync(Article model)
        {
            _context.Articles.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        // 출력 
        public async Task<List<Article>> GetArticlesAsync() => await _context.Articles.OrderByDescending(m => m.Id).ToListAsync();

        // 상세
        public async Task<Article> GetArticleByIdAsync(int id) => await _context.Articles.Where(m => m.Id == id).SingleOrDefaultAsync();

        // 수정
        public async Task<Article> EditArticleAsync(Article model)
        {
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return model;
        }

        // 삭제
        public async Task DeleteArticleAsync(int id)
        {
            var model = await _context.Articles.Where(m => m.Id == id).SingleOrDefaultAsync();
            if (model != null)
            {
                _context.Articles.Remove(model);
                await _context.SaveChangesAsync();
            }
        }

        // 페이징
        public async Task<PagingResult<Article>> GetAllAsync(int pageIndex, int pageSize)
        {
            var totalRecords = await _context.Articles.CountAsync();
            var models = await _context.Articles
                .OrderByDescending(m => m.Id)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagingResult<Article>(models, totalRecords);
        }
    }
}
