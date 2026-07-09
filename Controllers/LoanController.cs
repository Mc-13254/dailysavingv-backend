using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/loans")]
[Authorize]
public class LoanController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly Services.INotificationService _notifications;

    public LoanController(AppDbContext db, ICurrentUserService currentUser, Services.INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    // ---- Loan Products (simplified: direct admin config, no Maker-Checker) ----

    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<LoanProductDto>>> GetProducts()
    {
        var products = await _db.LoanProducts.OrderBy(p => p.Name).ToListAsync();
        return Ok(products.Select(ToDto));
    }

    [HttpPost("products")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<LoanProductDto>> CreateProduct(CreateLoanProductRequest request)
    {
        var product = new LoanProduct
        {
            Code = request.Code, Name = request.Name, InterestMethod = request.InterestMethod,
            AnnualInterestRate = request.AnnualInterestRate, MinAmount = request.MinAmount, MaxAmount = request.MaxAmount,
            MinTermMonths = request.MinTermMonths, MaxTermMonths = request.MaxTermMonths,
            PenaltyRatePerDay = request.PenaltyRatePerDay, GracePeriodDays = request.GracePeriodDays,
            CreatedBy = _currentUser.CodeUser
        };
        _db.LoanProducts.Add(product);
        await _db.SaveChangesAsync();
        return Ok(ToDto(product));
    }

    private static LoanProductDto ToDto(LoanProduct p) => new(
        p.LoanProductID, p.Code, p.Name, p.InterestMethod, p.AnnualInterestRate,
        p.MinAmount, p.MaxAmount, p.MinTermMonths, p.MaxTermMonths, p.PenaltyRatePerDay, p.GracePeriodDays, p.Statut
    );

    // ---- Loan Applications --------------------------------------------------

    [HttpGet("applications")]
    public async Task<ActionResult<IEnumerable<LoanApplicationRowDto>>> GetApplications([FromQuery] string? status)
    {
        var query = _db.LoanApplications.Include(a => a.Client).Include(a => a.LoanProduct).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);

        var apps = await query.OrderByDescending(a => a.RequestDate).Take(300).ToListAsync();
        return Ok(apps.Select(a => new LoanApplicationRowDto(
            a.LoanApplicationID, a.ClientID, a.Client != null ? $"{a.Client.Nom} {a.Client.Prenom}".Trim() : a.ClientID,
            a.LoanProduct?.Name ?? "—", a.RequestedAmount, a.RequestedTermMonths, a.ApprovedAmount, a.ApprovedTermMonths,
            a.Status, a.RequestedBy, a.RequestDate, a.ApprovedBy, a.ApprovalDate
        )));
    }

    [HttpPost("applications")]
    public async Task<ActionResult> CreateApplication(CreateLoanApplicationRequest request)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == request.ClientID)
            ?? throw new KeyNotFoundException("Client introuvable.");
        if (client.ValidationStatus != "VALIDATED")
            throw new InvalidOperationException("Le client doit être actif (validé) pour soumettre une demande de prêt.");
        if (client.IsBlacklisted)
            throw new InvalidOperationException("Ce client est sur liste noire — demande refusée.");

        var product = await _db.LoanProducts.FirstOrDefaultAsync(p => p.LoanProductID == request.LoanProductID && p.Statut == "ACTIVE")
            ?? throw new KeyNotFoundException("Produit de prêt introuvable ou inactif.");

        if (request.RequestedAmount < product.MinAmount || request.RequestedAmount > product.MaxAmount)
            throw new InvalidOperationException($"Le montant doit être compris entre {product.MinAmount:N0} et {product.MaxAmount:N0} pour ce produit.");
        if (request.RequestedTermMonths < product.MinTermMonths || request.RequestedTermMonths > product.MaxTermMonths)
            throw new InvalidOperationException($"La durée doit être comprise entre {product.MinTermMonths} et {product.MaxTermMonths} mois pour ce produit.");

        // A client cannot have two active loans stacking indefinitely without oversight —
        // simplification: block a new application while another is still ACTIVE.
        var hasActiveLoan = await _db.Loans.AnyAsync(l => l.ClientID == request.ClientID && l.Status == "ACTIVE");
        if (hasActiveLoan)
            throw new InvalidOperationException("Ce client a déjà un prêt actif. Il doit le solder avant d'en demander un nouveau.");

        var app = new LoanApplication
        {
            ClientID = request.ClientID,
            LoanProductID = request.LoanProductID,
            AgenceID = _currentUser.AgenceID ?? client.AgenceID,
            CollectorID = client.CollectorID,
            RequestedAmount = request.RequestedAmount,
            RequestedTermMonths = request.RequestedTermMonths,
            Purpose = request.Purpose,
            RequestedBy = _currentUser.CodeUser!
        };
        _db.LoanApplications.Add(app);
        await _db.SaveChangesAsync();

        await _notifications.SendToSupervisorsAsync(
            app.AgenceID, "Nouvelle demande de prêt",
            $"{client.Nom} {client.Prenom} demande {request.RequestedAmount:N0} sur {request.RequestedTermMonths} mois.",
            "INFO", "/loans"
        );

        return Ok(new { message = "Demande de prêt soumise pour approbation.", loanApplicationId = app.LoanApplicationID });
    }

    [HttpPost("applications/{id:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> ApproveApplication(int id, ApproveLoanApplicationRequest request)
    {
        var app = await _db.LoanApplications.FirstOrDefaultAsync(a => a.LoanApplicationID == id)
            ?? throw new KeyNotFoundException("Demande introuvable.");
        if (app.Status != "PENDING") throw new InvalidOperationException("Cette demande a déjà été traitée.");

        app.Status = "APPROVED";
        app.ApprovedAmount = request.ApprovedAmount;
        app.ApprovedTermMonths = request.ApprovedTermMonths;
        app.ApprovedBy = _currentUser.CodeUser;
        app.ApprovalDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Demande de prêt approuvée. Prêt à être décaissé." });
    }

    [HttpPost("applications/{id:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> RejectApplication(int id, RejectLoanApplicationRequest request)
    {
        var app = await _db.LoanApplications.FirstOrDefaultAsync(a => a.LoanApplicationID == id)
            ?? throw new KeyNotFoundException("Demande introuvable.");
        if (app.Status != "PENDING") throw new InvalidOperationException("Cette demande a déjà été traitée.");

        app.Status = "REJECTED";
        app.RejectionReason = request.Reason;
        app.ApprovedBy = _currentUser.CodeUser;
        app.ApprovalDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Demande de prêt rejetée." });
    }

    [HttpPost("applications/{id:int}/disburse")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<LoanDetailDto>> Disburse(int id, DisburseLoanRequest request)
    {
        var app = await _db.LoanApplications.Include(a => a.LoanProduct).FirstOrDefaultAsync(a => a.LoanApplicationID == id)
            ?? throw new KeyNotFoundException("Demande introuvable.");
        if (app.Status != "APPROVED") throw new InvalidOperationException("Seule une demande approuvée peut être décaissée.");

        var product = app.LoanProduct!;
        var principal = app.ApprovedAmount!.Value;
        var term = app.ApprovedTermMonths!.Value;
        var disbursementDate = DateTime.UtcNow;

        var (lines, totalPrincipal, totalInterest) = LoanAmortizationService.BuildSchedule(
            principal, product.AnnualInterestRate, term, product.InterestMethod, disbursementDate);

        var count = await _db.Loans.CountAsync();
        var loan = new Loan
        {
            LoanApplicationID = app.LoanApplicationID,
            LoanNumber = $"LN-{disbursementDate:yyyyMM}-{(count + 1):D5}",
            ClientID = app.ClientID,
            LoanProductID = app.LoanProductID,
            AgenceID = app.AgenceID,
            CollectorID = app.CollectorID,
            PrincipalAmount = principal,
            AnnualInterestRate = product.AnnualInterestRate,
            InterestMethod = product.InterestMethod,
            TermMonths = term,
            DisbursementDate = disbursementDate,
            DisbursedToAccountID = request.DisbursedToAccountID,
            DisbursedBy = _currentUser.CodeUser!,
            TotalPrincipal = totalPrincipal,
            TotalInterest = totalInterest,
            OutstandingPrincipal = totalPrincipal,
            OutstandingInterest = totalInterest,
            NextDueDate = lines.First().DueDate
        };
        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();

        foreach (var line in lines)
        {
            _db.LoanInstallments.Add(new LoanInstallment
            {
                LoanID = loan.LoanID,
                InstallmentNumber = line.InstallmentNumber,
                DueDate = line.DueDate,
                PrincipalDue = line.Principal,
                InterestDue = line.Interest
            });
        }

        // Optional: credit the disbursement to a client account if one was specified.
        if (!string.IsNullOrWhiteSpace(request.DisbursedToAccountID))
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == request.DisbursedToAccountID);
            if (account != null)
            {
                account.Balance += principal;
                account.AvailableBalance += principal;
            }
        }

        app.Status = "DISBURSED";
        await _db.SaveChangesAsync();

        return await GetLoanDetail(loan.LoanID);
    }

    // ---- Active Loans --------------------------------------------------------

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanRowDto>>> GetLoans([FromQuery] string? status)
    {
        var query = _db.Loans.Include(l => l.Client).Include(l => l.LoanProduct).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);

        var loans = await query.OrderByDescending(l => l.DisbursementDate).Take(300).ToListAsync();
        var loanIds = loans.Select(l => l.LoanID).ToList();
        var overdueCounts = await _db.LoanInstallments
            .Where(i => loanIds.Contains(i.LoanID) && i.Status != "PAID" && i.DueDate < DateTime.UtcNow)
            .GroupBy(i => i.LoanID)
            .Select(g => new { LoanID = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(loans.Select(l => new LoanRowDto(
            l.LoanID, l.LoanNumber, l.ClientID, l.Client != null ? $"{l.Client.Nom} {l.Client.Prenom}".Trim() : l.ClientID,
            l.LoanProduct?.Name ?? "—", l.PrincipalAmount, l.OutstandingPrincipal, l.OutstandingInterest, l.OutstandingPenalty,
            l.DisbursementDate, l.NextDueDate, l.Status, overdueCounts.FirstOrDefault(o => o.LoanID == l.LoanID)?.Count ?? 0
        )));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoanDetailDto>> GetLoanDetail(int id)
    {
        var loan = await _db.Loans.Include(l => l.Client).Include(l => l.LoanProduct)
            .FirstOrDefaultAsync(l => l.LoanID == id)
            ?? throw new KeyNotFoundException("Prêt introuvable.");

        var installments = await _db.LoanInstallments.Where(i => i.LoanID == id).OrderBy(i => i.InstallmentNumber).ToListAsync();
        var repayments = await _db.LoanRepayments.Where(r => r.LoanID == id).OrderByDescending(r => r.PaymentDate).ToListAsync();
        var agence = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AgenceID == loan.AgenceID);

        return Ok(new LoanDetailDto(
            loan.LoanID, loan.LoanNumber, loan.ClientID, loan.Client != null ? $"{loan.Client.Nom} {loan.Client.Prenom}".Trim() : loan.ClientID,
            agence?.Nom ?? "—", loan.LoanProduct?.Name ?? "—", loan.AnnualInterestRate, loan.InterestMethod,
            loan.PrincipalAmount, loan.TermMonths, loan.DisbursementDate, loan.DisbursedBy, loan.DisbursedToAccountID,
            loan.TotalPrincipal, loan.TotalInterest, loan.OutstandingPrincipal, loan.OutstandingInterest, loan.OutstandingPenalty,
            loan.NextDueDate, loan.Status, loan.WriteOffReason,
            installments.Select(i => new LoanInstallmentDto(
                i.LoanInstallmentID, i.InstallmentNumber, i.DueDate, i.PrincipalDue, i.InterestDue, i.PenaltyDue,
                i.PrincipalDue + i.InterestDue + i.PenaltyDue, i.PrincipalPaid, i.InterestPaid, i.PenaltyPaid,
                i.PrincipalPaid + i.InterestPaid + i.PenaltyPaid, i.PaidDate, i.Status
            )).ToList(),
            repayments.Select(r => new LoanRepaymentDto(r.LoanRepaymentID, r.Amount, r.PrincipalPaid, r.InterestPaid, r.PenaltyPaid, r.ReceiptNumber, r.PaymentDate, r.ReceivedBy)).ToList()
        ));
    }

    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult<IEnumerable<LoanInstallmentDto>>> GetSchedule(int id)
    {
        var installments = await _db.LoanInstallments.Where(i => i.LoanID == id).OrderBy(i => i.InstallmentNumber).ToListAsync();
        return Ok(installments.Select(i => new LoanInstallmentDto(
            i.LoanInstallmentID, i.InstallmentNumber, i.DueDate, i.PrincipalDue, i.InterestDue, i.PenaltyDue,
            i.PrincipalDue + i.InterestDue + i.PenaltyDue, i.PrincipalPaid, i.InterestPaid, i.PenaltyPaid,
            i.PrincipalPaid + i.InterestPaid + i.PenaltyPaid, i.PaidDate, i.Status
        )));
    }

    // ---- Repayment (allocates: penalty first, then interest, then principal) ----

    [HttpPost("{id:int}/repay")]
    public async Task<ActionResult> Repay(int id, RepayLoanRequest request)
    {
        if (request.Amount <= 0) throw new InvalidOperationException("Le montant doit être supérieur à zéro.");

        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.LoanID == id)
            ?? throw new KeyNotFoundException("Prêt introuvable.");
        if (loan.Status != "ACTIVE") throw new InvalidOperationException("Ce prêt n'est plus actif.");

        // Refresh overdue penalties before allocating the payment.
        await ApplyPenaltiesAsync(loan);

        var remaining = request.Amount;
        decimal principalPaid = 0, interestPaid = 0, penaltyPaid = 0;

        var installments = await _db.LoanInstallments
            .Where(i => i.LoanID == id && i.Status != "PAID")
            .OrderBy(i => i.InstallmentNumber)
            .ToListAsync();

        foreach (var inst in installments)
        {
            if (remaining <= 0) break;

            var penaltyOwed = inst.PenaltyDue - inst.PenaltyPaid;
            var pay = Math.Min(remaining, penaltyOwed);
            inst.PenaltyPaid += pay; remaining -= pay; penaltyPaid += pay;

            var interestOwed = inst.InterestDue - inst.InterestPaid;
            pay = Math.Min(remaining, interestOwed);
            inst.InterestPaid += pay; remaining -= pay; interestPaid += pay;

            var principalOwed = inst.PrincipalDue - inst.PrincipalPaid;
            pay = Math.Min(remaining, principalOwed);
            inst.PrincipalPaid += pay; remaining -= pay; principalPaid += pay;

            var fullyPaid = inst.PrincipalPaid >= inst.PrincipalDue && inst.InterestPaid >= inst.InterestDue && inst.PenaltyPaid >= inst.PenaltyDue;
            inst.Status = fullyPaid ? "PAID" : (inst.PrincipalPaid + inst.InterestPaid + inst.PenaltyPaid) > 0 ? "PARTIAL" : inst.Status;
            if (fullyPaid) inst.PaidDate = DateTime.UtcNow;
        }

        loan.OutstandingPrincipal -= principalPaid;
        loan.OutstandingInterest -= interestPaid;
        loan.OutstandingPenalty -= penaltyPaid;

        var nextUnpaid = await _db.LoanInstallments.Where(i => i.LoanID == id && i.Status != "PAID").OrderBy(i => i.InstallmentNumber).FirstOrDefaultAsync();
        loan.NextDueDate = nextUnpaid?.DueDate;

        if (loan.OutstandingPrincipal <= 0.01m && loan.OutstandingInterest <= 0.01m && loan.OutstandingPenalty <= 0.01m)
        {
            loan.Status = "CLOSED";
            loan.ClosedDate = DateTime.UtcNow;
            loan.OutstandingPrincipal = 0; loan.OutstandingInterest = 0; loan.OutstandingPenalty = 0;
        }

        var receiptNumber = $"LNR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        _db.LoanRepayments.Add(new LoanRepayment
        {
            LoanID = id, Amount = request.Amount, PrincipalPaid = principalPaid, InterestPaid = interestPaid, PenaltyPaid = penaltyPaid,
            ReceiptNumber = receiptNumber, ReceivedBy = _currentUser.CodeUser!
        });

        await _db.SaveChangesAsync();

        return Ok(new { message = "Remboursement enregistré.", receiptNumber, loanStatus = loan.Status });
    }

    private async Task ApplyPenaltiesAsync(Loan loan)
    {
        var product = await _db.LoanProducts.FirstOrDefaultAsync(p => p.LoanProductID == loan.LoanProductID);
        if (product == null || product.PenaltyRatePerDay <= 0) return;

        var overdue = await _db.LoanInstallments
            .Where(i => i.LoanID == loan.LoanID && i.Status != "PAID" && i.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var inst in overdue)
        {
            var daysLate = (int)(DateTime.UtcNow.Date - inst.DueDate.Date).TotalDays - product.GracePeriodDays;
            if (daysLate <= 0) continue;

            var outstandingOnInstallment = (inst.PrincipalDue - inst.PrincipalPaid) + (inst.InterestDue - inst.InterestPaid);
            var expectedPenalty = Math.Round(outstandingOnInstallment * (product.PenaltyRatePerDay / 100m) * daysLate, 2);
            if (expectedPenalty > inst.PenaltyDue)
            {
                var delta = expectedPenalty - inst.PenaltyDue;
                inst.PenaltyDue = expectedPenalty;
                loan.OutstandingPenalty += delta;
                inst.Status = "OVERDUE";
            }
        }
    }

    [HttpPost("{id:int}/write-off")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> WriteOff(int id, WriteOffLoanRequest request)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.LoanID == id)
            ?? throw new KeyNotFoundException("Prêt introuvable.");

        loan.Status = "WRITTEN_OFF";
        loan.WriteOffReason = request.Reason;
        loan.ClosedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Prêt passé en perte (write-off)." });
    }

    // ---- Dashboard -----------------------------------------------------------

    [HttpGet("dashboard")]
    public async Task<ActionResult<LoanDashboardDto>> Dashboard()
    {
        var loans = await _db.Loans.ToListAsync();
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var overdueLoanIds = await _db.LoanInstallments
            .Where(i => i.Status != "PAID" && i.DueDate < DateTime.UtcNow)
            .Select(i => i.LoanID).Distinct().ToListAsync();

        return Ok(new LoanDashboardDto(
            loans.Count, loans.Count(l => l.Status == "ACTIVE"), loans.Count(l => l.Status == "CLOSED"),
            overdueLoanIds.Count,
            loans.Sum(l => l.PrincipalAmount),
            loans.Where(l => l.Status == "ACTIVE").Sum(l => l.OutstandingPrincipal),
            loans.Where(l => l.Status == "ACTIVE").Sum(l => l.OutstandingInterest),
            loans.Where(l => l.DisbursementDate >= monthStart).Sum(l => l.PrincipalAmount),
            await _db.LoanApplications.CountAsync(a => a.Status == "PENDING")
        ));
    }
}
