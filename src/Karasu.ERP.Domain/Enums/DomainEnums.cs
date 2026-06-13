namespace Karasu.ERP.Domain.Enums;

public enum BusinessType { Retail, Restaurant, Cafe, NightClub, Market, Wholesale, Distributor, ECommerce }
public enum SubscriptionPlan { Free, Starter, Professional, Enterprise }
public enum ProductStatus { Active, Inactive, Discontinued }
public enum CustomerType { Individual, Corporate }
public enum CustomerStatus { Active, Inactive, Blocked }
public enum OrderStatus { Draft, Pending, Confirmed, Preparing, Shipping, Delivered, Cancelled }
public enum OrderType { Sale, Return, Wholesale, Online, Pos }
public enum PaymentMethod { Cash, CreditCard, BankTransfer, Credit }
public enum PosSessionStatus { Open, Closed }
public enum StockMovementType { In, Out, Transfer, Adjustment, Return }
public enum StockTransferStatus { Pending, Completed, Cancelled }
public enum StockCountStatus { InProgress, Completed, Cancelled }
public enum QuoteStatus { Draft, Sent, Accepted, Rejected, Converted, Expired, Cancelled }
public enum InvoiceType { Standard, EInvoice, EArchive }
public enum InvoiceStatus { Draft, Issued, Paid, Cancelled }
public enum CashTransactionType { In, Out }
public enum BankTransactionType { In, Out }
public enum ReceivableStatus { Open, PartiallyPaid, Paid, Overdue, Cancelled }
public enum PayableStatus { Open, PartiallyPaid, Paid, Overdue, Cancelled }
public enum FinancePaymentDirection { Collection, Disbursement }
