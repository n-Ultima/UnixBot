using System;
using Microsoft.Extensions.Configuration;

namespace Unix.Common;

public class UnixConfiguration
{
    private string _Token = null!;
    private string _ConnectionString = null!;
    private ulong[] _OwnerIds = default!;
    private bool _PrivelegedMode = false;

    public string Token
    {
        get => _Token;
        set
        {
            if (value == null)
                throw new NullReferenceException("Token must be defined in appsettings.json file.");
            _Token = value;
        }
    }

    public string ConnectionString
    {
        get => _ConnectionString;
        set
        {
            if (value == null)
                throw new NullReferenceException("Database connection string must be defined in appsettings.json file.");
            _ConnectionString = value;
        }
    }

    public ulong[] OwnerIds
    {
        get => _OwnerIds;
        set
        {
            if (value == default)
                throw new NullReferenceException("OwnerIds must be defined in appsettings.json file.");
            _OwnerIds = value;
        }
    }

    public bool PrivelegedMode
    {
        get => _PrivelegedMode;
        set => _PrivelegedMode = value;
    }
    public UnixConfiguration()
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Token = config.GetValue<string>(nameof(Token));
        ConnectionString = config.GetValue<string>(nameof(ConnectionString));
        OwnerIds = config.GetSection(nameof(OwnerIds)).Get<ulong[]>();
        PrivelegedMode = config.GetValue<bool>(nameof(PrivelegedMode));
    }
}