using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentspilsItc.Models;

namespace Rank.Data
{
    public class RankContext : DbContext
    {
        public RankContext (DbContextOptions<RankContext> options)
            : base(options)
        {
        }

        public DbSet<VentspilsItc.Models.Ranking> Ranking { get; set; } = default!;
    }
}
