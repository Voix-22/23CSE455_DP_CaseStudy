using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

public enum AuthProvider { LOCAL, GOOGLE }
public enum CustomerStatus { ACTIVE, INACTIVE }

[Table("customers")]
public class Customer
{
    [Key]
    public long CustomerId { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? DrivingLicenseNo { get; set; }

    [MaxLength(30)]
    public string? PassportNo { get; set; }

    [MaxLength(255)]
    public string? Address1 { get; set; }

    [MaxLength(255)]
    public string? Address2 { get; set; }

    public long? CityId { get; set; }
    public long? StateId { get; set; }

    [MaxLength(10)]
    public string? Pincode { get; set; }

    public CustomerStatus? Status { get; set; }

    // BCrypt hash — never returned in any DTO. Null for guest customers
    // created mid-booking (upsertGuestCustomer) who have no password yet.
    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [MaxLength(20)]
    public AuthProvider? Provider { get; set; }

    [MaxLength(100)]
    public string? ProviderId { get; set; }

    public bool? EmailVerified { get; set; }

    // Base64-encoded document — no file-storage infra in this project, same
    // tradeoff as the Java @Lob column.
    [Column("govt_id_document")]
    public string? GovtIdDocument { get; set; }

    [Column("govt_id_document_name"), MaxLength(255)]
    public string? GovtIdDocumentName { get; set; }

    [Column("govt_id_document_type"), MaxLength(30)]
    public string? GovtIdDocumentType { get; set; }
}
