using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class InvoiceServices(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<InvoiceResponseDto> CreateInvoice(Guid userId, InvoiceRequestDto invoiceRequestDto)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new KeyNotFoundException("user not found");

        // ✅ Reset invoice count if new month
        if (user.LastInvoiceReset == null || user.LastInvoiceReset.Value.Month != DateTime.UtcNow.Month)
        {
            user.MonthlyInvoiceCount = 0;
            user.LastInvoiceReset = DateTime.UtcNow;
        }

        // CHECK SUBSCRIPTION LIMIT
        if (user.SubscriptionPlan == "Free" && user.MonthlyInvoiceCount >= 2)
            throw new InvalidOperationException("Free plan users can only create 2 invoices per month. Please upgrade to continue.");

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == invoiceRequestDto.CustomerId && c.UserId == userId) ?? throw new UnauthorizedAccessException("Invalid customer details.");

        // ✅ Calculate subtotal, tax, total from backend
        var subtotal = invoiceRequestDto.Items.Sum(i => i.Quantity * i.UnitPrice);
        var taxAmount = subtotal * (invoiceRequestDto.TaxRate / 100);
        var total = subtotal + taxAmount - invoiceRequestDto.Discount;

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceRequestDto.InvoiceNumber,
            Status = invoiceRequestDto.Status,
            IssueDate = invoiceRequestDto.IssueDate,
            DueDate = invoiceRequestDto.DueDate,
            TaxRate = invoiceRequestDto.TaxRate,
            Discount = invoiceRequestDto.Discount,
            Notes = invoiceRequestDto.Notes,
            CustomerId = invoiceRequestDto.CustomerId,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            Total = total,
            Customer = customer,
            UserId = userId,
            User = user,
            // ✅ Add invoice items
            Items = [.. invoiceRequestDto.Items.Select(item => new InvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Quantity * item.UnitPrice,
            })]
        };

        _context.Invoices.Add(invoice);
        user.MonthlyInvoiceCount += 1;
        await _context.SaveChangesAsync();

        // ✅ Map to response DTO
        var response = new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            TaxRate = invoice.TaxRate,
            TaxAmount = invoice.TaxAmount,
            Discount = invoice.Discount,
            Total = invoice.Total,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            Customer = new CustomerResponseDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Company = customer.Company,
                Address = customer.Address,
                PhoneNumber = customer.PhoneNumber,
                CreatedAt = customer.CreatedAt
            },
            Items = [.. invoice.Items.Select(it => new InvoiceItemResponseDto
            {
                Id = it.Id,
                Description = it.Description,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                Amount = it.Amount
            })]
        };

        return response;
    }

    public async Task<LastInvoiceNumberResponseDto> GetLastInvoiceNumber(Guid userId)
    {
        var lastInvoice = await _context.Invoices
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        return new LastInvoiceNumberResponseDto
        {
            LastInvoiceNumber = lastInvoice?.InvoiceNumber
        };
    }

    public async Task<PaginatedResponse<InvoiceResponseDto>> GetAllInvoice(Guid userId, PaginationParams paginationParams,
        string? InvoiceNumber = null,
        string? CustomerName = null,
        string? Status = null)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Where(i => i.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(InvoiceNumber))
            query = query.Where(i => i.InvoiceNumber.Contains(InvoiceNumber));
        if (!string.IsNullOrWhiteSpace(CustomerName))
            query = query.Where(i => i.Customer.Name.Contains(CustomerName));
        if (!string.IsNullOrWhiteSpace(Status))
            query = query.Where(i => i.Status.Contains(Status));

        var totalCount = await query.CountAsync();

        var invoices = await query
            .OrderBy(u => u.InvoiceNumber)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(i => new InvoiceResponseDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Status = i.Status,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                Subtotal = i.Subtotal,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                Discount = i.Discount,
                Total = i.Total,
                Notes = i.Notes,
                CreatedAt = i.CreatedAt,
                Customer = new CustomerResponseDto
                {
                    Id = i.Customer.Id,
                    Name = i.Customer.Name,
                    Email = i.Customer.Email,
                    Company = i.Customer.Company,
                    Address = i.Customer.Address,
                    PhoneNumber = i.Customer.PhoneNumber,
                    CreatedAt = i.Customer.CreatedAt
                },
                Items = i.Items.Select(it => new InvoiceItemResponseDto
                {
                    Id = it.Id,
                    Description = it.Description,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Amount = it.Amount
                }).ToList()
            })
            .ToListAsync();

        return new PaginatedResponse<InvoiceResponseDto>
        {
            Items = invoices,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize)
        };
    }

    public async Task<InvoiceResponseDto> UpdateInvoice(Guid userId, Guid invoiceId, InvoiceUpdateDto invoiceUpdateDto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.UserId == userId)
            ?? throw new UnauthorizedAccessException("Invoice not found or you do not have access to it.");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        // ✅ Check if Free user has reached their invoice limit
        bool isFreeUser = user.SubscriptionPlan == "Free" && user.MonthlyInvoiceCount >= 2;

        if (isFreeUser)
        {
            // 🧾 Allow only non-financial updates
            if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Status))
                invoice.Status = invoiceUpdateDto.Status;

            if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Notes))
                invoice.Notes = invoiceUpdateDto.Notes;

            if (invoiceUpdateDto.DueDate.HasValue)
                invoice.DueDate = invoiceUpdateDto.DueDate.Value;

            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Status = invoice.Status,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                Subtotal = invoice.Subtotal,
                TaxRate = invoice.TaxRate,
                TaxAmount = invoice.TaxAmount,
                Discount = invoice.Discount,
                Total = invoice.Total,
                Notes = invoice.Notes,
                CreatedAt = invoice.CreatedAt,
                Customer = new CustomerResponseDto
                {
                    Id = invoice.Customer.Id,
                    Name = invoice.Customer.Name,
                    Email = invoice.Customer.Email,
                    Company = invoice.Customer.Company,
                    Address = invoice.Customer.Address,
                    PhoneNumber = invoice.Customer.PhoneNumber,
                    CreatedAt = invoice.Customer.CreatedAt
                },
                Items = invoice.Items.Select(it => new InvoiceItemResponseDto
                {
                    Id = it.Id,
                    Description = it.Description,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Amount = it.Amount
                }).ToList()
            };
        }

        if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Status))
            invoice.Status = invoiceUpdateDto.Status;

        if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Notes))
            invoice.Notes = invoiceUpdateDto.Notes;

        if (invoiceUpdateDto.DueDate.HasValue)
            invoice.DueDate = invoiceUpdateDto.DueDate.Value;

        // ✅ Handle Invoice Items Update
        if (invoiceUpdateDto.Items != null && invoiceUpdateDto.Items.Count > 0)
        {
            // Clear existing items
            _context.InvoiceItems.RemoveRange(invoice.Items);

            // Add new items and recalculate amounts
            invoice.Items = invoiceUpdateDto.Items.Select(it => new InvoiceItem
            {
                Description = it.Description,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                Amount = it.Quantity * it.UnitPrice,
                InvoiceId = invoice.Id
            }).ToList();
        }

        // Recalculate financials
        invoice.Subtotal = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
        invoice.TaxAmount = invoice.Subtotal * (invoice.TaxRate / 100);
        invoice.Total = invoice.Subtotal + invoice.TaxAmount - invoice.Discount;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // ✅ Return updated invoice DTO
        return new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            TaxRate = invoice.TaxRate,
            TaxAmount = invoice.TaxAmount,
            Discount = invoice.Discount,
            Total = invoice.Total,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            Customer = new CustomerResponseDto
            {
                Id = invoice.Customer.Id,
                Name = invoice.Customer.Name,
                Email = invoice.Customer.Email,
                Company = invoice.Customer.Company,
                Address = invoice.Customer.Address,
                PhoneNumber = invoice.Customer.PhoneNumber,
                CreatedAt = invoice.Customer.CreatedAt
            },
            Items = invoice.Items.Select(it => new InvoiceItemResponseDto
            {
                Id = it.Id,
                Description = it.Description,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                Amount = it.Amount // computed
            }).ToList()
        };
    }

    public async Task<InvoiceResponseDto> GetSingleInvoiceAsync(Guid userId, Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.UserId == userId)
            ?? throw new KeyNotFoundException("Invoice not found or you don't have permission to view it.");
            return new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            TaxRate = invoice.TaxRate,
            TaxAmount = invoice.TaxAmount,
            Discount = invoice.Discount,
            Total = invoice.Total,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,

            Customer = new CustomerResponseDto
            {
                Id = invoice.Customer.Id,
                Name = invoice.Customer.Name,
                Email = invoice.Customer.Email,
                Company = invoice.Customer.Company,
                Address = invoice.Customer.Address,
                PhoneNumber = invoice.Customer.PhoneNumber,
                CreatedAt = invoice.Customer.CreatedAt
            },

            Items = [.. invoice.Items.Select(it => new InvoiceItemResponseDto
            {
                Id = it.Id,
                Description = it.Description,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                Amount = it.Amount
            })]
        };
    }
}