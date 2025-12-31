using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Helpers;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services
{
    public class PaymentService(ApplicationDbContext context, EncryptionHelper encryptionHelper)
    {
        private readonly ApplicationDbContext _context = context;
        private readonly EncryptionHelper _encryptionHelper = encryptionHelper;

        public async Task<PaymentInfoResponseDto> CreateOrUpdatePaymentInfoAsync(
            Guid businessId,
            Guid userId,
            PaymentInfoRequestDto dto)
        {
            var businessUser = await _context.BusinessUsers
                .Include(bu => bu.Business)
                .FirstOrDefaultAsync(bu =>
                    bu.UserId == userId &&
                    bu.BusinessId == businessId &&
                    bu.IsActive &&
                    !bu.IsDeleted &&
                    !bu.Business.IsDeleted)
                ?? throw new KeyNotFoundException("Business not found.");

            if (businessUser.Role != "Owner" && businessUser.Role != "Admin")
            throw new UnauthorizedAccessException(
                "You do not have permission to manage payment information.");


            // FIND EXISTING PAYMENT INFORMATION FOR BUSINESS
            var existingPaymentInfo = await _context.PaymentInfo
                .FirstOrDefaultAsync(p =>
                    p.BusinessId == businessId);

            // Encrypt sensitive data
            var encryptedAccountNumber = _encryptionHelper.Encrypt(dto.AccountNumber);

            if (existingPaymentInfo == null)
            {
                // Create new record
                var newPaymentInfo = new PaymentInfo
                {
                    BusinessId = businessId,
                    BankName = dto.BankName,
                    AccountName = dto.AccountName,
                    AccountNumber = encryptedAccountNumber,
                    RoutingNumber = dto.RoutingNumber,
                    SwiftCode = dto.SwiftCode,
                    IBAN = dto.IBAN,
                    PaymentTerms = dto.PaymentTerms
                };

                _context.PaymentInfo.Add(newPaymentInfo);

                _context.AuditLogs.Add(new AuditLog
                    {
                        Action = "CREATE",
                        EntityName = "PAYMENT_INFO",
                        EntityId = newPaymentInfo.Id,
                        UserId = userId,
                        BusinessId = businessId,
                        ChangeBy = userId.ToString()
                    });

                await _context.SaveChangesAsync();

                return MapToResponse(newPaymentInfo, dto.AccountNumber);
            }
            else
            {
                // UPDATE EXISTING RECORD
                existingPaymentInfo.BankName = dto.BankName;
                existingPaymentInfo.AccountName = dto.AccountName;
                existingPaymentInfo.AccountNumber = encryptedAccountNumber;
                existingPaymentInfo.RoutingNumber = dto.RoutingNumber;
                existingPaymentInfo.SwiftCode = dto.SwiftCode;
                existingPaymentInfo.IBAN = dto.IBAN;
                existingPaymentInfo.PaymentTerms = dto.PaymentTerms;

                _context.PaymentInfo.Update(existingPaymentInfo);

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "UPDATE",
                    EntityName = "PAYMENT_INFO",
                    EntityId = existingPaymentInfo.Id,
                    UserId = userId,
                    BusinessId = businessId,
                    ChangeBy = userId.ToString()
                });

                await _context.SaveChangesAsync();

                return MapToResponse(existingPaymentInfo, dto.AccountNumber);
            }
        }

        public async Task<PaymentInfoResponseDto?> GetPaymentInfoAsync(Guid businessId)
        {
            var paymentInfo = await _context.PaymentInfo
                .FirstOrDefaultAsync(p => p.BusinessId == businessId);

            if (paymentInfo == null)
                return null;

            // Decrypt account number
            var decryptedAccountNumber = _encryptionHelper.Decrypt(paymentInfo.AccountNumber);

            return MapToResponse(paymentInfo, decryptedAccountNumber);
        }

        private static PaymentInfoResponseDto MapToResponse(PaymentInfo entity, string decryptedAccountNumber)
        {
            return new PaymentInfoResponseDto
            {
                Id = entity.Id,
                BankName = entity.BankName,
                AccountName = entity.AccountName,
                AccountNumber = decryptedAccountNumber,
                RoutingNumber = entity.RoutingNumber,
                SwiftCode = entity.SwiftCode,
                IBAN = entity.IBAN,
                PaymentTerms = entity.PaymentTerms
            };
        }
    }
}
