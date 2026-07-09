using DailySavingV.API.Entities;

namespace DailySavingV.API.Services;

public static class LoanAmortizationService
{
    public record ScheduleLine(int InstallmentNumber, DateTime DueDate, decimal Principal, decimal Interest);

    public static (List<ScheduleLine> Lines, decimal TotalPrincipal, decimal TotalInterest) BuildSchedule(
        decimal principal, decimal annualRatePercent, int termMonths, string method, DateTime startDate)
    {
        var lines = new List<ScheduleLine>();
        var monthlyRate = annualRatePercent / 100m / 12m;

        if (method == "FLAT")
        {
            // Flat: interest is charged on the original principal for the whole term,
            // split evenly across installments. Simple, common in microfinance for
            // short-term daily/weekly savings-linked loans.
            var totalInterest = Math.Round(principal * (annualRatePercent / 100m) * (termMonths / 12m), 2);
            var interestPerInstallment = Math.Round(totalInterest / termMonths, 2);
            var principalPerInstallment = Math.Round(principal / termMonths, 2);
            decimal principalAccumulated = 0, interestAccumulated = 0;

            for (int i = 1; i <= termMonths; i++)
            {
                var isLast = i == termMonths;
                var p = isLast ? principal - principalAccumulated : principalPerInstallment;
                var interest = isLast ? totalInterest - interestAccumulated : interestPerInstallment;
                principalAccumulated += p;
                interestAccumulated += interest;
                lines.Add(new ScheduleLine(i, startDate.AddMonths(i), p, interest));
            }

            return (lines, principal, totalInterest);
        }
        else
        {
            // Reducing balance (standard amortizing loan): equal total installment,
            // interest computed on the remaining balance each period.
            decimal payment;
            if (monthlyRate == 0)
            {
                payment = Math.Round(principal / termMonths, 2);
            }
            else
            {
                var factor = (double)Math.Pow(1 + (double)monthlyRate, termMonths);
                payment = Math.Round(principal * monthlyRate * (decimal)factor / ((decimal)factor - 1), 2);
            }

            decimal balance = principal;
            decimal totalPrincipal = 0, totalInterest = 0;

            for (int i = 1; i <= termMonths; i++)
            {
                var interest = Math.Round(balance * monthlyRate, 2);
                var principalPortion = i == termMonths ? balance : Math.Round(payment - interest, 2);
                if (principalPortion > balance) principalPortion = balance; // guards against rounding drift
                balance -= principalPortion;
                totalPrincipal += principalPortion;
                totalInterest += interest;
                lines.Add(new ScheduleLine(i, startDate.AddMonths(i), principalPortion, interest));
            }

            return (lines, totalPrincipal, totalInterest);
        }
    }
}
