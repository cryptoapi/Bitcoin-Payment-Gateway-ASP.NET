
namespace Gourl.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class GoUrlEntities : DbContext
    {
        public GoUrlEntities()
            : base("name=GoUrlEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<crypto_payments> crypto_payments { get; set; }
    }
}
