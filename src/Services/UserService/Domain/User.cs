namespace UserService.Domain;

/// <summary>
/// User entity representing a customer or admin.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string Role { get; private set; } = "Customer";
    public bool IsActive { get; private set; } = true;
    public bool EmailVerified { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? RefreshTokenExpiry { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    private User() { } // EF Core

    public static User Create(string email, string passwordHash, string firstName, string lastName, string? phoneNumber = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateProfile(string? firstName, string? lastName, string? phoneNumber)
    {
        if (!string.IsNullOrWhiteSpace(firstName))
            FirstName = firstName;
        if (!string.IsNullOrWhiteSpace(lastName))
            LastName = lastName;
        if (phoneNumber != null)
            PhoneNumber = phoneNumber;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetRefreshToken(string token, DateTimeOffset expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetRole(string role)
    {
        Role = role;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Address AddAddress(string street, string city, string state, string postalCode, string country, string addressType, bool isDefault = false)
    {
        if (isDefault)
        {
            foreach (var addr in _addresses.Where(a => a.AddressType == addressType))
            {
                addr.SetDefault(false);
            }
        }

        var address = Address.Create(Id, street, city, state, postalCode, country, addressType, isDefault);
        _addresses.Add(address);
        return address;
    }

    public void RemoveAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address != null)
        {
            _addresses.Remove(address);
        }
    }
}

/// <summary>
/// User address entity.
/// </summary>
public class Address
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string AddressType { get; private set; } = "Shipping";
    public bool IsDefault { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Address() { } // EF Core

    public static Address Create(Guid userId, string street, string city, string state, string postalCode, string country, string addressType, bool isDefault)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Street = street,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country,
            AddressType = addressType,
            IsDefault = isDefault,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }
}
