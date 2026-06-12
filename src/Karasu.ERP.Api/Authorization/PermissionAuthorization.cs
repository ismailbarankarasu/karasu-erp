using Microsoft.AspNetCore.Authorization;

namespace Karasu.ERP.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole("CompanyOwner") || context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim("permission", requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public static class Policies
{
    public const string RoleView = "Role.View";
    public const string RoleCreate = "Role.Create";
    public const string RoleUpdate = "Role.Update";
    public const string AuditView = "Audit.View";

    public const string ProductView = "Product.View";
    public const string ProductCreate = "Product.Create";
    public const string ProductUpdate = "Product.Update";
    public const string ProductDelete = "Product.Delete";

    public const string CustomerView = "Customer.View";
    public const string CustomerCreate = "Customer.Create";
    public const string CustomerUpdate = "Customer.Update";
    public const string CustomerDelete = "Customer.Delete";

    public const string OrderView = "Order.View";
    public const string OrderCreate = "Order.Create";
    public const string OrderUpdate = "Order.Update";
    public const string OrderDelete = "Order.Delete";
    public const string OrderConfirm = "Order.Confirm";
    public const string OrderCancel = "Order.Cancel";

    public const string QuoteView = "Quote.View";
    public const string QuoteCreate = "Quote.Create";
    public const string QuoteUpdate = "Quote.Update";
    public const string QuoteConvert = "Quote.Convert";

    public const string InvoiceView = "Invoice.View";
    public const string InvoiceCreate = "Invoice.Create";

    public const string StockView = "Stock.View";
    public const string StockAdjust = "Stock.Adjust";
    public const string StockTransferCreate = "Stock.Transfer.Create";
    public const string StockCountCreate = "Stock.Count.Create";

    public const string WarehouseView = "Warehouse.View";
    public const string WarehouseCreate = "Warehouse.Create";
    public const string WarehouseUpdate = "Warehouse.Update";
    public const string WarehouseDelete = "Warehouse.Delete";

    public const string PosSessionOpen = "Pos.Session.Open";
    public const string PosSessionClose = "Pos.Session.Close";
    public const string PosSessionView = "Pos.Session.View";
    public const string PosSaleSell = "Pos.Sale.Sell";
    public const string PosSaleReturn = "Pos.Sale.Return";
}
