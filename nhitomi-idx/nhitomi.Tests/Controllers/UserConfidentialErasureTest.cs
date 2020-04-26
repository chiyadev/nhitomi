using Force.DeepCloner;
using nhitomi.Database;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi.Controllers
{
    [Parallelizable(ParallelScope.All)]
    public class UserConfidentialErasureTest
    {
        sealed class Controller : nhitomiControllerBase
        {
            public new User ProcessUser(User user) => base.ProcessUser(user);
        }

        readonly User _original = new User
        {
            Id       = "self",
            Username = "phosphene47",
            Email    = "phosphene47@chiya.dev",
            DiscordConnection = new UserDiscordConnection
            {
                Id = 2
            }
        };

        [Test]
        public void Self()
        {
            var controller = new Controller
            {
                User = new DbUser
                {
                    Id = "self"
                }
            };

            var user = controller.ProcessUser(_original.DeepClone());

            // nothing should be erased for self
            Assert.That(user.Username, Is.EqualTo("phosphene47"));
            Assert.That(user.Email, Is.EqualTo("phosphene47@chiya.dev"));
            Assert.That(user.DiscordConnection.Id, Is.EqualTo(2));
        }

        [Test]
        public void Moderator()
        {
            var controller = new Controller
            {
                User = new DbUser
                {
                    Id          = "mod",
                    Permissions = new[] { UserPermissions.ManageUsers }
                }
            };

            var user = controller.ProcessUser(_original.DeepClone());

            // always visible
            Assert.That(user.Username, Is.EqualTo("phosphene47"));

            // only mod
            Assert.That(user.Email, Is.EqualTo("phosphene47@chiya.dev"));

            // only self
            Assert.That(user.DiscordConnection, Is.Null);
        }

        [Test]
        public void Other()
        {
            var controller = new Controller
            {
                User = new DbUser
                {
                    Id = "other"
                }
            };

            var user = controller.ProcessUser(_original.DeepClone());

            // always visible
            Assert.That(user.Username, Is.EqualTo("phosphene47"));

            // erased
            Assert.That(user.Email, Is.Null);
            Assert.That(user.DiscordConnection, Is.Null);
        }
    }
}