:On Error exit

PRINT '01/05 Company, subcompany and location hierarchy';
:r .\Company_SubCompany_Location_Hierarchy.sql

PRINT '02/05 Optional subcompany support for locations';
:r .\Location_Optional_SubCompany.sql

PRINT '03/05 Supervisor dashboard analytics';
:r .\Supervisor_Dashboard_Analytics.sql

PRINT '04/05 Order return module';
:r .\Order_Return_Module.sql

PRINT '05/05 Meth assignment, dashboard and inline return workflow';
:r .\Meth_Assignment_Visibility_Fix.sql

PRINT 'LIVE DATABASE DEPLOYMENT COMPLETED SUCCESSFULLY';
