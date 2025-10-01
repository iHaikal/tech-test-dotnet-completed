using System;
using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using Moq;
using Xunit;

namespace ClearBank.DeveloperTest.Tests;

public class PaymentServiceTests
{
    // Happy paths

    [Fact]
    public void MakePayment_Bacs_AllowedScheme_AccountExists_SucceedsAndUpdatesBalance()
    {
        // Arrange
        var debtor = "D-001";
        var amount = 10m;
        var initialBalance = 100m;
        var account = new Account
        {
            AccountNumber = debtor,
            Balance = initialBalance,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.Bacs
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialBalance - amount, account.Balance);
    }

    [Fact]
    public void MakePayment_FasterPayments_SufficientBalance_SucceedsAndUpdatesBalance()
    {
        // Arrange
        var debtor = "D-002";
        var amount = 25m;
        var initialBalance = 100m;
        var account = new Account
        {
            AccountNumber = debtor,
            Balance = initialBalance,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.FasterPayments
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialBalance - amount, account.Balance);
    }

    [Fact]
    public void MakePayment_Chaps_LiveAccount_SucceedsAndUpdatesBalance()
    {
        // Arrange
        var debtor = "D-003";
        var amount = 50m;
        var initialBalance = 200m;
        var account = new Account
        {
            AccountNumber = debtor,
            Balance = initialBalance,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Chaps
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.Chaps
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialBalance - amount, account.Balance);
    }

    // Failure paths

    [Theory]
    [InlineData(PaymentScheme.Bacs)]
    [InlineData(PaymentScheme.FasterPayments)]
    [InlineData(PaymentScheme.Chaps)]
    public void MakePayment_NullAccount_ReturnsFailure_NoUpdate(PaymentScheme scheme)
    {
        // Arrange
        var debtor = "D-NULL";
        var amount = 10m;

        var storeMock = new Mock<IAccountDataStore>(MockBehavior.Strict);
        storeMock.Setup(s => s.GetAccount(debtor)).Returns((Account)null);

        var factoryMock = new Mock<IAccountDataStoreFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateAccountDataStore()).Returns(storeMock.Object);

        var sut = new PaymentService(factoryMock.Object);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = scheme
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.False(result.Success);
        factoryMock.Verify(f => f.CreateAccountDataStore(), Times.Once);
        storeMock.Verify(s => s.GetAccount(debtor), Times.Once);
        storeMock.Verify(s => s.UpdateAccount(It.IsAny<Account>()), Times.Never);
    }

    [Theory]
    [InlineData(PaymentScheme.Bacs)]
    [InlineData(PaymentScheme.FasterPayments)]
    [InlineData(PaymentScheme.Chaps)]
    public void MakePayment_DisallowedSchemeFlag_ReturnsFailure_NoUpdate(PaymentScheme scheme)
    {
        // Arrange
        var debtor = "D-004";
        var amount = 10m;
        var initialBalance = 100m;

        var account = new Account
        {
            AccountNumber = debtor,
            Balance = initialBalance,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = 0 // no flags set -> disallowed for all
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = scheme
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(initialBalance, account.Balance);
    }

    [Fact]
    public void MakePayment_FasterPayments_InsufficientFunds_ReturnsFailure_NoUpdate()
    {
        // Arrange
        var debtor = "D-005";
        var amount = 100m;
        var initialBalance = 50m;

        var account = new Account
        {
            AccountNumber = debtor,
            Balance = initialBalance,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.FasterPayments
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(initialBalance, account.Balance);
    }

    [Theory]
    [InlineData(AccountStatus.Disabled)]
    [InlineData(AccountStatus.InboundPaymentsOnly)]
    public void MakePayment_Chaps_NonLiveStatus_ReturnsFailure_NoUpdate(AccountStatus status)
    {
        // Arrange
        var debtor = "D-006";
        var amount = 10m;
        var initialBalance = 100m;

        var account = new Account
        {
            AccountNumber = debtor,
            Balance = initialBalance,
            Status = status,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Chaps
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.Chaps
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(initialBalance, account.Balance);
    }

    // Invalid requests

    [Theory]
    [InlineData(0.00)]
    [InlineData(-1.00)]
    [InlineData(0.0001)]
    public void MakePayment_InvalidAmount_ReturnsFailure_NoUpdate(decimal amount)
    {
        // Arrange
        var debtor = "D-007";
        var account = new Account
        {
            AccountNumber = debtor,
            Balance = 100m,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.FasterPayments | AllowedPaymentSchemes.Chaps
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.Bacs
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void MakePayment_EmptyDebtorAccount_ReturnsFailure_NoUpdate()
    {
        // Arrange
        var debtor = ""; // invalid due to [Required]
        var account = new Account
        {
            AccountNumber = "IGNORED",
            Balance = 100m,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs
        };

        var sut = CreateSut(account, debtorToReturn: debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = 10m,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.Bacs
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void MakePayment_FasterPayments_ExactBalanceEqualsAmount_SucceedsAndZeroesBalance()
    {
        // Arrange
        var debtor = "D-008";
        var amount = 50m;
        var initialBalance = 50m;

        var account = new Account
        {
            AccountNumber = debtor,
            Balance = initialBalance,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments
        };

        var sut = CreateSut(account, debtor);

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = debtor,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentScheme = PaymentScheme.FasterPayments
        };

        // Act
        var result = sut.MakePayment(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0m, account.Balance);
    }

    private static PaymentService CreateSut(Account accountToReturn, string debtorToReturn)
    {
        var storeMock = new Mock<IAccountDataStore>(MockBehavior.Strict);
        storeMock.Setup(s => s.GetAccount(debtorToReturn)).Returns(accountToReturn);
        storeMock.Setup(s => s.UpdateAccount(It.IsAny<Account>()));

        var factoryMock = new Mock<IAccountDataStoreFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateAccountDataStore()).Returns(storeMock.Object);

        return new PaymentService(factoryMock.Object);
    }
}

