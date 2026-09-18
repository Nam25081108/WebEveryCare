namespace EveryCare.Api.Domain.Enums;

public enum UserRole { Customer, Partner, Admin }
public enum UserStatus { Pending, Active, Locked }
public enum PartnerType { Individual, Team }
public enum VerificationStatus { Pending, Approved, Rejected }
public enum WorkArea { General, Scope, LivingRoom, Bedroom, Kitchen, Bathroom }
public enum BuildingType { House, Office }
public enum BuildingCondition { Existing, NewOrRenovated }
public enum AreaTier { Under60, From60To80, From81To100, Custom101To500 }
public enum BookingStatus { Draft, Searching, AwaitingCustomerSelection, Assigned, PartnerTravelling, InProgress, Completed, Cancelled, NoPartnerFound }
public enum AssignmentStatus { Invited, Accepted, Rejected, Expired, Selected, Released }
public enum CleanerSelectionMode { Automatic, CustomerChooses, FavoriteFirst }
public enum PaymentMethod { Cash, BankTransfer }
public enum PaymentStatus { Pending, Paid, Failed, Cancelled }
public enum PhotoType { Before, After }
public enum ExtraChargeStatus { PendingCustomerApproval, Approved, Rejected }
