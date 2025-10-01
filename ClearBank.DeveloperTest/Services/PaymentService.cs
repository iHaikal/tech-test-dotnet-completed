using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Types;
using System.ComponentModel.DataAnnotations;

namespace ClearBank.DeveloperTest.Services
{
    public class PaymentService(IAccountDataStoreFactory accountDataStoreFactory) : IPaymentService
    {
        public MakePaymentResult MakePayment(MakePaymentRequest request)
        {
            var validationContext = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, validationContext, null, true))
            {
                return new MakePaymentResult { Success = false };
            }

            var accountDataStore = accountDataStoreFactory.CreateAccountDataStore();
            var account = accountDataStore.GetAccount(request.DebtorAccountNumber);
            if (account == null)
            {
                return new MakePaymentResult { Success = false };
            }

            var isAllowed = request.PaymentScheme switch
            {
                PaymentScheme.Bacs =>
                    account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Bacs),

                PaymentScheme.FasterPayments =>
                    account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.FasterPayments)
                    && account.Balance >= request.Amount,

                PaymentScheme.Chaps =>
                    account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Chaps)
                    && account.Status == AccountStatus.Live,

                _ => false
            };

            if (!isAllowed)
            {
                return new MakePaymentResult { Success = false };
            }

            account.Balance -= request.Amount;
            accountDataStore.UpdateAccount(account);

            return new MakePaymentResult { Success = true };
        }
    }
}
