using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class CustomerService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<CustomerResponseDto> CreateCustomer(Guid businessId, CustomerCreateDto customerCreateDto)
    {
        var user = await _context.Businesses.FindAsync(businessId) ?? throw new KeyNotFoundException("user not found");

        var customer = new Customer
        {
            Name = customerCreateDto.Name,
            Email = customerCreateDto.Email,
            Address = customerCreateDto.Address,
            PhoneNumber = customerCreateDto.PhoneNumber,
            Company = customerCreateDto.Company,
            BusinessId = businessId,
            // Business = Business
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Company = customer.Company,
            Address = customer.Address,
            PhoneNumber = customer.PhoneNumber,
            CreatedAt = customer.CreatedAt
        };
    }

    public async Task<CustomerResponseDto> UpdateCustomer(Guid customerId, Guid businessId, CustomerCreateDto customerUpdateDto)
    {
        var customer = await _context.Customers
            .Where(c => c.Id == customerId && c.BusinessId == businessId)
            .FirstOrDefaultAsync();

        if (customer == null)
            throw new Exception("Customer not found!");

        if (!string.IsNullOrWhiteSpace(customerUpdateDto.Name))
            customer.Name = customerUpdateDto.Name;
        if (!string.IsNullOrWhiteSpace(customerUpdateDto.Email))
            customer.Email = customerUpdateDto.Email;
        if (!string.IsNullOrWhiteSpace(customerUpdateDto.Address))
            customer.Address = customerUpdateDto.Address;
        if (!string.IsNullOrWhiteSpace(customerUpdateDto.PhoneNumber))
            customer.PhoneNumber = customerUpdateDto.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(customerUpdateDto.Company))
            customer.Company = customerUpdateDto.Company;

        await _context.SaveChangesAsync();

        return new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Company = customer.Company,
            Address = customer.Address,
            PhoneNumber = customer.PhoneNumber,
            CreatedAt = customer.CreatedAt
        };
    }

    public async Task <PaginatedResponse<CustomerResponseDto>> GetCustomers(Guid businessId, PaginationParams paginationParams,
        string? Name = null,
        string? Company = null,
        string? Email = null,
        string? PhoneNumber = null)
    {
        var query = _context.Customers
            .Where(x => x.BusinessId == businessId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Name))
            query = query.Where(c => c.Name.Contains(Name));
        if (!string.IsNullOrWhiteSpace(Company))
            query = query.Where(c => c.Company.Contains(Company));
        if (!string.IsNullOrWhiteSpace(Email))
            query = query.Where(c => c.Email.Contains(Email));
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
            query = query.Where(c => c.PhoneNumber.Contains(PhoneNumber));

        var totalCount = await query.CountAsync();

        var customers = await query
            .OrderBy(u => u.Name)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(u => new CustomerResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Company = u.Company,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<CustomerResponseDto>
        {
            Items = customers,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize)
        };
    }

    public async Task DeleteCustomer(Guid customerId, Guid businessId)
    {
        var customer = await _context.Customers
        .Include(c => c.Invoices)
        .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId) ?? throw new KeyNotFoundException("Customer not found");
        if (customer.BusinessId != businessId)
            throw new UnauthorizedAccessException("You do not have permission to delete this customer.");

        if (customer.Invoices.Count != 0)
        throw new InvalidOperationException("Cannot delete customer with existing invoices.");

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }
}