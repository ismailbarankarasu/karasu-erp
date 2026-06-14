using Microsoft.AspNetCore.Authorization;

namespace Karasu.ERP.Api.Authorization;

public static class AuthorizationPolicyRegistration
{
    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        void Add(string policyName, string permission) =>
            options.AddPolicy(policyName, p => p.Requirements.Add(new PermissionRequirement(permission)));

        Add(Policies.RoleView, "Role.Role.View");
        Add(Policies.RoleCreate, "Role.Role.Create");
        Add(Policies.RoleUpdate, "Role.Role.Update");
        Add(Policies.AuditView, "Audit.Log.View");

        Add(Policies.ProductView, "Product.Product.View");
        Add(Policies.ProductCreate, "Product.Product.Create");
        Add(Policies.ProductUpdate, "Product.Product.Update");
        Add(Policies.ProductDelete, "Product.Product.Delete");

        Add(Policies.CustomerView, "Customer.Customer.View");
        Add(Policies.CustomerCreate, "Customer.Customer.Create");
        Add(Policies.CustomerUpdate, "Customer.Customer.Update");
        Add(Policies.CustomerDelete, "Customer.Customer.Delete");

        Add(Policies.OrderView, "Order.Order.View");
        Add(Policies.OrderCreate, "Order.Order.Create");
        Add(Policies.OrderUpdate, "Order.Order.Update");
        Add(Policies.OrderDelete, "Order.Order.Delete");
        Add(Policies.OrderConfirm, "Order.Order.Confirm");
        Add(Policies.OrderCancel, "Order.Order.Cancel");

        Add(Policies.QuoteView, "Quote.Quote.View");
        Add(Policies.QuoteCreate, "Quote.Quote.Create");
        Add(Policies.QuoteUpdate, "Quote.Quote.Update");
        Add(Policies.QuoteConvert, "Quote.Quote.Convert");

        Add(Policies.InvoiceView, "Invoice.Invoice.View");
        Add(Policies.InvoiceCreate, "Invoice.Invoice.Create");

        Add(Policies.StockView, "Stock.Item.View");
        Add(Policies.StockAdjust, "Stock.Item.Adjust");
        Add(Policies.StockTransferCreate, "Stock.Transfer.Create");
        Add(Policies.StockCountCreate, "Stock.Count.Create");

        Add(Policies.WarehouseView, "Warehouse.Warehouse.View");
        Add(Policies.WarehouseCreate, "Warehouse.Warehouse.Create");
        Add(Policies.WarehouseUpdate, "Warehouse.Warehouse.Update");
        Add(Policies.WarehouseDelete, "Warehouse.Warehouse.Delete");

        Add(Policies.PosSessionOpen, "Pos.Session.Open");
        Add(Policies.PosSessionClose, "Pos.Session.Close");
        Add(Policies.PosSessionView, "Pos.Session.View");
        Add(Policies.PosSaleSell, "Pos.Sale.Sell");
        Add(Policies.PosSaleReturn, "Pos.Sale.Return");

        Add(Policies.FinanceView, "Finance.Account.View");
        Add(Policies.FinanceCreate, "Finance.Account.Create");

        Add(Policies.DashboardView, "Dashboard.Summary.View");

        Add(Policies.ReportSalesView, "Report.Sales.View");
        Add(Policies.ReportFinanceView, "Report.Sales.View");
        Add(Policies.ReportCustomerView, "Report.Sales.View");
        Add(Policies.ReportProductView, "Report.Sales.View");
        Add(Policies.ReportStockView, "Report.Sales.View");
        Add(Policies.ReportExport, "Report.Sales.Export");
    }
}
