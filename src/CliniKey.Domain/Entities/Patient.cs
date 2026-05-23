using CliniKey.Domain.Enums;
using CliniKey.Domain.Events;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Patient : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletable
{
    public PatientName Name { get; private set; }
    public PhoneNumber Phone { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? InsuranceDetails { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Patient(
        Guid id,
        PatientName name,
        PhoneNumber phone,
        DateOnly dateOfBirth,
        Gender gender,
        string? insuranceDetails,
        TimeProvider clock) : base(clock)
    {
        Id = id;
        Name = name;
        Phone = phone;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        InsuranceDetails = insuranceDetails;
    }

    private Patient()
    {
        Name = null!;
        Phone = null!;
    }

    public static Patient Create(
        PatientName name,
        PhoneNumber phone,
        DateOnly dateOfBirth,
        Gender gender,
        TimeProvider clock,
        string? insuranceDetails = null)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        var patient = new Patient(
            Guid.NewGuid(),
            name,
            phone,
            dateOfBirth,
            gender,
            insuranceDetails,
            clock);

        patient.RaiseDomainEvent(new PatientCreatedEvent(patient.Id, now));

        return patient;
    }

    public void UpdatePhone(PhoneNumber newPhone)
    {
        if (Phone != newPhone)
        {
            Phone = newPhone;
        }
    }

    public void UpdateInsurance(string? insuranceDetails)
    {
        InsuranceDetails = insuranceDetails;
    }

    public void SoftDelete()
    {
        if (!IsDeleted)
        {
            IsDeleted = true;
            DeletedAtUtc = Clock.GetUtcNow().UtcDateTime;
        }
    }
}
