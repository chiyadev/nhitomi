using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi.Controllers
{
    public class UserControllerTest : TestBaseServices
    {
        [Test]
        public async Task OpenRegister()
        {
            var users = Services.GetService<IUserService>();

            var controller = Services.GetService<UserController>();

            // open registration
            GetOptions<UserServiceOptions>().OpenRegistration = true;

            // register
            var user = (await controller.CreateAsync(new NewUserRequest
            {
                Username = "testUser672",
                Password = "securePassword1234"
            })).Value.User;

            Assert.That(user, Is.Not.Null);

            var id = user.Id;

            // authenticate
            var authResponse = (await controller.AuthenticateAsync(new AuthenticateRequest
            {
                Username = "testUser672",
                Password = "securePassword1234"
            })).Value;

            Assert.That(authResponse.Token, Is.Not.Null);
            Assert.That(authResponse.User, Is.Not.Null);
            Assert.That(authResponse.User.Id, Is.EqualTo(id));

            // get user (by other user)
            controller.User = null;

            user = (await controller.GetAsync(id)).Value;

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Id, Is.EqualTo(id));
            Assert.That(user.Username, Is.EqualTo("testUser672"));
            Assert.That(user.Email, Is.Null);
            Assert.That(user.Permissions, Is.Empty);
            Assert.That(user.Restrictions, Is.Null.Or.Empty);

            // get user (by self)
            controller.User = await users.GetAsync(id);

            user = (await controller.GetAsync(id)).Value;

            // creation snapshot
            var snapshots = await controller.SearchSnapshotsAsync(new SnapshotQuery
            {
                TargetId = user.Id,
                Limit    = 100
            });

            Assert.That(snapshots.Total, Is.EqualTo(1));
            Assert.That(snapshots.Items, Has.One.Items);
            Assert.That(snapshots.Items[0].Type, Is.EqualTo(SnapshotType.Creation));
            Assert.That(snapshots.Items[0].CommitterId, Is.EqualTo(id));
            Assert.That(snapshots.Items[0].Source, Is.EqualTo(SnapshotSource.System));
            Assert.That(snapshots.Items[0].Target, Is.EqualTo(SnapshotTarget.User));
            Assert.That(snapshots.Items[0].TargetId, Is.EqualTo(user.Id));

            // get snapshot value (by self)
            var snapshotValue = (await controller.GetSnapshotValueAsync(snapshots.Items[0].Id)).Value;

            Assert.That(snapshotValue, Is.Not.Null);
            Assert.That(snapshotValue.Id, Is.EqualTo(id));
            //Assert.That(snapshotValue.Email, Is.EqualTo(""));

            // get snapshot value (by other user)
            controller.User = await users.GetAsync(id);

            snapshotValue = (await controller.GetSnapshotValueAsync(snapshots.Items[0].Id)).Value;

            Assert.That(snapshotValue, Is.Not.Null);
            Assert.That(snapshotValue.Id, Is.EqualTo(id));
            Assert.That(snapshotValue.Email, Is.Null);
        }

        [Test]
        public async Task InviteRegister()
        {
            var users = Services.GetService<IUserService>();

            var controller = Services.GetService<UserController>();

            // open registration
            GetOptions<UserServiceOptions>().OpenRegistration = true;

            // create initial user
            var inviter = (await controller.CreateAsync(new NewUserRequest
            {
                Username = "user",
                Password = "pass"
            })).Value.User;

            Assert.That(inviter, Is.Not.Null);

            // create invite
            controller.User = await users.GetAsync(inviter.Id);

            var invite = (await controller.CreateInviteAsync(new NewUserInviteRequest
            {
                ExpiryMinutes = 17
            })).Value;

            Assert.That(invite, Is.Not.Null);
            Assert.That(invite.Accepted, Is.False);
            Assert.That(invite.InviterId, Is.EqualTo(inviter.Id));
            Assert.That(invite.InviteeId, Is.Null);
            Assert.That(invite.AcceptedTime, Is.Null);
            Assert.That(invite.ExpiryTime, Is.EqualTo(invite.CreatedTime + TimeSpan.FromMinutes(17)));

            // cannot delete invite if not owned
            controller.User = await MakeUserAsync();

            var deleteInvite = await controller.DeleteInviteAsync(invite.Id);

            Assert.That(deleteInvite, Is.TypeOf<BadRequestObjectResult>());

            // able to delete invite before being accepted
            controller.User = await users.GetAsync(inviter.Id);

            deleteInvite = await controller.DeleteInviteAsync(invite.Id);

            Assert.That(deleteInvite, Is.TypeOf<OkResult>());

            // create invite again
            invite = (await controller.CreateInviteAsync(new NewUserInviteRequest
            {
                ExpiryMinutes = 17
            })).Value;

            Assert.That(invite, Is.Not.Null);
            Assert.That(invite.Accepted, Is.False);
            Assert.That(invite.AcceptedTime, Is.Null);

            // close registration
            GetOptions<UserServiceOptions>().OpenRegistration = false;

            controller.UserId = null;

            // try registering with invalid invite
            var createResult = await controller.CreateAsync(new NewUserRequest
            {
                Username = "user2",
                Password = "pass",
                InviteId = "  f "
            });

            Assert.That(createResult.Value, Is.Null);
            Assert.That(createResult.Result, Is.TypeOf<BadRequestObjectResult>());

            // register using invite
            createResult = await controller.CreateAsync(new NewUserRequest
            {
                Username = "user2",
                Password = "pass",
                InviteId = invite.Id
            });

            Assert.That(createResult.Value, Is.Not.Null);

            var invitee = createResult.Value.User;

            Assert.That(invitee.Username, Is.EqualTo("user2"));

            // invite information should be updated (as invitee)
            controller.User = await users.GetAsync(invitee.Id);

            invite = (await controller.GetInviteAsync(invite.Id)).Value;

            Assert.That(invite, Is.Not.Null);
            Assert.That(invite.Accepted, Is.True);
            Assert.That(invite.InviterId, Is.EqualTo(inviter.Id));
            Assert.That(invite.InviteeId, Is.EqualTo(invitee.Id));
            Assert.That(invite.AcceptedTime, Is.Not.Null);
            Assert.That(invite.ExpiryTime, Is.EqualTo(invite.CreatedTime + TimeSpan.FromMinutes(17)));

            // invite information should be updated (as inviter)
            controller.User = await users.GetAsync(inviter.Id);

            invite = (await controller.GetInviteAsync(invite.Id)).Value;

            Assert.That(invite, Is.Not.Null);

            // invite cannot be viewed by users other than inviter or invitee
            controller.User = await MakeUserAsync();

            var nullInvite = (await controller.GetInviteAsync(invite.Id)).Value;

            Assert.That(nullInvite, Is.Null);

            // invite cannot be deleted after being used
            controller.User = await users.GetAsync(inviter.Id);

            var deleteResult = await controller.DeleteInviteAsync(invite.Id);

            Assert.That(deleteResult, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task MaxInviteLimit()
        {
            var user = await MakeUserAsync();

            var service = Services.GetService<IUserInviteService>();

            // accepted invites should not be counted towards the limit
            for (var i = 0; i < 3; i++)
            {
                var invite = await service.CreateAsync(user.Id, TimeSpan.FromMinutes(10));

                await service.SetAcceptedAsync(invite.Id, user.Id);
            }

            var limit = GetOptions<UserServiceOptions>().MaxInvitesPerUser;

            const int over = 3;

            var controller = Services.GetService<UserController>();
            controller.User = user;

            // create some invites over the limit
            var invites = await Task.WhenAll(
                Enumerable.Range(0, limit + over)
                          .Select(async _ =>
                           {
                               var result = await controller.CreateInviteAsync(new NewUserInviteRequest
                               {
                                   ExpiryMinutes = 10
                               });

                               return result.Value;
                           }));

            Assert.That(invites.Where(i => i != null), Has.Exactly(limit).Items);
            Assert.That(invites, Has.Exactly(limit + over).Items);
        }
    }
}