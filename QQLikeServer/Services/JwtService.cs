using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using QQLike.Entity.Configuration;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class JwtService(JwtConfig config) : IJwtService
{
    public string Generate<T>(T payload, TimeSpan expire)
    {
        var type = payload.GetType();
        var claims = type.GetProperties()
            .Select(e=>new Claim(e.Name, 
                e.GetValue(payload)?.ToString() ?? string.Empty))
            .ToList();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claims,
            expires: DateTime.Now.AddSeconds(expire.TotalSeconds),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public T Parse<T>(string token)
    { 
        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config.Issuer,
            ValidAudience = config.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey))
        }, out var validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        var claims = jwtToken
            .Claims
            .ToDictionary(c => c.Type, c => c.Value);
        var result = Activator.CreateInstance<T>();
        foreach (var property in typeof(T).GetProperties())
        {
            if (claims.TryGetValue(property.Name, out var value))
            {
                var transformedValue = BaseTypeValueTransform(value, property.PropertyType);
                property.SetValue(result, transformedValue);
            }
        }
        return result;
    }

    private object BaseTypeValueTransform(string value,Type type)
    {
        if(type == typeof(string))
            return value;
        else if (type == typeof(int))
        {
            if (int.TryParse(value, out var intValue))
                return intValue;
        }
        else if(type == typeof(long))
        {
            if (long.TryParse(value, out var longValue))
                return longValue;
        }
        else if(type == typeof(bool))
        {
            if (bool.TryParse(value, out var boolValue))
                return boolValue;
        }
        else if(type == typeof(double))
        {
            if (double.TryParse(value, out var doubleValue))
                return doubleValue;
        }
        return null;
    }
    
}