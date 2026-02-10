using Microsoft.AspNetCore.Identity;
using System;

namespace Hasher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hasher = new PasswordHasher<object>();
            var hash = hasher.HashPassword(new object(), "Lecturer@123");
            Console.WriteLine(hash);
        }
    }
}
