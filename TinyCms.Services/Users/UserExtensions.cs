using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using TinyCms.Core;
using TinyCms.Core.Domain.Users;
using TinyCms.Core.Infrastructure;
using TinyCms.Services.Common;
using TinyCms.Services.Localization;

namespace TinyCms.Services.Users
{
    public static class UserExtensions
    {
        /// <summary>
        /// Get full name
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>User full name</returns>
        public static string GetFullName(this User user)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            var firstName = user.GetAttribute<string>(SystemUserAttributeNames.FirstName);
            var lastName = user.GetAttribute<string>(SystemUserAttributeNames.LastName);

            string fullName = "";
            if (!String.IsNullOrWhiteSpace(firstName) && !String.IsNullOrWhiteSpace(lastName))
                fullName = string.Format("{0} {1}", firstName, lastName);
            else
            {
                if (!String.IsNullOrWhiteSpace(firstName))
                    fullName = firstName;

                if (!String.IsNullOrWhiteSpace(lastName))
                    fullName = lastName;
            }
            return fullName;
        }
        /// <summary>
        /// Formats the user name
        /// </summary>
        /// <param name="user">Source</param>
        /// <param name="stripTooLong">Strip too long user name</param>
        /// <param name="maxLength">Maximum user name length</param>
        /// <returns>Formatted text</returns>
        public static string FormatUserName(this User user, bool stripTooLong = false, int maxLength = 0)
        {
            if (user == null)
                return string.Empty;

            if (user.IsGuest())
            {
                return EngineContext.Current.Resolve<ILocalizationService>().GetResource("User.Guest");
            }

            string result = string.Empty;
            switch (EngineContext.Current.Resolve<UserSettings>().UserNameFormat)
            {
                case UserNameFormat.ShowEmails:
                    result = user.Email;
                    break;
                case UserNameFormat.ShowUsernames:
                    result = user.Username;
                    break;
                case UserNameFormat.ShowFullNames:
                    result = user.GetFullName();
                    break;
                case UserNameFormat.ShowFirstName:
                    result = user.GetAttribute<string>(SystemUserAttributeNames.FirstName);
                    break;
                default:
                    break;
            }

            if (stripTooLong && maxLength > 0)
            {
                result = CommonHelper.EnsureMaximumLength(result, maxLength);
            }

            return result;
        }


        /// <summary>
        /// Gets coupon codes
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>Coupon codes</returns>
        public static string[] ParseAppliedGiftCardCouponCodes(this User user)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            var existingGiftCartCouponCodes = user.GetAttribute<string>(SystemUserAttributeNames.GiftCardCouponCodes,
                genericAttributeService);

            var couponCodes = new List<string>();
            if (String.IsNullOrEmpty(existingGiftCartCouponCodes))
                return couponCodes.ToArray();

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(existingGiftCartCouponCodes);

                var nodeList1 = xmlDoc.SelectNodes(@"//GiftCardCouponCodes/CouponCode");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes != null && node1.Attributes["Code"] != null)
                    {
                        string code = node1.Attributes["Code"].InnerText.Trim();
                        couponCodes.Add(code);
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.Write(exc.ToString());
            }
            return couponCodes.ToArray();
        }
        /// <summary>
        /// Adds a coupon code
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="couponCode">Coupon code</param>
        /// <returns>New coupon codes document</returns>
        public static void ApplyGiftCardCouponCode(this User user, string couponCode)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            string result = string.Empty;
            try
            {
                var existingGiftCartCouponCodes = user.GetAttribute<string>(SystemUserAttributeNames.GiftCardCouponCodes,
                    genericAttributeService);

                couponCode = couponCode.Trim().ToLower();

                var xmlDoc = new XmlDocument();
                if (String.IsNullOrEmpty(existingGiftCartCouponCodes))
                {
                    var element1 = xmlDoc.CreateElement("GiftCardCouponCodes");
                    xmlDoc.AppendChild(element1);
                }
                else
                {
                    xmlDoc.LoadXml(existingGiftCartCouponCodes);
                }
                var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//GiftCardCouponCodes");

                XmlElement gcElement = null;
                //find existing
                var nodeList1 = xmlDoc.SelectNodes(@"//GiftCardCouponCodes/CouponCode");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes != null && node1.Attributes["Code"] != null)
                    {
                        string couponCodeAttribute = node1.Attributes["Code"].InnerText.Trim();
                        if (couponCodeAttribute.ToLower() == couponCode.ToLower())
                        {
                            gcElement = (XmlElement)node1;
                            break;
                        }
                    }
                }

                //create new one if not found
                if (gcElement == null)
                {
                    gcElement = xmlDoc.CreateElement("CouponCode");
                    gcElement.SetAttribute("Code", couponCode);
                    rootElement.AppendChild(gcElement);
                }

                result = xmlDoc.OuterXml;
            }
            catch (Exception exc)
            {
                Debug.Write(exc.ToString());
            }

            //apply new value
            genericAttributeService.SaveAttribute(user, SystemUserAttributeNames.GiftCardCouponCodes, result);
        }
        /// <summary>
        /// Removes a coupon code
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="couponCode">Coupon code to remove</param>
        /// <returns>New coupon codes document</returns>
        public static void RemoveGiftCardCouponCode(this User user, string couponCode)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            //get applied coupon codes
            var existingCouponCodes = user.ParseAppliedGiftCardCouponCodes();

            //clear them
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            genericAttributeService.SaveAttribute<string>(user, SystemUserAttributeNames.GiftCardCouponCodes, null);

            //save again except removed one
            foreach (string existingCouponCode in existingCouponCodes)
                if (!existingCouponCode.Equals(couponCode, StringComparison.InvariantCultureIgnoreCase))
                    user.ApplyGiftCardCouponCode(existingCouponCode);
        }

        /// <summary>
        /// Check whether password recovery token is valid
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="token">Token to validate</param>
        /// <returns>Result</returns>
        public static bool IsPasswordRecoveryTokenValid(this User user, string token)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var cPrt = user.GetAttribute<string>(SystemUserAttributeNames.PasswordRecoveryToken);
            if (String.IsNullOrEmpty(cPrt))
                return false;

            if (!cPrt.Equals(token, StringComparison.InvariantCultureIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Check whether password recovery link is expired
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="userSettings">User settings</param>
        /// <returns>Result</returns>
        public static bool IsPasswordRecoveryLinkExpired(this User user, UserSettings userSettings)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            if (userSettings == null)
                throw new ArgumentNullException("userSettings");

            if (userSettings.PasswordRecoveryLinkDaysValid == 0)
                return false;
            
            var geneatedDate = user.GetAttribute<DateTime?>(SystemUserAttributeNames.PasswordRecoveryTokenDateGenerated);
            if (!geneatedDate.HasValue)
                return false;

            var daysPassed = (DateTime.UtcNow - geneatedDate.Value).TotalDays;
            if (daysPassed > userSettings.PasswordRecoveryLinkDaysValid)
                return true;

            return false;
        }

        /// <summary>
        /// Get user role identifiers
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="showHidden">A value indicating whether to load hidden records</param>
        /// <returns>User role identifiers</returns>
        public static int[] GetUserRoleIds(this User user, bool showHidden = false)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var userRolesIds = user.UserRoles
               .Where(cr => showHidden || cr.Active)
               .Select(cr => cr.Id)
               .ToArray();

            return userRolesIds;
        }
    }
}
