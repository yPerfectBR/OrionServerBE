using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orion.Protocol.Login;

namespace Orion.Protocol.Tests;

public sealed class LoginAuthTests : IDisposable
{
    private const string TestIssuer = "https://authorization.franchise.minecraft-services.net/";
    private const string TestAudience = "api://auth-minecraft-services/multiplayer";
    private const string TestKid = "test-kid";

    public void Dispose()
    {
        LoginIdentity.ClearCachedAuthConfigForTesting();
    }

    [Fact]
    public void VerifyOffline_AcceptsAuthType2TokenWithDisplayNameInClientJwt()
    {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string publicKey = ExportSpkiBase64(clientKey);
        Guid playerUuid = Guid.NewGuid();

        string identityToken = SignEs384Jwt(
            clientKey,
            headerExtra: new Dictionary<string, string> { ["x5u"] = publicKey },
            payload: new Dictionary<string, object>
            {
                ["cpk"] = publicKey,
                ["identity"] = playerUuid.ToString()
            });

        string clientJwt = SignEs384Jwt(
            clientKey,
            headerExtra: new Dictionary<string, string> { ["x5u"] = publicKey },
            payload: new Dictionary<string, object>
            {
                ["SelfSignedId"] = playerUuid.ToString(),
                ["ThirdPartyName"] = "BedrockPlayer",
                ["SkinId"] = "standard_custom"
            });

        string identityJson = JsonSerializer.Serialize(new
        {
            AuthenticationType = 2,
            Token = identityToken
        });
        LoginEnvelope envelope = LoginEnvelope.Parse(identityJson);

        VerifiedIdentity identity = OfflineIdentity.VerifyOffline(envelope, clientJwt);

        Assert.Equal("BedrockPlayer", identity.Username);
        Assert.Equal(playerUuid.ToString(), identity.Uuid);
        Assert.Equal(publicKey, identity.IdentityPublicKey);
    }

    [Fact]
    public void Verify_AcceptsEs384ServiceTokenWithKid()
    {
        using ECDsa serviceKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string clientPublicKey = ExportSpkiBase64(clientKey);
        string playerUuid = Guid.NewGuid().ToString();

        ConfigureEs384AuthConfig(serviceKey);

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string token = SignEs384Jwt(
            serviceKey,
            headerExtra: new Dictionary<string, string> { ["kid"] = TestKid },
            payload: new Dictionary<string, object>
            {
                ["cpk"] = clientPublicKey,
                ["xname"] = "OnlinePlayer",
                ["xid"] = "9876543210987654",
                ["identity"] = playerUuid,
                ["iss"] = TestIssuer,
                ["aud"] = TestAudience,
                ["exp"] = now + 3600
            });

        string identityJson = JsonSerializer.Serialize(new { Token = token });
        VerifiedIdentity identity = LoginIdentity.Verify(identityJson);

        Assert.Equal("OnlinePlayer", identity.Username);
        Assert.Equal("9876543210987654", identity.Xuid);
        Assert.Equal(playerUuid, identity.Uuid);
        Assert.Equal(clientPublicKey, identity.IdentityPublicKey);
    }

    [Fact]
    public void Verify_AcceptsRs256ServiceToken()
    {
        using RSA serviceKey = RSA.Create(2048);
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string clientPublicKey = ExportSpkiBase64(clientKey);

        ConfigureRs256AuthConfig(serviceKey);

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string token = SignRs256Jwt(
            serviceKey,
            headerExtra: new Dictionary<string, string> { ["kid"] = TestKid, ["typ"] = "JWT" },
            payload: new Dictionary<string, object>
            {
                ["cpk"] = clientPublicKey,
                ["xname"] = "LegacyPlayer",
                ["xid"] = "5555666677778888",
                ["identity"] = Guid.NewGuid().ToString(),
                ["iss"] = TestIssuer,
                ["aud"] = TestAudience,
                ["exp"] = now + 3600
            });

        string identityJson = JsonSerializer.Serialize(new { Token = token });
        VerifiedIdentity identity = LoginIdentity.Verify(identityJson);

        Assert.Equal("LegacyPlayer", identity.Username);
    }

    private void ConfigureEs384AuthConfig(ECDsa serviceKey)
    {
        LoginIdentity.SetCachedAuthConfigForTesting(new LoginIdentity.AuthConfig(
            TestIssuer,
            ["ES384", "RS256"],
            new Dictionary<string, LoginIdentity.CachedVerificationKey>
            {
                [TestKid] = new LoginIdentity.CachedVerificationKey(null, ECDsa.Create(serviceKey.ExportParameters(false)))
            },
            DateTimeOffset.UtcNow.AddHours(1)));
    }

    private void ConfigureRs256AuthConfig(RSA serviceKey)
    {
        LoginIdentity.SetCachedAuthConfigForTesting(new LoginIdentity.AuthConfig(
            TestIssuer,
            ["ES384", "RS256"],
            new Dictionary<string, LoginIdentity.CachedVerificationKey>
            {
                [TestKid] = new LoginIdentity.CachedVerificationKey(RSA.Create(serviceKey.ExportParameters(false)), null)
            },
            DateTimeOffset.UtcNow.AddHours(1)));
    }

    private static string SignEs384Jwt(
        ECDsa privateKey,
        Dictionary<string, string> headerExtra,
        Dictionary<string, object> payload)
    {
        Dictionary<string, object> header = new() { ["alg"] = "ES384" };
        foreach ((string key, string value) in headerExtra)
        {
            header[key] = value;
        }

        return SignJwt(privateKey, header, payload, HashAlgorithmName.SHA384);
    }

    private static string SignRs256Jwt(
        RSA privateKey,
        Dictionary<string, string> headerExtra,
        Dictionary<string, object> payload)
    {
        Dictionary<string, object> header = new() { ["alg"] = "RS256" };
        foreach ((string key, string value) in headerExtra)
        {
            header[key] = value;
        }

        string headerSegment = EncodeBase64Url(JsonSerializer.Serialize(header));
        string payloadSegment = EncodeBase64Url(JsonSerializer.Serialize(payload));
        byte[] signingInput = Encoding.ASCII.GetBytes($"{headerSegment}.{payloadSegment}");
        byte[] signature = privateKey.SignData(signingInput, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{headerSegment}.{payloadSegment}.{EncodeBase64Url(signature)}";
    }

    private static string SignJwt(ECDsa privateKey, Dictionary<string, object> header, Dictionary<string, object> payload, HashAlgorithmName hashAlgorithm)
    {
        string headerSegment = EncodeBase64Url(JsonSerializer.Serialize(header));
        string payloadSegment = EncodeBase64Url(JsonSerializer.Serialize(payload));
        byte[] signingInput = Encoding.ASCII.GetBytes($"{headerSegment}.{payloadSegment}");
        byte[] signature = privateKey.SignData(signingInput, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{headerSegment}.{payloadSegment}.{EncodeBase64Url(signature)}";
    }

    private static string ExportSpkiBase64(ECDsa key) =>
        Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());

    private static string EncodeBase64Url(string value) =>
        EncodeBase64Url(Encoding.UTF8.GetBytes(value));

    private static string EncodeBase64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
