# ClearBank Developer Test – Refactor Summary

## Overview of Changes

- Introduced interfaces to abstract data access:
  - `IAccountDataStore` with `GetAccount` and `UpdateAccount`.
  - `IAccountDataStoreFactory` to create the appropriate data store instance.
- Refactored `PaymentService` to:
  - Depend on `IAccountDataStoreFactory` (primary constructor) via dependency inversion.
  - Validate the incoming request using `System.ComponentModel.DataAnnotations`.
  - Fail fast on invalid requests or disallowed operations.
- Moved configuration access to a factory:
  - `AccountDataStoreFactory` resolves `BackupAccountDataStore` versus `AccountDataStore` using `ConfigurationManager.AppSettings["DataStoreType"]`.
- Added comprehensive unit tests using xUnit and Moq covering happy paths, failure paths, and input validation.

## What Changed and Why

### 1) Data Access Abstractions
- Added `IAccountDataStore` and updated `AccountDataStore` and `BackupAccountDataStore` to implement it.
- Added `IAccountDataStoreFactory` and `AccountDataStoreFactory`.

Why? enables dependency inversion and easy mocking for tests.

### 2) PaymentService Refactor
File: `ClearBank.DeveloperTest\Services\PaymentService.cs`

Key improvements:
- Primary constructor: `public class PaymentService(IAccountDataStoreFactory accountDataStoreFactory) : IPaymentService`.
- Request validation via DataAnnotations (`Validator.TryValidateObject`).
- Early returns on invalid input or missing account.
- Single responsibility: the service now orchestrates validation, rule checks, and persistence; it does not construct data store implementations or read configuration.

Why? removing nested conditionals, separating concerns and enabling mocks.

### 3) Input Validation
File: `ClearBank.DeveloperTest\Types\MakePaymentRequest.cs`

Changes:
- Added `[Required]` to `DebtorAccountNumber` and `PaymentScheme`.
- Added `[Range(0.01, double.MaxValue)]` to `Amount`.

Why? Centralizes validation rules with the model. 
<!-- - Prevents invalid requests (e.g., empty debtor, non-positive amounts).
- Centralizes validation rules with the model. -->

### 4) Unit Tests
File: `ClearBank.DeveloperTest.Tests\PaymentServiceTests.cs`
.
- Success paths for Bacs, FasterPayments (including exact balance), and Chaps.
- Failure when account is null.
- Failure for disallowed scheme flags.
- Failure for insufficient funds (FasterPayments).
- Failure for non-live CHAPS status.
- Failure for invalid requests (amount <= 0 or empty debtor).

<!-- Why:
- Demonstrates testability via dependency injection.
- Ensures business rules and validation behave as intended.
- Protects against regressions. -->

<!-- ## SOLID Principles

- Single Responsibility: `PaymentService` handles payment orchestration; configuration and data store selection moved to `AccountDataStoreFactory`; persistence via `IAccountDataStore`.
- Open/Closed: Scheme rules are centralized; future work can extract strategies per scheme for extension without modification.
- Liskov Substitution: `AccountDataStore` and `BackupAccountDataStore` are interchangeable via `IAccountDataStore`.
- Interface Segregation: Introduced minimal, focused interfaces (`IAccountDataStore`, `IAccountDataStoreFactory`).
- Dependency Inversion: `PaymentService` depends on abstractions; tests inject mocks to isolate behavior. -->

## Notes and Future Enhancements

- Allow amounts between `0` and `0.01` (current `[Range(0.01, ...)]` is strict). 
- Add `FailureReason` to `MakePaymentResult` for clearer failure details.  
- Replace `switch` clause in `MakePayment` method with a strategy pattern (`IPaymentRuleProvider` keyed by scheme) for cleaner enum-to-rule mapping.  
- Refactor `IAccountDataStoreFactory` to avoid `ConfigurationManager`; use DI with `IOptions<T>` (or similar) to inject config and resolve `IAccountDataStore`.  
