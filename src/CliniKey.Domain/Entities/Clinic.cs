using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Clinic : AggregateRoot<Guid>, IAuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxAddressLength = 500;

    public Guid TenantId { get; private init; }
    public string Name { get; private set; }
    public PhoneNumber Phone { get; private set; }
    public string Address { get; private set; }
    public ClinicStatus Status { get; private set; }
    public DateTime? DeactivatedAtUtc { get; private set; }
    public Guid? DeactivatedByUserId { get; private set; }

    public bool IsActive => Status == ClinicStatus.Active;

    private readonly List<ClinicDentist> _clinicDentists = [];
    public IReadOnlyCollection<ClinicDentist> ClinicDentists => _clinicDentists.AsReadOnly();

    private Clinic(
        Guid id,
        Guid tenantId,
        string name,
        PhoneNumber phone,
        string address,
        TimeProvider clock) : base(clock)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Phone = phone;
        Address = address;
        Status = ClinicStatus.Active;
    }

    private Clinic()
    {
        Name = null!;
        Phone = null!;
        Address = null!;
    }

    public static Result<Clinic> Create(
        Guid id,
        Guid tenantId,
        string name,
        string phone,
        string address,
        TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return Result.Failure<Clinic>(ClinicErrors.InvalidName);
        }

        var phoneResult = PhoneNumber.Create(phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure<Clinic>(phoneResult.Error);
        }

        if (string.IsNullOrWhiteSpace(address) || address.Length > MaxAddressLength)
        {
            return Result.Failure<Clinic>(ClinicErrors.InvalidAddress);
        }

        return new Clinic(id, tenantId, name, phoneResult.Value, address, clock);
    }

    public Result Activate()
    {
        if (Status == ClinicStatus.Active) return Result.Failure(ClinicErrors.AlreadyActive);

        var now = Clock.GetUtcNow().UtcDateTime;
        Status = ClinicStatus.Active;
        DeactivatedAtUtc = null;
        DeactivatedByUserId = null;
        MarkUpdated();
        RaiseDomainEvent(new ClinicActivatedEvent(Id, now));
        return Result.Success();
    }

    public Result Deactivate() => Deactivate(null);

    public Result Deactivate(Guid? operatorUserId)
    {
        if (Status == ClinicStatus.Inactive) return Result.Failure(ClinicErrors.AlreadyInactive);

        var now = Clock.GetUtcNow().UtcDateTime;
        Status = ClinicStatus.Inactive;
        DeactivatedAtUtc = now;
        DeactivatedByUserId = operatorUserId;
        MarkUpdated();
        RaiseDomainEvent(new ClinicDeactivatedEvent(Id, operatorUserId, now));
        return Result.Success();
    }

    public Result Suspend(Guid? operatorUserId)
    {
        if (Status == ClinicStatus.Suspended) return Result.Failure(ClinicErrors.AlreadySuspended);

        var now = Clock.GetUtcNow().UtcDateTime;
        Status = ClinicStatus.Suspended;
        DeactivatedAtUtc = now;
        DeactivatedByUserId = operatorUserId;
        MarkUpdated();
        RaiseDomainEvent(new ClinicDeactivatedEvent(Id, operatorUserId, now));
        return Result.Success();
    }

    public Result UpdateContact(string phone, string address)
    {
        var phoneResult = PhoneNumber.Create(phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure(phoneResult.Error);
        }

        if (string.IsNullOrWhiteSpace(address) || address.Length > MaxAddressLength)
        {
            return Result.Failure(ClinicErrors.InvalidAddress);
        }

        if (Phone == phoneResult.Value && Address == address)
        {
            return Result.Success();
        }

        var now = Clock.GetUtcNow().UtcDateTime;
        Phone = phoneResult.Value;
        Address = address;
        MarkUpdated();
        RaiseDomainEvent(new ClinicContactUpdatedEvent(Id, now));
        return Result.Success();
    }

    public Result AddDentist(Guid dentistId)
    {
        if (_clinicDentists.Any(cd => cd.DentistId == dentistId))
        {
            return Result.Failure(ClinicErrors.DentistAlreadyAdded);
        }

        _clinicDentists.Add(ClinicDentist.Create(Id, dentistId));
        MarkUpdated();
        return Result.Success();
    }

    public Result RemoveDentist(Guid dentistId)
    {
        var clinicDentist = _clinicDentists.FirstOrDefault(cd => cd.DentistId == dentistId);
        if (clinicDentist is null)
        {
            return Result.Failure(ClinicErrors.DentistNotFound);
        }

        _clinicDentists.Remove(clinicDentist);
        MarkUpdated();
        return Result.Success();
    }

}
