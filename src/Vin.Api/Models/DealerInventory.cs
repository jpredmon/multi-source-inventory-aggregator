namespace Vin.Api.Models;

public class DealerInventory
{
     // No [Key] attribute needed. EF Core's default convention treats a property
    // named exactly "Id" (or "<ClassName>Id") as the primary key, and maps it
    // to a SQL Server IDENTITY column (auto-incrementing) unless told otherwise.
    public int Id { get; set; }
     // `= string.Empty` rather than a nullable `string?`. Since <Nullable>enable</Nullable>
    // is set in the csproj, a non-nullable string property must have a non-null default
    // to compile without a warning — and it maps to a NOT NULL column in SQL Server.
    // No explicit length ([MaxLength]/[Column(TypeName)]) was set, so EF's SqlServer
    // provider falls back to nvarchar(max) — fine for a practice project, but a real
    // schema would size this (VINs are always 17 chars; stock numbers are short).
    public string Vin { get; set; } = string.Empty;
    public string StockNumber { get; set; } = string.Empty;
     // decimal, never float/double, for money — float/double use binary floating point
    // and can't represent values like 14250.00 exactly, which silently corrupts totals
    // after enough arithmetic. EF Core maps decimal to SQL Server's decimal(18,2) by
    // convention here (no explicit HasPrecision call) — that default triggers an EF
    // startup warning in stricter configurations; worth knowing since it comes up in
    // real code review.
    public decimal Cost { get; set; }
    // Plain DateTime, not DateTimeOffset. This is the deliberate friction point from
    // the design spec: dealer dates arrive date-only ("2025-01-15"), auction dates
    // arrive as UTC "Z" timestamps, and sale dates arrive with a "-05:00" offset — but
    // all three get stored as offset-less DateTime. SQL Server's datetime2 (EF's
    // default mapping for DateTime) has no concept of timezone, so any offset
    // information is silently discarded at the moment of insert. That's the real-world
    // "systems disagree on timezone" bug class this project exists to practice.
    public DateTime DateAcquired { get; set; }
}
