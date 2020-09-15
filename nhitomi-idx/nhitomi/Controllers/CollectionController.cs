using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Database;
using nhitomi.Models;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints for managing collections.
    /// </summary>
    [Route("collections")]
    public class CollectionController : nhitomiControllerBase
    {
        readonly IServiceProvider _services;
        readonly ICollectionService _collections;
        readonly IUserService _users;

        public CollectionController(IServiceProvider services, ICollectionService collections, IUserService users)
        {
            _services    = services;
            _collections = collections;
            _users       = users;
        }

        public class CreateCollectionRequest
        {
            /// <summary>
            /// Collection information.
            /// </summary>
            [Required]
            public CollectionBase Collection { get; set; }

            /// <summary>
            /// Type of objects in this collection.
            /// </summary>
            [Required]
            public ObjectType Type { get; set; }

            /// <summary>
            /// Items to add into this collection after creation.
            /// </summary>
            public string[] Items { get; set; }
        }

        /// <summary>
        /// Creates a new empty collection.
        /// </summary>
        /// <param name="request">Create collection request.</param>
        [HttpPost(Name = "createCollection"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult<Collection>> CreateAsync(CreateCollectionRequest request)
        {
            var result = await _collections.CreateAsync(request.Type, request.Collection, UserId, request.Items);

            return result.AsT0.Convert(_services);
        }

        CollectionConstraints CurrentConstraint => new CollectionConstraints
        {
            // allow manage users perm because collections are considered as part of "user data"
            OwnerId = User.HasPermissions(UserPermissions.ManageUsers) ? null : UserId
        };

        /// <summary>
        /// Retrieves collection information.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        [HttpGet("{id}", Name = "getCollection"), RequireUser]
        public async Task<ActionResult<Collection>> GetAsync(string id)
        {
            var result = await _collections.GetAsync(id);

            if (!result.TryPickT0(out var collection, out _) || !CurrentConstraint.Test(collection))
                return ResultUtilities.NotFound(id);

            return collection.Convert(_services);
        }

        /// <summary>
        /// Updates collection information.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="model">New collection information.</param>
        [HttpPut("{id}", Name = "updateCollection"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult<Collection>> UpdateAsync(string id, CollectionBase model)
        {
            var result = await _collections.UpdateAsync(id, model, CurrentConstraint);

            if (!result.TryPickT0(out var collection, out _))
                return ResultUtilities.NotFound(id);

            return collection.Convert(_services);
        }

        /// <summary>
        /// Deletes a collection.
        /// </summary>
        /// <remarks>
        /// This will remove all items in the collection. Collection will become inaccessible by all other co-owners.
        /// </remarks>
        /// <param name="id">Collection ID.</param>
        [HttpDelete("{id}", Name = "deleteCollection"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            await _collections.DeleteAsync(id, CurrentConstraint);

            return Ok();
        }

        public class AddCollectionOwnerRequest
        {
            /// <summary>
            /// ID of the user to add as a co-owner of the collection.
            /// </summary>
            [Required]
            public string UserId { get; set; }
        }

        /// <summary>
        /// Adds a user as a co-owner of a collection.
        /// </summary>
        /// <remarks>
        /// This may fail if the target user had opted out of collection sharing.
        /// </remarks>
        /// <param name="id">Collection ID.</param>
        /// <param name="request">Add owner request.</param>
        [HttpPost("{id}/owners", Name = "addCollectionOwner"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult<Collection>> AddOwnerAsync(string id, AddCollectionOwnerRequest request)
        {
            var userResult = await _users.GetAsync(request.UserId);

            if (!userResult.TryPickT0(out var user, out _) || !user.AllowSharedCollections)
                return ResultUtilities.NotFound(request.UserId);

            var result = await _collections.AddOwnerAsync(id, request.UserId, CurrentConstraint);

            if (!result.TryPickT0(out var collection, out _))
                return ResultUtilities.NotFound(id);

            return collection.Convert(_services);
        }

        /// <summary>
        /// Removes a user as a co-owner of a collection.
        /// </summary>
        /// <remarks>
        /// The collection will become inaccessible to the user who has been removed as co-owner.
        /// If the collection has no owners after this operation, the collection will be deleted instead.
        /// </remarks>
        /// <param name="id">Collection ID.</param>
        /// <param name="ownerId">Co-owner user ID.</param>
        [HttpDelete("{id}/owners/{ownerId}", Name = "removeCollectionOwner"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult> RemoveOwnerAsync(string id, string ownerId)
        {
            await _collections.RemoveOwnerAsync(id, ownerId, CurrentConstraint);

            return Ok();
        }

        public class CollectionItemsRequest
        {
            /// <summary>
            /// IDs of items in collection.
            /// </summary>
            [Required]
            public string[] Items { get; set; }
        }

        public class AddCollectionItemsRequest : CollectionItemsRequest
        {
            /// <summary>
            /// Position of the inserted items.
            /// </summary>
            [Required]
            public CollectionInsertPosition Position { get; set; }
        }

        /// <summary>
        /// Adds items to a collection.
        /// </summary>
        /// <remarks>
        /// Collections are ordered sets of items. Duplicate items are ignored.
        /// </remarks>
        /// <param name="id">Collection ID.</param>
        /// <param name="request">Add items request.</param>
        [HttpPost("{id}/items", Name = "addCollectionItems"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult<Collection>> AddItemsAsync(string id, AddCollectionItemsRequest request)
        {
            var result = await _collections.AddItemsAsync(id, request.Items, request.Position, CurrentConstraint);

            if (!result.TryPickT0(out var collection, out _))
                return ResultUtilities.NotFound(id);

            return collection.Convert(_services);
        }

        /// <summary>
        /// Removes items from a collection.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="request">Remove items request.</param>
        [HttpPost("{id}/items/delete", Name = "removeCollectionItems"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult<Collection>> RemoveItemsAsync(string id, CollectionItemsRequest request)
        {
            var result = await _collections.RemoveItemsAsync(id, request.Items, CurrentConstraint);

            if (!result.TryPickT0(out var collection, out _))
                return ResultUtilities.NotFound(id);

            return collection.Convert(_services);
        }

        /// <summary>
        /// Sorts the items in a collection.
        /// </summary>
        /// <remarks>
        /// The request should contain all items in the collection rearranged by the requester.
        /// If the array of items is a subset of the full collection, no guarantee is made about the order of items that are missing.
        /// </remarks>
        /// <param name="id">Collection ID.</param>
        /// <param name="request">Sort items request.</param>
        [HttpPost("{id}/items/sort", Name = "sortCollectionItems"), RequireUser(Unrestricted = true), RequireDbWrite]
        public async Task<ActionResult<Collection>> SortItemsAsync(string id, CollectionItemsRequest request)
        {
            var result = await _collections.SortAsync(id, request.Items, CurrentConstraint);

            if (!result.TryPickT0(out var collection, out _))
                return ResultUtilities.NotFound(id);

            return collection.Convert(_services);
        }
    }
}