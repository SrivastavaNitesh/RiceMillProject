LIVE DATABASE SQL DEPLOYMENT - 28 OCT 2026
============================================================

Take a full database backup before deployment.

Run these files from the project root in this exact order:

1. Company_SubCompany_Location_Hierarchy.sql
2. Location_Optional_SubCompany.sql
3. Supervisor_Dashboard_Analytics.sql
4. Order_Return_Module.sql
5. Meth_Assignment_Visibility_Fix.sql

Recommended one-command deployment:

sqlcmd -S "LIVE_SERVER" -d "LIVE_DATABASE" -U "USERNAME" -P "PASSWORD" -b -i "Live_Deployment_2026-10-28.sql"

The master file uses SQLCMD :r include commands. In SSMS, enable
Query > SQLCMD Mode before executing Live_Deployment_2026-10-28.sql.

DO NOT RUN ON LIVE:

- Order_Return_Verification.sql (rollback-only integration verification)
- Annapurna_SubCompany_Location_Seed.sql (business data remapping utility)
- Any file inside analysis\ (test fixtures)
- Restore_Truncated_System_Masters.sql unless a restore is specifically required

Notes:

- The hierarchy migration preserves NULL SubCompanyId locations. These are
  valid direct-main-company locations.
- All five deployment scripts are repeatable/idempotent migrations.
- Stop deployment immediately if sqlcmd returns a non-zero exit code.
