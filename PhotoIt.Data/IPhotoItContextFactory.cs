using Microsoft.EntityFrameworkCore;

namespace PhotoIt.Data;

public interface IPhotoItContextFactory
{
    DbContext CreateDbContext();
}
