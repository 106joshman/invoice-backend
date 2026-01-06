using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class CustomerService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<CustomerResponseDto> CreateCustomer(
        Guid businessId,
        Guid userId,
        CustomerCreateDto customerCreateDto)
    {
        var businessUser = await _context.BusinessUsers
        .Include(bu => bu.Business)
        .FirstOrDefaultAsync(bu =>
            bu.UserId == userId &&
            bu.BusinessId == businessId &&
            bu.IsActive &&
            !bu.IsDeleted &&
            !bu.Business.IsDeleted)
        ?? throw new UnauthorizedAccessException(
            "You are not authorized to perform this action.");

        // OPTIONAL: ROLE_BASED PERMISSION CHECK
        if (businessUser.Role != "Owner" && businessUser.Role != "Admin")
        throw new UnauthorizedAccessException(
            "You do not have permission to create customers.");

        var existingCustomer = await _context.Customers
            .Where(c =>
                c.Email.ToLower() == customerCreateDto.Email &&
                c.BusinessId == businessId)
            .FirstOrDefaultAsync();

        if (existingCustomer != null)
        {
            // CHECK TO SEE IF ANY NEED TO RESTORE DELETED CUSTOMER AND UPDATE DATA
            if (existingCustomer.IsDeleted)
            {
                existingCustomer.IsDeleted = false;
                existingCustomer.Name = customerCreateDto.Name;
                existingCustomer.Address = customerCreateDto.Address;
                existingCustomer.PhoneNumber = customerCreateDto.PhoneNumber;
                existingCustomer.Company = customerCreateDto.Company;

                // 🧾 AUDIT LOG
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "RESTORE",
                    EntityName = "CUSTOMER",
                    EntityId = existingCustomer.Id,
                    UserId = userId,
                    BusinessId = businessId,
                    ChangeBy = userId.ToString()
                });

                await _context.SaveChangesAsync();

                return new CustomerResponseDto
                {
                    Id = existingCustomer.Id,
                    Name = existingCustomer.Name,
                    Email = existingCustomer.Email,
                    Company = existingCustomer.Company,
                    Address = existingCustomer.Address,
                    PhoneNumber = existingCustomer.PhoneNumber,
                    CreatedAt = existingCustomer.CreatedAt
                };
            }

            // EXIT IF CUSTOMER EXISTS AND IS NOT DELETED
            throw new InvalidOperationException("A customer with this email already exists.");
        }

        // CREATE NEW CUSTOMER
        var customer = new Customer
        {
            Name = customerCreateDto.Name,
            Email = customerCreateDto.Email,
            Address = customerCreateDto.Address,
            PhoneNumber = customerCreateDto.PhoneNumber,
            Company = customerCreateDto.Company,
            BusinessId = businessId,
        };

        _context.Customers.Add(customer);

        // 🧾 AUDIT LOG
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "CREATE_CUSTOMER",
            EntityName = "CUSTOMER",
            EntityId = customer.Id,
            UserId = userId,
            BusinessId = businessId,
            ChangeBy = userId.ToString()
        });

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

    public async Task<CustomerResponseDto> GetCustomerById(Guid customerId,  Guid businessId)
    {
        var customer = await _context.Customers
            .Where(c =>
                c.Id == customerId &&
                c.BusinessId == businessId &&
                !c.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Customer not found!");

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

    public async Task<CustomerResponseDto> UpdateCustomer(
        Guid customerId,
        Guid businessId,
        Guid userId,
        CustomerCreateDto customerUpdateDto)
    {
        var customer = await _context.Customers
            .Where(c =>
                c.Id == customerId &&
                c.BusinessId == businessId &&
                !c.IsDeleted)
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

        // 🧾 AUDIT LOG
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "UPDATE",
            EntityName = "CUSTOMER",
            EntityId = customer.Id,
            UserId = userId,
            BusinessId = businessId,
            ChangeBy = userId.ToString()
        });

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

    public async Task <PaginatedResponse<CustomerResponseDto>> GetCustomers(
        Guid businessId,
        Guid userId,
        PaginationParams paginationParams,
        string? Name = null,
        string? Company = null,
        string? Email = null,
        string? PhoneNumber = null)
    {
        var query = _context.Customers
            .Where(x =>
                x.BusinessId == businessId &&
                x.IsDeleted == false)
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

    public async Task DeleteCustomer(
        Guid customerId,
        Guid userId,
        Guid businessId)
    {
        // VERIFY THE LOGGED IN USER BELONGS TO THE BUSINESS AND HAS PERMISSION
        var businessUser = await _context.BusinessUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(bu =>
                bu.UserId == userId &&
                bu.BusinessId == businessId &&
                bu.IsActive &&
                !bu.IsDeleted)
            ?? throw new UnauthorizedAccessException("You do not have permission to perform this action.");

        // GET THE CUSTOMER
        var customer = await _context.Customers
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c =>
            c.Id == customerId &&
            c.BusinessId == businessId &&
            !c.IsDeleted)
            ?? throw new KeyNotFoundException("Customer not found");

        if (customer.Invoices.Count != 0)
        throw new InvalidOperationException("Cannot delete customer with existing invoices.");

        customer.IsDeleted = true;

        // 🧾 AUDIT LOG
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "DELETE",
            EntityName = "CUSTOMER",
            EntityId = customer.Id,
            UserId = userId,
            BusinessId = businessId,
            ChangeBy = userId.ToString()
        });

        await _context.SaveChangesAsync();
    }
}