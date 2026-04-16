using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    /// <summary>
    /// Regression test: the AppUpdate package must never expose fields or properties that imply
    /// user acceptance or dialog-shown semantics. The Play In-App Updates API does NOT surface
    /// whether the user saw or accepted the update dialog — only whether the flow completed.
    /// Any such field would be a semantic lie and a review-time red flag.
    /// </summary>
    [TestFixture]
    public class AppUpdateDataRegressionTests
    {
        static readonly string[] ForbiddenNames =
        {
            "UserAccepted",
            "DialogShown",
            "WasDialogShown",
            "UserAcceptedUpdate"
        };

        [Test]
        public void HasNoUserAcceptedField()
        {
            var assembly = typeof(AppUpdateInfo).Assembly;
            var violations = assembly.GetTypes()
                .SelectMany(t =>
                    t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                     .Select(f => $"{t.Name}.{f.Name}")
                     .Concat(
                    t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                     .Select(p => $"{t.Name}.{p.Name}"))
                )
                .Where(name => ForbiddenNames.Any(forbidden =>
                    name.Split('.').Last().Equals(forbidden, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            Assert.IsEmpty(violations,
                $"Found forbidden field/property names that imply user-acceptance semantics: " +
                $"{string.Join(", ", violations)}");
        }
    }
}
