using System.Net.Mail;
using MindNova.Domain.Entities;

namespace MindNova.Domain.Validation;

public static class ClientValidator
{
    public static List<string> Validate(Client client)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(client.FirstName))
            errors.Add("FirstName is required.");

        if (string.IsNullOrWhiteSpace(client.LastName))
            errors.Add("LastName is required.");

        if (string.IsNullOrWhiteSpace(client.Email))
        {
            errors.Add("Email is required.");
        }
        else if (!IsValidEmail(client.Email))
        {
            errors.Add("Email format is invalid.");
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Contains(' '))
            return false;

        return MailAddress.TryCreate(email, out _);
    }
}
