namespace DailySavingV.API.DTOs;

// Shared across every Maker-Checker "reject" endpoint (Roles, Agence, IMF,
// Users, Collector, Client, Contract, ContractType, Commission, Department,
// Accounts...).
public record RejectRequest(string Reason);
