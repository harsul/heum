using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Data;

public class HeumdDbContext(DbContextOptions<HeumdDbContext> options) : DbContext(options)
{
}
