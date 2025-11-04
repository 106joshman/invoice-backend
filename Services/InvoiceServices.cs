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

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == invoiceRequestDto.CustomerId && c.UserId == userId) ?? throw new UnauthorizedAccessException("Invalid customer details.");

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceRequestDto.InvoiceNumber,
            Status = invoiceRequestDto.Status,
            IssueDate = invoiceRequestDto.IssueDate,
            DueDate = invoiceRequestDto.DueDate,
            Subtotal = invoiceRequestDto.Subtotal,
            TaxRate = invoiceRequestDto.TaxRate,
            TaxAmount = invoiceRequestDto.TaxAmount,
            Discount = invoiceRequestDto.Discount,
            Total = invoiceRequestDto.Total,
            Notes = invoiceRequestDto.Notes,
            CustomerId = invoiceRequestDto.CustomerId,
            Customer = customer,
            UserId = userId,
            User = user
        };

        // ✅ Add invoice items
        foreach (var itemDto in invoiceRequestDto.Items)
        {
            var item = new InvoiceItem
            {
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Amount = itemDto.Amount,
                Invoice = invoice
            };
            invoice.Items.Add(item);
        }

        _context.Invoices.Add(invoice);
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

    public async Task<InvoiceResponseDto> UpdateInvoice(Guid userId, Guid invoiceId, InvoiceUpdateDto  invoiceUpdateDto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.UserId == userId) ?? throw new UnauthorizedAccessException("Invoice not found or you do not have access to it.");

        if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Status))
            invoice.Status = invoiceUpdateDto.Status;

        if (!string.IsNullOrWhiteSpace(invoiceUpdateDto.Notes))
            invoice.Notes = invoiceUpdateDto.Notes;

        if (invoiceUpdateDto.Subtotal.HasValue)
            invoice.Subtotal = invoiceUpdateDto.Subtotal.Value;

        if (invoiceUpdateDto.TaxRate.HasValue)
            invoice.TaxRate = invoiceUpdateDto.TaxRate.Value;

        if (invoiceUpdateDto.TaxAmount.HasValue)
            invoice.TaxAmount = invoiceUpdateDto.TaxAmount.Value;

        if (invoiceUpdateDto.Discount.HasValue)
            invoice.Discount = invoiceUpdateDto.Discount.Value;

        if (invoiceUpdateDto.Total.HasValue)
            invoice.Total = invoiceUpdateDto.Total.Value;

        // ✅ Handle Invoice Items Update
        if (invoiceUpdateDto.Items != null && invoiceUpdateDto.Items.Count != 0)
        {
            foreach (var itemDto in invoiceUpdateDto.Items)
            {
                if (itemDto.Id.HasValue)
                {
                    // Update existing item
                    var existingItem = invoice.Items.FirstOrDefault(it => it.Id == itemDto.Id.Value);
                    if (existingItem != null)
                    {
                        existingItem.Description = itemDto.Description;
                        existingItem.Quantity = itemDto.Quantity;
                        existingItem.UnitPrice = itemDto.UnitPrice;
                        existingItem.Amount = itemDto.Amount;
                    }
                }
                else
                {
                    // Add new item
                    var newItem = new InvoiceItem
                    {
                        Description = itemDto.Description,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        Amount = itemDto.Amount,
                        InvoiceId = invoice.Id
                    };
                    invoice.Items.Add(newItem);
                }
            }
        }

        invoice.UpdatedAt = DateTime.UtcNow;

        // ✅ Save all changes
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
}