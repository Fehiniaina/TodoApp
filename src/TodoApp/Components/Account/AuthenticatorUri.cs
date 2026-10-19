using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

namespace TodoApp.Components.Account;

internal static class AuthenticatorUri
{
    private const string Issuer = "TodoApp";

    [SuppressMessage("Major Code Smell", "S1075:URIs should not be hardcoded",
        Justification = "otpauth:// est un format de protocole fixe, pas une URI d'environnement.")]
    private static readonly CompositeFormat Format = CompositeFormat.Parse("otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6");

    public static string Create(UrlEncoder encoder, string email, string unformattedKey) =>
        string.Format(
            CultureInfo.InvariantCulture,
            string.Format(null, Format, 42),
            encoder.Encode(Issuer),
            encoder.Encode(email),
            unformattedKey);
}