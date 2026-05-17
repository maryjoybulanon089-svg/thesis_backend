using Microsoft.AspNetCore.Identity;

// Quick utility to generate password hashes
// Run this with: dotnet run PasswordHasher.cs

public class PasswordHasherUtility
{
    public static void Main(string[] args)
    {
        var hasher = new PasswordHasher<object>();
        
        Console.WriteLine("=== Password Hashes for ThesisRepositoryDB ===\n");
        
        var passwords = new Dictionary<string, string>
        {
            { "admin@thesis.com", "AdminPass123!" },
            { "faculty@thesis.com", "password123" },
            { "student@thesis.com", "password123" },
            { "uploader@thesis.com", "password123" },
            { "approver@thesis.com", "password123" }
        };
        
        foreach (var kvp in passwords)
        {
            var hash = hasher.HashPassword(null, kvp.Value);
            Console.WriteLine($"Email: {kvp.Key}");
            Console.WriteLine($"Password: {kvp.Value}");
            Console.WriteLine($"Hash: {hash}");
            Console.WriteLine();
        }
    }
}
