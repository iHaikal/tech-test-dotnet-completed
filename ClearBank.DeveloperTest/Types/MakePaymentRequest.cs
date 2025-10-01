using System;
using System.ComponentModel.DataAnnotations;

namespace ClearBank.DeveloperTest.Types
{
    public class MakePaymentRequest
    {
        public string CreditorAccountNumber { get; set; }

        [Required]
        public string DebtorAccountNumber { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        [Required]
        public PaymentScheme PaymentScheme { get; set; }
    }
}
