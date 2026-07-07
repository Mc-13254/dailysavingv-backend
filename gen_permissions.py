modules = [
    ("Dashboard", [("VIEW", "View Dashboard")]),
    ("IMF", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Agency", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Department", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Users", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Roles", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Contract Types", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Commission Types", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Commission Ranges", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Numbering Parameters", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete")]),
    ("Collectors", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete"),("APPROVE","Approve")]),
    ("Clients", [("VIEW","View"),("CREATE","Create"),("EDIT","Edit"),("DELETE","Delete"),("APPROVE","Approve")]),
    ("Operations", [
        ("DAILY_COLLECTIONS","Daily Collections"),("DEPOSITS","Deposits"),("WITHDRAWALS","Withdrawals"),
        ("TRANSFERS","Transfers"),("VALIDATION","Validation"),("REVERSE","Reverse Transaction"),
    ]),
    ("Reports", [("VIEW","View Reports"),("EXPORT","Export Reports"),("FINANCIAL","Financial Reports")]),
    ("Security", [("AUDIT_LOGS","Audit Logs"),("ACTIVITY_LOGS","Activity Logs"),("LOGIN_HISTORY","Login History")]),
]

def code(module, action):
    return (module.upper().replace(" ", "_") + "_" + action).replace("__","_")

with open("/home/claude/backend/DailySavingV.API/db_patch_permissions_module.sql", "w") as f:
    f.write("-- Patch: create the Permission/RolePermission module (RBAC matrix)\n")
    f.write("-- and seed the fixed permission catalog.\n\n")
    f.write("""CREATE TABLE Permission (
    PermissionID    INT IDENTITY(1,1) PRIMARY KEY,
    PermissionCode  NVARCHAR(60)  NOT NULL UNIQUE,
    PermissionName  NVARCHAR(100) NOT NULL,
    Module          NVARCHAR(50)  NOT NULL,
    Action          NVARCHAR(30)  NOT NULL,
    Description     NVARCHAR(300) NULL
);
GO

CREATE TABLE RolePermission (
    RolePermissionID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID           INT NOT NULL REFERENCES Role(RoleID) ON DELETE CASCADE,
    PermissionID     INT NOT NULL REFERENCES Permission(PermissionID) ON DELETE CASCADE,
    Allowed          BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_RolePermission UNIQUE (RoleID, PermissionID)
);
GO

""")
    f.write("INSERT INTO Permission (PermissionCode, PermissionName, Module, Action) VALUES\n")
    rows = []
    for module, actions in modules:
        for action, name in actions:
            c = code(module, action)
            rows.append(f"('{c}','{name}','{module}','{action}')")
    f.write(",\n".join(rows))
    f.write(";\nGO\n\n")

    f.write("-- Administrator role always has full permissions.\n")
    f.write("""INSERT INTO RolePermission (RoleID, PermissionID, Allowed)
SELECT r.RoleID, p.PermissionID, 1
FROM Role r
CROSS JOIN Permission p
WHERE r.Code = 'ADMIN';
GO
""")

print("done", sum(len(a) for _, a in modules), "permissions")
