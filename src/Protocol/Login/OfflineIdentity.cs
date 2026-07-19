using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orion.Protocol.Login;

public readonly record struct OfflineCertificateData(
    string IdentityPublicKey,
    string DisplayName,
    string IdentityUuid
);

public readonly record struct OfflineTokenData(
    string IdentityPublicKey,
    string DisplayName,
    string IdentityUuid,
    string Xuid
);

public static class OfflineIdentity
{
    private const string AudienceApi = "api://auth-minecraft-services/multiplayer";

    public static bool IsOfflineLogin(string identityJson)
    {
        LoginEnvelope envelope = LoginEnvelope.Parse(identityJson);
        return IsOfflineLogin(envelope);
    }

    public static bool IsOfflineLogin(LoginEnvelope envelope)
    {
        if (envelope.AuthenticationType == 1)
        {
            return false;
        }

        if (envelope.AuthenticationType == 2)
        {
            return TryParseTokenPublicKey(envelope.Token, out _);
        }

        if (string.IsNullOrWhiteSpace(envelope.Token))
        {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        string token = JwtVerification.NormalizeToken(envelope.Token);
        if (!JwtVerification.TryDecodeJwt(token, out JsonElement header, out JsonElement payload))
        {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        string algorithm = JsonValue.GetString(header, "alg");
        if (string.Equals(algorithm, "RS256", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(JsonValue.GetString(payload, "aud"), AudienceApi, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (!IsEcAlgorithm(algorithm))
        {
            return false;
        }

        string displayName = FirstNonEmpty(
            JsonValue.GetString(payload, "xname"),
            JsonValue.GetString(payload, "displayName"));
        string publicKey = FirstNonEmpty(
            JsonValue.GetString(payload, "cpk"),
            JsonValue.GetString(payload, "clientPublicKey"),
            JsonValue.GetString(header, "x5u"));
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(publicKey))
        {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        string xuid = JsonValue.GetString(payload, "xid");
        if (!string.IsNullOrWhiteSpace(xuid) && IsOnlineIssuer(JsonValue.GetString(payload, "iss")))
        {
            return false;
        }

        return true;
    }

    public static VerifiedIdentity VerifyOffline(LoginEnvelope envelope, string clientJwt)
    {
        if (!string.IsNullOrWhiteSpace(envelope.Token)
            && TryParseTokenIdentity(envelope.Token, clientJwt, out OfflineTokenData token, out _))
        {
            VerifyClientJwt(clientJwt, token.IdentityPublicKey);

            string xuid = string.IsNullOrWhiteSpace(token.Xuid)
                ? GetOfflineXuid(token.DisplayName)
                : token.Xuid;

            Guid uuid = ResolveIdentityUuid(token.IdentityUuid, token.Xuid, token.DisplayName);

            return new VerifiedIdentity(
                token.IdentityPublicKey,
                token.DisplayName,
                xuid,
                uuid.ToString()
            );
        }

        if (TryParseLegacyChain(envelope.Chain, out OfflineCertificateData certificate))
        {
            VerifyClientJwt(clientJwt, certificate.IdentityPublicKey);
            string xuid = OfflineIdentity.GetOfflineXuid(certificate.DisplayName);
            return ToVerifiedIdentity(certificate, certificate.DisplayName, xuid);
        }

        throw new InvalidOperationException("Invalid offline certificate: missing extraData.");
    }

    public static OfflineCertificateData ParseCertificate(string identityJson)
    {
        LoginEnvelope envelope = LoginEnvelope.Parse(identityJson);
        if (!TryParseLegacyChain(envelope.Chain, out OfflineCertificateData data))
        {
            throw new InvalidOperationException("Invalid offline certificate: missing extraData.");
        }

        return data;
    }

    public static OfflineTokenData ParseOfflineToken(string token)
    {
        if (!TryParseTokenIdentity(token, out OfflineTokenData data))
        {
            throw new InvalidOperationException("Invalid offline token: missing player identity.");
        }

        return data;
    }

    public static void VerifyClientJwt(string clientJwt, string identityPublicKey)
    {
        JwtVerification.VerifyJwtSignature(clientJwt, identityPublicKey);
    }

    public static string GetOfflineXuid(string username)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"OfflineXUID:{username}"));
        ulong value = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(hash);
        return value.ToString().PadLeft(16, '0')[..16];
    }

    public static Guid GetUuidFromUsername(string username)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes($"OfflinePlayer:{username}"));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    public static Guid GetUuidFromXuid(string xuid)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("pocket-auth-1-xuid:" + xuid));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    public static VerifiedIdentity ToVerifiedIdentity(OfflineCertificateData certificate, string username, string xuid)
    {
        Guid uuid = Guid.TryParse(certificate.IdentityUuid, out Guid parsed)
            ? parsed
            : GetUuidFromUsername(username);

        return new VerifiedIdentity(
            certificate.IdentityPublicKey,
            username,
            xuid,
            uuid.ToString()
        );
    }

    private static bool TryParseTokenIdentity(string token, out OfflineTokenData data)
    {
        return TryParseTokenIdentity(token, clientJwt: null, out data, out _);
    }

