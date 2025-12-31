using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class InvoiceServices(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<InvoiceResponseDto> CreateInvoice(Guid businessId, Guid userId, InvoiceRequestDto invoiceRequestDto)
    {
        var business = await _context.Businesses.FindAsync(businessId) ?? throw new KeyNotFoundException("business not found");

        // CHECK SUBSCRIPTION LIMIT
        if (business.SubscriptionPlan == "Free" && business.MonthlyInvoiceCount >= 2)
            throw new InvalidOperationException("Free plan users can only create 2 invoices per month. Please upgrade to continue.");



        var customer = await _context.Customers
            .FirstOrDefaultAsync(c =>
                c.Id == invoiceRequestDto.CustomerId &&
                c.BusinessId == businessId &&
                !c.IsDeleted)
            ?? throw new UnauthorizedAccessException("Invalid customer.");

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
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            Total = total,
            Notes = invoiceRequestDto.Notes,

            CustomerId = invoiceRequestDto.CustomerId,
            Customer = customer,
            CreatedByUserId = userId,
            BusinessId = businessId,

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
        business.MonthlyInvoiceCount += 1;

        // 🔐 AUDIT LOG
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "CREATE",
            EntityName = "INVOICE",
            EntityId = invoice.Id,
            UserId = userId,
            BusinessId = businessId,
            ChangeBy = userId.ToString()
        });
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
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedBy = invoice.CreatedByUser.Email,
            BusinessId = invoice.BusinessId,
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

    public async Task<LastInvoiceNumberResponseDto> GetLastInvoiceNumber(Guid businessId)
    {
        var lastInvoice = await _context.Invoices
            .Where(i =>
                i.BusinessId == businessId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        return new LastInvoiceNumberResponseDto
        {
            LastInvoiceNumber = lastInvoice?.InvoiceNumber
        };
    }

    public async Task<List<InvoiceResponseDto>> GetOutstandingInvoicesAsync(
        Guid businessId)
    {
        var outstandingStatuses = new[] { "Draft", "Sent", "Overdue" };

        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i =>
                i.BusinessId == businessId &&
                !i.IsDeleted &&
                 outstandingStatuses.Contains(i.Status))
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
            })
            .ToListAsync();

        return invoices;
    }

        public async Task<PaginatedResponse<InvoiceResponseDto>> GetAllInvoice(
        Guid userId,
        Guid businessId,
        PaginationParams paginationParams,
        string? InvoiceNumber = null,
        string? CustomerName = null,
        string? Status = null)
    {
        var businessUser = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == userId &&
                bu.BusinessId == businessId &&
                bu.IsActive &&
                !bu.IsDeleted &&
                !bu.Business.IsDeleted)
            ?? throw new KeyNotFoundException("User not found.");

        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Where(i =>
                i.BusinessId == businessId &&
                !i.IsDeleted)
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

    public async Task<InvoiceResponseDto> UpdateInvoice(
        Guid businessId,
        Guid userId,
        Guid invoiceId,
        InvoiceUpdateDto invoiceUpdateDto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i =>
                i.Id == invoiceId &&
                i.BusinessId == businessId &&
                !i.IsDeleted)
            ?? throw new UnauthorizedAccessException("Invoice not found or access denied.");

        var business = await _context.Businesses.FindAsync(businessId)
            ?? throw new KeyNotFoundException("User not found.");

        // ✅ Check if Free user has reached their invoice limit
        bool isFreeLocked =
            business.SubscriptionPlan == "Free" &&
            business.MonthlyInvoiceCount >= 2;

        // ✅ Restrict Free users when monthly invoice count is maxed
        if (isFreeLocked)
        {
            throw new InvalidOperationException(
                "Free plan reached. Upgrade to edit invoices.");
        }


        if (isFreeLocked)
        {
            // 🧾 Allow only non-financial updates
            if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Status))
                invoice.Status = invoiceUpdateDto.Status;

            if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Notes))
                invoice.Notes = invoiceUpdateDto.Notes;

            if (invoiceUpdateDto.DueDate.HasValue)
                invoice.DueDate = invoiceUpdateDto.DueDate.Value;

            if (invoiceUpdateDto.TaxRate.HasValue)
                invoice.TaxRate = invoiceUpdateDto.TaxRate.Value;

            if (invoiceUpdateDto.Discount.HasValue)
                invoice.Discount = invoiceUpdateDto.Discount.Value;

            invoice.UpdatedAt = DateTime.UtcNow;

            // 🧾 AUDIT LOG
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "UPDATE",
                EntityName = "INVOICE",
                EntityId = invoice.Id,
                UserId = userId,
                BusinessId = businessId,
                ChangeBy = userId.ToString()
            });

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
            invoice.Items = [.. invoiceUpdateDto.Items.Select(it => new InvoiceItem
            {
                Description = it.Description,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                Amount = it.Quantity * it.UnitPrice,
                InvoiceId = invoice.Id
            })];
        }

        // Recalculate financials
        invoice.Subtotal = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
        invoice.TaxAmount = invoice.Subtotal * (invoice.TaxRate / 100);
        invoice.Total = invoice.Subtotal + invoice.TaxAmount - invoice.Discount;
        invoice.UpdatedAt = DateTime.UtcNow;

        // 🔐 AUDIT LOG
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "UPDATE",
            EntityName = "INVOICE",
            EntityId = invoice.Id,
            UserId = userId,
            BusinessId = businessId,
            ChangeBy = userId.ToString()
        });

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

    public async Task<InvoiceResponseDto> GetSingleInvoiceAsync(
        Guid businessId,
        Guid userId,
        Guid invoiceId)
    {
        var businessUser = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == userId &&
                bu.BusinessId == businessId &&
                bu.IsActive &&
                !bu.IsDeleted &&
                !bu.Business.IsDeleted)
            ?? throw new KeyNotFoundException("User not found.");

        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.BusinessId == businessId)
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

    public async Task<BusinessInvoiceStatsDto> GetInvoiceStatistics(
        Guid businessId,
        Guid userId)
    {
        var businessUser = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == userId &&
                bu.BusinessId == businessId &&
                bu.IsActive &&
                !bu.IsDeleted &&
                !bu.Business.IsDeleted)
            ?? throw new UnauthorizedAccessException("Access denied.");

        var invoices = _context.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId &&
            !i.IsDeleted);

        return new BusinessInvoiceStatsDto
        {
            BusinessId =businessId,

            TotalInvoices = await invoices.CountAsync(),

            TotalBilled = await invoices
                .Where(i => i.Status != "draft" &&
                            i.Status != "cancelled")
                .SumAsync(i => i.Total),

            TotalPaid = await invoices
                .Where(i => i.Status == "paid")
                .SumAsync(i => i.Total),

            TotalOutstanding = await invoices
                .Where(i => i.Status == "sent" ||
                            i.Status == "overdue")
                .SumAsync(i => i.Total),

            TotalOverdue = await invoices
                .Where(i => i.Status == "overdue")
                .SumAsync(i => i.Total),

            DraftCount = await invoices
                .CountAsync(i => i.Status == "draft")
        };
    }


    public async Task DeleteInvoice(Guid userId, Guid invoiceId, Guid businessId)
    {
        var invoice = await _context.Invoices
        .FirstOrDefaultAsync(c =>
            c.Id == invoiceId &&
            c.BusinessId == businessId &&
            !c.IsDeleted)
        ?? throw new UnauthorizedAccessException("Invoice not found");

        // 🔒 Permission check
        var businessUser = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu =>
                bu.UserId == userId &&
                bu.BusinessId == businessId &&
                bu.IsActive)
            ?? throw new UnauthorizedAccessException("Access denied.");

        if (businessUser.Role == "Member")
            throw new UnauthorizedAccessException(
                "Members cannot delete invoices.");

        if (invoice.IsDeleted)
            return; // already deleted

        invoice.IsDeleted = true;
        foreach (var item in invoice.Items)
        {
            item.IsDeleted = true;
        }
        invoice.DeletedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        // 🧾 AUDIT LOG
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "DELETE",
            EntityName = "INVOICE",
            EntityId = invoice.Id,
            UserId = userId,
            BusinessId = businessId,
            ChangeBy = userId.ToString()
        });

        await _context.SaveChangesAsync();
    }
}