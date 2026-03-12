using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DNC.InternshipSystem.Web.Services
{
    /// <summary>
    /// Xu ly nghiep vu doanh nghiep
    /// Co chong trung lap khi tao doanh nghiep ben ngoai
    /// </summary>
    public class CompanyService : ICompanyService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(AppDbContext context, ILogger<CompanyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<Company>> GetApprovedCompanies(
            string? search, string? province, int page, int pageSize)
        {
            var query = _context.Companies
                .Where(c => c.Status == CompanyStatus.Approved && !c.IsExternal)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));

            if (!string.IsNullOrWhiteSpace(province))
                query = query.Where(c => c.Province == province);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await query
                .OrderBy(c => c.Province)
                .ThenBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Company>
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page
            };
        }

        public async Task<List<string>> GetProvinces()
        {
            return await _context.Companies
                .Where(c => c.Status == CompanyStatus.Approved && !c.IsExternal && !string.IsNullOrEmpty(c.Province))
                .Select(c => c.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }

        /// <summary>
        /// Tim doanh nghiep ben ngoai theo ten + ma so thue
        /// Neu chua ton tai thi tao moi (chong tao trung lap)
        /// </summary>
        public async Task<Company> FindOrCreateExternal(string name, string address,
            string? taxCode, string? contactPerson, string? contactEmail, string? phone)
        {
            // Tim doanh nghiep da ton tai theo Ten + MST (neu co MST)
            Company? existing = null;

            if (!string.IsNullOrWhiteSpace(taxCode))
            {
                existing = await _context.Companies
                    .FirstOrDefaultAsync(c => c.TaxCode == taxCode && c.IsExternal);
            }

            // Neu khong co MST, tim theo ten chinh xac
            if (existing == null)
            {
                existing = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Name == name && c.Address == address && c.IsExternal);
            }

            if (existing != null)
            {
                _logger.LogInformation("Tim thay doanh nghiep ben ngoai da ton tai: {Name} (ID={Id})", name, existing.Id);
                return existing;
            }

            // Tao moi neu chua ton tai
            var company = new Company
            {
                Name = name,
                Address = address,
                TaxCode = taxCode,
                ContactPerson = contactPerson,
                ContactEmail = contactEmail,
                PhoneNumber = phone,
                IsExternal = true,
                Status = CompanyStatus.Approved
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tao moi doanh nghiep ben ngoai: {Name} (ID={Id})", name, company.Id);
            return company;
        }
    }
}
