using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;

namespace PossiLicence.Extensions
{
    public static class CompanyExtensions
    {
        private static readonly Random _random = new Random();

        public static async Task<int> GenerateUniqueIdAsync(DBContext _dbContext)
        {
            int uniqId;
            bool exists;

            do
            {
                uniqId = _random.Next(10000, 99999); // 5 haneli
                exists = await _dbContext.Companies.AnyAsync(x => x.UniqId == uniqId);
            }
            while (exists);

            return uniqId;
        }
    }
}