    private static bool TryParseTokenIdentity(string token, string? clientJwt, out OfflineTokenData data, out string failureReason)
    {
        data = default;
        failureReason = string.Empty;
        token = JwtVerification.NormalizeToken(token);

        if (string.IsNullOrWhiteSpace(token))
        {
            failureReason = "empty_token";
            return false;
        }

        if (!JwtVerification.TryDecodeJwt(token, out JsonElement header, out JsonElement payload))
        {
            failureReason = "jwt_decode_failed";
            return false;
        }

        string publicKey = FirstNonEmpty(
            JsonValue.GetString(payload, "cpk"),
            JsonValue.GetString(payload, "clientPublicKey"),
            JsonValue.GetString(header, "x5u"));

        if (string.IsNullOrWhiteSpace(publicKey))
        {
            failureReason = "missing_public_key";
            return false;
        }

        string displayName = FirstNonEmpty(
            JsonValue.GetString(payload, "xname"),
            JsonValue.GetString(payload, "displayName"));

        string identity = FirstNonEmpty(
            JsonValue.GetString(payload, "identity"),
            JsonValue.GetString(payload, "uuid"),
            JsonValue.GetString(payload, "sub"));

        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(clientJwt))
        {
            TryParseClientIdentityHints(clientJwt, out string clientDisplayName, out string clientIdentity);
            displayName = clientDisplayName;
            identity = FirstNonEmpty(identity, clientIdentity);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            failureReason = "missing_display_name";
            return false;
        }

        string xuid = FirstNonEmpty(
            JsonValue.GetString(payload, "xid"),
            JsonValue.GetString(payload, "XUID"),
            JsonValue.GetString(payload, "xuid"));

        data = new OfflineTokenData(publicKey, displayName, identity, xuid);
        return true;
    }

    private static bool TryParseTokenPublicKey(string token, out string publicKey)
    {
        publicKey = string.Empty;
        token = JwtVerification.NormalizeToken(token);

        if (!JwtVerification.TryDecodeJwt(token, out JsonElement header, out JsonElement payload))
        {
            return false;
        }

        publicKey = FirstNonEmpty(
            JsonValue.GetString(payload, "cpk"),
            JsonValue.GetString(payload, "clientPublicKey"),
            JsonValue.GetString(header, "x5u"));

        return !string.IsNullOrWhiteSpace(publicKey);
    }

    private static bool TryParseClientIdentityHints(string clientJwt, out string displayName, out string identityUuid)
    {
        displayName = string.Empty;
        identityUuid = string.Empty;

        if (!JwtVerification.TryDecodeJwt(JwtVerification.NormalizeToken(clientJwt), out _, out JsonElement payload))
        {
            return false;
        }

        displayName = FirstNonEmpty(
            JsonValue.GetString(payload, "ThirdPartyName"),
            JsonValue.GetString(payload, "DisplayName"));

        identityUuid = JsonValue.GetString(payload, "SelfSignedId");
        return !string.IsNullOrWhiteSpace(displayName);
    }

    private static Guid ResolveIdentityUuid(string identityUuid, string xuid, string displayName)
    {
        if (Guid.TryParse(identityUuid, out Guid parsed))
        {
            return parsed;
        }

        if (!string.IsNullOrWhiteSpace(xuid))
        {
            return GetUuidFromXuid(xuid);
        }

        return GetUuidFromUsername(displayName);
    }

    private static bool TryParseLegacyChain(string[] chain, out OfflineCertificateData data)
    {
        data = default;

        for (int i = chain.Length - 1; i >= 0; i--)
        {
            if (TryParseJwtExtraData(chain[i], out data))
            {
                return true;
            }
        }

        for (int i = chain.Length - 1; i >= 0; i--)
        {
            if (TryParseTokenIdentity(chain[i], out OfflineTokenData token))
            {
                data = new OfflineCertificateData(token.IdentityPublicKey, token.DisplayName, token.IdentityUuid);
                return true;
            }
        }

        return false;
    }

    private static bool TryParseJwtExtraData(string jwt, out OfflineCertificateData data)
    {
        data = default;

        if (!JwtVerification.TryDecodeJwt(jwt, out JsonElement header, out JsonElement payload))
        {
            return false;
        }

        if (!payload.TryGetProperty("extraData", out JsonElement extraData))
        {
            return false;
        }

        string displayName = JsonValue.GetString(extraData, "displayName");
        string identity = JsonValue.GetString(extraData, "identity");
        string publicKey = FirstNonEmpty(
            JsonValue.GetString(payload, "identityPublicKey"),
            JsonValue.GetString(header, "x5u"));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        data = new OfflineCertificateData(publicKey, displayName, identity);
        return true;
    }

    private static bool IsEcAlgorithm(string algorithm)
    {
        return string.Equals(algorithm, "ES384", StringComparison.OrdinalIgnoreCase)
            || string.Equals(algorithm, "ES256", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOnlineIssuer(string issuer)
    {
        return issuer.Contains("minecraft-services", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("mojang", StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
            {
                return values[i];
            }
        }

        return string.Empty;
    }
}
