using Microsoft.AspNetCore.Identity;
using WebUtilities.Core.Entities;

namespace WebUtilities.Infrastructure.Identity;

public class ConsoleEmailSender : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        Console.WriteLine($"[EmailStub] Confirmation link for {email}: {confirmationLink}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        Console.WriteLine($"[EmailStub] Password reset code for {email}: {resetCode}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        Console.WriteLine($"[EmailStub] Password reset link for {email}: {resetLink}");
        return Task.CompletedTask;
    }
}
