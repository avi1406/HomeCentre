// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Implementations.B2C.Graph.Models;
using Microsoft.Graph;

namespace HomeCentre.PIM.BAL.Implementations.B2C.Graph.Services
{
    internal static class UserService
    {
        public static async Task<IList<User>> ListUsers(this GraphServiceClient graphClient)
        {
            // Get all users (one page)
            var result = await graphClient.Users
                .Request()
                .Select(e => new
                {
                    e.DisplayName,
                    e.Id,
                    e.Identities
                })
                .GetAsync();

            return result.CurrentPage;
        }

        public static async Task<IList<User>> ListUsersWithCustomAttribute(this GraphServiceClient graphClient,
            string[] customAttributes, string b2CExtensionAppClientId)
        {
            if (string.IsNullOrWhiteSpace(b2CExtensionAppClientId))
            {
                throw new ArgumentException(
                    "B2cExtensionAppClientId (its Application ID) is missing from appsettings.json. Find it in the App registrations pane in the Azure portal. The app registration has the name 'b2c-extensions-app. Do not modify. Used by AADB2C for storing user data.'.",
                    nameof(b2CExtensionAppClientId));
            }

            // Declare the names of the custom attributes
            const string customAttributeName1 = "FavouriteSeason";
            const string customAttributeName2 = "LovesPets";

            Helpers.B2CCustomAttributeHelper helper = new Helpers.B2CCustomAttributeHelper(b2CExtensionAppClientId);
            var customAttributesCompleteNames =
                string.Join(",", customAttributes.Select(x => helper.GetCompleteAttributeName(x)));

            // Get all users (one page)
            var result = await graphClient.Users
                .Request()
                .Select($"id,displayName,identities,{customAttributesCompleteNames}")
                .GetAsync();

            return result.CurrentPage;
        }

        public static async Task<User> GetUserById(this GraphServiceClient graphClient, string userId)
        {
            // Get user by object ID
            var result = await graphClient.Users[userId]
                .Request()
                .Select(e => new
                {
                    e.DisplayName,
                    e.Id,
                    e.Identities
                })
                .GetAsync();

            return result;
        }

        public static async Task<User> GetUserBySignInName(this GraphServiceClient graphClient, string userId,
            string tenantId)
        {
            // Get user by sign-in name
            var result = await graphClient.Users
                .Request()
                .Filter($"identities/any(c:c/issuerAssignedId eq '{userId}' and c/issuer eq '{tenantId}')")
                .Select(e => new
                {
                    e.DisplayName,
                    e.Id,
                    e.Identities
                })
                .GetAsync();
            return result[0];
        }

        public static async Task DeleteUserById(this GraphServiceClient graphClient, string userId)
        {
            // Delete user by object ID
            await graphClient.Users[userId]
                .Request()
                .DeleteAsync();
        }

        public static async Task SetPasswordByUserId(this GraphServiceClient graphClient, string userId,
            string password)
        {
            var user = new User
            {
                PasswordPolicies = "DisablePasswordExpiration,DisableStrongPassword",
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = false,
                    Password = password,
                }
            };

            // Update user by object ID
            await graphClient.Users[userId]
                .Request()
                .UpdateAsync(user);
        }

        public static async Task<List<User>> BulkCreate(this GraphServiceClient graphClient, UsersModel users,
            string tenantId)
        {
            var outputUsers = new List<User>();
            foreach (var user in users.Users)
            {
                user.SetB2CProfile(tenantId);

                // Create the user account in the directory
                outputUsers.Add(await graphClient.Users
                    .Request()
                    .AddAsync(user));
            }

            return outputUsers;
        }

        public static async Task<User> CreateUserWithCustomAttribute(this GraphServiceClient graphClient,
            string b2CExtensionAppClientId, string tenantId, User user, Dictionary<string, string> customAttributes)
        {
            if (string.IsNullOrWhiteSpace(b2CExtensionAppClientId))
            {
                throw new ArgumentException(
                    "B2C Extension App ClientId (ApplicationId) is missing in the appsettings.json. Get it from the App Registrations blade in the Azure portal. The app registration has the name 'b2c-extensions-app. Do not modify. Used by AADB2C for storing user data.'.",
                    nameof(b2CExtensionAppClientId));
            }

            // Get the complete name of the custom attribute (Azure AD extension)
            Helpers.B2CCustomAttributeHelper helper = new Helpers.B2CCustomAttributeHelper(b2CExtensionAppClientId);

            // Fill custom attributes
            IDictionary<string, object> extensionInstance = new Dictionary<string, object>();
            foreach (var customAttribute in customAttributes.Keys)
            {
                extensionInstance.Add(helper.GetCompleteAttributeName(customAttribute),
                    customAttributes[customAttribute]);
            }

            // Create user
            var result = await graphClient.Users
                .Request()
                .AddAsync(user);

            var userId = result.Id;
            var customAttributesCompleteNames =
                string.Join(",", customAttributes.Keys.Select(x => helper.GetCompleteAttributeName(x)));

            // Get created user by object ID
            result = await graphClient.Users[userId]
                .Request()
                .Select(
                    $"id,givenName,surName,displayName,identities,{customAttributesCompleteNames}")
                .GetAsync();
            return result;
        }
    }
}