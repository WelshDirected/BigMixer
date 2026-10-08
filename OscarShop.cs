using HarmonyLib;
using ScheduleOne.NPCs.CharacterClasses;
using ScheduleOne.UI.Shop;
using UnityEngine;

namespace BigMixer
{
    // Lists the Mk3 in Oscar's warehouse shop, in person only (not in the phone delivery app).
    internal static class OscarShop
    {
        public static bool TryAddListing()
        {
            if (Mk3Factory.Definition == null)
                return false;
            Oscar oscar = Object.FindObjectOfType<Oscar>();
            if (oscar == null || oscar.ShopInterface == null)
                return false;
            ShopInterface shop = oscar.ShopInterface;
            if (shop.GetListing(BigMixerMod.ItemId) != null)
                return true;

            ShopListing listing = new ShopListing();
            listing.name = BigMixerMod.ItemName;
            listing.Item = Mk3Factory.Definition;
            listing.CanBeDelivered = false;
            shop.Listings.Add(listing);
            AccessTools.Method(typeof(ShopInterface), "CreateListingUI").Invoke(shop, new object[] { listing });
            listing.Initialize(shop);
            AccessTools.Method(typeof(ShopInterface), "RefreshShownItems").Invoke(shop, null);
            BigMixerMod.Log.Msg("Added " + BigMixerMod.ItemName + " to " + shop.ShopName + " for $" + listing.Price + ".");
            return true;
        }
    }
}
